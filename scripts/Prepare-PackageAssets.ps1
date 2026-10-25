param(
    [Parameter(Mandatory = $true)]
    [string]$SourceDirectory,

    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory
)

# Anonymizes LMU result files so they can be published as fixtures, while keeping every race
# event (incidents, penalties, track limits, scoring...) usable by the application.
# - One alias map is shared by every place where a driver name appears: <Name>, <Swap>, the
#   Driver="..." attribute of stream events and the names written inside event texts. The same
#   person therefore gets the same "Driver NNNN" alias everywhere and in every file.
# - Chat messages keep their sender alias, but their content is removed: it is free text typed
#   by players and cannot be anonymized reliably.
# - Names that are already aliases are kept unchanged, so the script can be re-run on fixtures
#   that were anonymized before (new names get aliases numbered after the highest existing one).
# The text processing is written in C# because result files hold about 100 000 events and
# PowerShell regex evaluators are too slow for that volume.

$ErrorActionPreference = 'Stop'
$sourcePath = [System.IO.Path]::GetFullPath($SourceDirectory)
$outputPath = [System.IO.Path]::GetFullPath($OutputDirectory)

if (-not (Test-Path -LiteralPath $sourcePath -PathType Container)) {
    throw "The source results directory does not exist: $sourcePath"
}

$sourceFiles = @(Get-ChildItem -LiteralPath $sourcePath -Filter '*.xml' -File | Sort-Object Name)
if ($sourceFiles.Count -eq 0) {
    throw "No XML result fixtures were found in: $sourcePath"
}

if (-not ('LmuFixtureAnonymizer' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

public static class LmuFixtureAnonymizer
{
    private const RegexOptions Options = RegexOptions.Compiled | RegexOptions.CultureInvariant;
    private static readonly Regex DriverBlock = new Regex(@"<Driver\b[^>]*>.*?</Driver>", Options | RegexOptions.Singleline);
    private static readonly Regex DriverName = new Regex(@"(<Name\b[^>]*>)(.*?)(</Name>)", Options | RegexOptions.Singleline);
    private static readonly Regex Swap = new Regex(@"(<Swap\b[^>]*>)([^<]*)(</Swap>)", Options);
    private static readonly Regex DriverAttribute = new Regex(@"(<[A-Za-z]+\b[^>]*?\bDriver="")([^""]*)("")", Options);
    private static readonly Regex Chat = new Regex(@"(<ChatMessage\b[^>]*>)([^<]*)(</ChatMessage>)", Options);
    private static readonly Regex Event = new Regex(@"(<(Incident|Score|Sector|Penalty|Sent)\b[^>]*>)([^<]*)(</\2>)", Options);
    private static readonly Regex ChatSender = new Regex(@"^(?<name>[^:]+?): ", Options);

    // Each pattern captures, in the group "name", a driver name written inside an event text.
    // The formats come from LMU result files: contacts, lap scoring, sector bests, damage,
    // penalties, driver swaps and the chequered flag.
    private static readonly Regex[] EventNames =
    {
        new Regex(@"^(?<name>.+?)\(\d+\)", Options),
        new Regex(@"with another vehicle (?<name>.+?)\(\d+\)$", Options),
        new Regex(@"^(?<name>.+?) (?:received|served|finished before serving) ", Options),
        new Regex(@"^Checkered for (?<name>.+?), laps=", Options),
        new Regex(@"^&quot;(?<name>.+?)&quot; has taken the wheel", Options),
        new Regex(@"^(?<name>.+?) Is now driving ", Options)
    };

    public static string Normalize(string rawName)
    {
        var name = WebUtility.HtmlDecode(rawName).Trim();
        return name.Length == 0 ? null : name.Normalize(NormalizationForm.FormKC).ToUpperInvariant();
    }

    public static void CollectNames(string content, HashSet<string> names)
    {
        Action<string> add = raw =>
        {
            var name = Normalize(raw);
            if (name != null)
            {
                names.Add(name);
            }
        };

        foreach (Match block in DriverBlock.Matches(content))
        {
            var name = DriverName.Match(block.Value);
            if (name.Success)
            {
                add(name.Groups[2].Value);
            }
        }

        foreach (Match match in Swap.Matches(content)) add(match.Groups[2].Value);
        foreach (Match match in DriverAttribute.Matches(content)) add(match.Groups[2].Value);
        foreach (Match match in Event.Matches(content))
        {
            foreach (var pattern in EventNames)
            {
                foreach (Match name in pattern.Matches(match.Groups[3].Value)) add(name.Groups["name"].Value);
            }
        }

        foreach (Match match in Chat.Matches(content))
        {
            var sender = ChatSender.Match(match.Groups[2].Value);
            if (sender.Success) add(sender.Groups["name"].Value);
        }
    }

    public static string Anonymize(string content, IDictionary<string, string> aliases)
    {
        Func<string, string> alias = raw =>
        {
            var name = Normalize(raw);
            return name == null ? raw : aliases[name];
        };
        MatchEvaluator replaceGroup2 = match =>
            match.Groups[1].Value + alias(match.Groups[2].Value) + match.Groups[3].Value;

        content = DriverBlock.Replace(content, block => DriverName.Replace(block.Value, replaceGroup2, 1));
        content = Swap.Replace(content, replaceGroup2);
        content = DriverAttribute.Replace(content, replaceGroup2);
        content = Event.Replace(content, match =>
        {
            var text = match.Groups[3].Value;
            foreach (var pattern in EventNames)
            {
                text = pattern.Replace(text, name =>
                {
                    var group = name.Groups["name"];
                    var start = group.Index - name.Index;
                    return name.Value.Substring(0, start) + alias(group.Value) + name.Value.Substring(start + group.Length);
                });
            }

            return match.Groups[1].Value + text + match.Groups[4].Value;
        });
        content = Chat.Replace(content, match =>
        {
            var sender = ChatSender.Match(match.Groups[2].Value);
            var senderAlias = sender.Success ? alias(sender.Groups["name"].Value) : "Driver";
            return match.Groups[1].Value + senderAlias + ": [message retiré]" + match.Groups[3].Value;
        });

        return content;
    }

    public static string FindLeak(string content, Regex realNamePattern)
    {
        var texts = new List<string>();
        foreach (Match match in DriverName.Matches(content)) texts.Add(match.Groups[2].Value);
        foreach (Match match in Swap.Matches(content)) texts.Add(match.Groups[2].Value);
        foreach (Match match in DriverAttribute.Matches(content)) texts.Add(match.Groups[2].Value);
        foreach (Match match in Event.Matches(content)) texts.Add(match.Groups[3].Value);
        foreach (Match match in Chat.Matches(content)) texts.Add(match.Groups[2].Value);

        var decoded = WebUtility.HtmlDecode(string.Join("\n", texts)).Normalize(NormalizationForm.FormKC);
        var leak = realNamePattern.Match(decoded);
        return leak.Success ? leak.Value : null;
    }
}
'@
}

$names = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
foreach ($sourceFile in $sourceFiles) {
    [LmuFixtureAnonymizer]::CollectNames([System.IO.File]::ReadAllText($sourceFile.FullName), $names)
}

if ($names.Count -eq 0) {
    throw "No driver names were found in the result fixtures under: $sourcePath"
}

# Existing aliases keep their number; real names are sorted and numbered after them.
$aliasPattern = [regex]'^DRIVER (\d{4,})$'
$aliases = [System.Collections.Generic.Dictionary[string, string]]::new([System.StringComparer]::Ordinal)
$highestAliasNumber = 0
$realNames = [System.Collections.Generic.List[string]]::new()
foreach ($name in $names) {
    $aliasMatch = $aliasPattern.Match($name)
    if ($aliasMatch.Success) {
        $aliasNumber = [int]$aliasMatch.Groups[1].Value
        $aliases[$name] = 'Driver {0:D4}' -f $aliasNumber
        $highestAliasNumber = [Math]::Max($highestAliasNumber, $aliasNumber)
    }
    else {
        $realNames.Add($name)
    }
}

$sortedNames = $realNames.ToArray()
[System.Array]::Sort($sortedNames, [System.StringComparer]::Ordinal)
for ($index = 0; $index -lt $sortedNames.Length; $index++) {
    $aliases[$sortedNames[$index]] = 'Driver {0:D4}' -f ($highestAliasNumber + $index + 1)
}

[void](New-Item -ItemType Directory -Path $outputPath -Force)
$encoding = [System.Text.UTF8Encoding]::new($false)
$processedFiles = 0

foreach ($sourceFile in $sourceFiles) {
    $content = [System.IO.File]::ReadAllText($sourceFile.FullName)
    $destinationFile = Join-Path $outputPath $sourceFile.Name
    [System.IO.File]::WriteAllText($destinationFile, [LmuFixtureAnonymizer]::Anonymize($content, $aliases), $encoding)
    $processedFiles++
}

# Final check: no collected real name may remain where driver names are written (names, swaps,
# Driver attributes, event and chat texts). Vehicle and team names are not checked, because a
# driver nicknamed after a car brand would otherwise be reported.
if ($sortedNames.Length -gt 0) {
    $realNamePattern = [regex]::new(
        '(?<![\p{L}\p{N}])(?:' + (($sortedNames | Sort-Object Length -Descending | ForEach-Object { [regex]::Escape($_) }) -join '|') + ')(?![\p{L}\p{N}])',
        [System.Text.RegularExpressions.RegexOptions]::IgnoreCase -bor [System.Text.RegularExpressions.RegexOptions]::CultureInvariant)
    foreach ($destinationFile in Get-ChildItem -LiteralPath $outputPath -Filter '*.xml' -File) {
        $leak = [LmuFixtureAnonymizer]::FindLeak([System.IO.File]::ReadAllText($destinationFile.FullName), $realNamePattern)
        if ($null -ne $leak) {
            throw "A driver name was not anonymized in packaged fixture '$($destinationFile.Name)' (near '$leak')."
        }
    }
}

Write-Output "Prepared $processedFiles anonymized result fixtures using $($aliases.Count) consistent driver aliases."
