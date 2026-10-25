param(
    [Parameter(Mandatory = $true)]
    [string]$SourceDirectory,

    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory,

    # Merges drivers who took part in at most one race into other drivers they never met, so the
    # demo fixtures contain fewer one-off drivers and richer head-to-head histories.
    [switch]$MergeSingleRaceDrivers
)

# Anonymizes LMU result files so they can be published as fixtures, while keeping every race
# event (incidents, penalties, track limits, scoring...) usable by the application.
# - One alias map is shared by every place where a driver name appears: <Name>, <Swap>, the
#   Driver="..." attribute of stream events and the names written inside event texts. The same
#   person therefore gets the same alias everywhere and in every file.
# - Aliases are readable first names ("Driver Tom", "Driver Nathan"...), given by decreasing
#   number of appearances; a number is appended when the list is exhausted ("Driver Tom 2").
# - With -MergeSingleRaceDrivers, a driver with at most one race shares the alias of another
#   driver only if they never appear in the same file, so no session contains the alias twice.
#   The main driver (most frequent) is never merged, and a merged alias never exceeds half of
#   the main driver's appearances, so the dashboard still detects the main driver.
# - Chat messages keep their sender alias, but their content is removed: it is free text typed
#   by players and cannot be anonymized reliably.
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

    /// Returns the names written in the <Name> element of each <Driver> block.
    public static HashSet<string> CollectDriverNames(string content)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match block in DriverBlock.Matches(content))
        {
            var name = DriverName.Match(block.Value);
            if (name.Success)
            {
                var normalized = Normalize(name.Groups[2].Value);
                if (normalized != null) names.Add(normalized);
            }
        }

        return names;
    }

    /// Gives every name a first-name alias, optionally merging drivers with at most one race.
    /// filesNames: every name found in each file; driverNames: names of the <Driver> blocks of
    /// each file; isRace: whether each file is a race.
    public static Dictionary<string, string> BuildAliases(
        IList<HashSet<string>> filesNames,
        IList<HashSet<string>> driverNames,
        IList<bool> isRace,
        string[] firstNames,
        bool mergeSingleRaceDrivers)
    {
        var files = new Dictionary<string, HashSet<int>>(StringComparer.Ordinal);
        var races = new Dictionary<string, int>(StringComparer.Ordinal);
        var driverFileCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var index = 0; index < filesNames.Count; index++)
        {
            foreach (var name in filesNames[index])
            {
                HashSet<int> set;
                if (!files.TryGetValue(name, out set)) files[name] = set = new HashSet<int>();
                set.Add(index);
                if (isRace[index]) races[name] = (races.TryGetValue(name, out var count) ? count : 0) + 1;
            }

            foreach (var name in driverNames[index])
            {
                driverFileCounts[name] = (driverFileCounts.TryGetValue(name, out var count) ? count : 0) + 1;
            }
        }

        var mainName = driverFileCounts
            .OrderByDescending(entry => entry.Value)
            .ThenBy(entry => entry.Key, StringComparer.Ordinal)
            .First().Key;
        var mergedFileCap = Math.Max(1, driverFileCounts[mainName] / 2);
        Func<string, int> raceCount = name => races.TryGetValue(name, out var count) ? count : 0;
        Func<string, int> firstFile = name => files[name].Min();

        var mainGroup = new AliasGroup(mainName, files[mainName]);
        var groups = new List<AliasGroup>();
        var singles = new List<string>();
        foreach (var name in files.Keys
            .Where(name => name != mainName)
            .OrderBy(firstFile)
            .ThenBy(name => name, StringComparer.Ordinal))
        {
            // Names that only appear inside events (never in a <Driver> block) are not a listed
            // driver of any session, so they can be merged whatever their number of races.
            if (mergeSingleRaceDrivers && (raceCount(name) < 2 || !driverFileCounts.ContainsKey(name)))
            {
                singles.Add(name);
            }
            else
            {
                groups.Add(new AliasGroup(name, files[name]));
            }
        }

        foreach (var name in singles)
        {
            var nameFiles = files[name];
            var target = groups
                .Where(group => group.Files.Count + nameFiles.Count <= mergedFileCap && !group.Files.Overlaps(nameFiles))
                .OrderBy(group => group.Files.Count)
                .FirstOrDefault();
            if (target == null)
            {
                groups.Add(new AliasGroup(name, nameFiles));
            }
            else
            {
                target.Add(name, nameFiles);
            }
        }

        var orderedGroups = new[] { mainGroup }
            .Concat(groups
                .OrderByDescending(group => group.Files.Count)
                .ThenBy(group => group.Files.Min()))
            .ToList();
        var aliases = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var index = 0; index < orderedGroups.Count; index++)
        {
            var round = index / firstNames.Length;
            var alias = "Driver " + firstNames[index % firstNames.Length] + (round == 0 ? string.Empty : " " + (round + 1));
            foreach (var member in orderedGroups[index].Members) aliases[member] = alias;
        }

        return aliases;
    }

    private sealed class AliasGroup
    {
        public AliasGroup(string name, IEnumerable<int> files)
        {
            Members = new List<string> { name };
            Files = new HashSet<int>(files);
        }

        public List<string> Members { get; }
        public HashSet<int> Files { get; }

        public void Add(string name, IEnumerable<int> files)
        {
            Members.Add(name);
            Files.UnionWith(files);
        }
    }

    public static string FindLeak(string content, Regex realNamePattern, Regex aliasPattern)
    {
        var texts = new List<string>();
        foreach (Match match in DriverName.Matches(content)) texts.Add(match.Groups[2].Value);
        foreach (Match match in Swap.Matches(content)) texts.Add(match.Groups[2].Value);
        foreach (Match match in DriverAttribute.Matches(content)) texts.Add(match.Groups[2].Value);
        foreach (Match match in Event.Matches(content)) texts.Add(match.Groups[3].Value);
        foreach (Match match in Chat.Matches(content)) texts.Add(match.Groups[2].Value);

        // Aliases are removed first: a real driver called "Tom" must not be reported inside "Driver Tom".
        var decoded = WebUtility.HtmlDecode(string.Join("\n", texts)).Normalize(NormalizationForm.FormKC);
        var leak = realNamePattern.Match(aliasPattern.Replace(decoded, " "));
        return leak.Success ? leak.Value : null;
    }
}
'@
}

# Readable aliases, given in this order to drivers sorted by decreasing number of appearances.
$firstNames = [string[]]@(
    'Tom', 'Nathan', 'Lucas', 'Emma', 'Hugo', 'Léa', 'Louis', 'Chloé', 'Jules', 'Manon',
    'Arthur', 'Camille', 'Adam', 'Sarah', 'Gabriel', 'Inès', 'Raphaël', 'Jade', 'Théo', 'Lina',
    'Noah', 'Zoé', 'Ethan', 'Alice', 'Paul', 'Louise', 'Victor', 'Clara', 'Maxime', 'Julia',
    'Antoine', 'Eva', 'Mathis', 'Anna', 'Enzo', 'Rose', 'Sacha', 'Mila', 'Léo', 'Nina',
    'Simon', 'Elsa', 'Martin', 'Lucie', 'Oscar', 'Margaux', 'Samuel', 'Pauline', 'Axel', 'Lou',
    'Bastien', 'Agathe', 'Clément', 'Juliette', 'Damien', 'Charlotte', 'Elliot', 'Romane', 'Félix', 'Anaïs',
    'Gaspard', 'Iris', 'Henri', 'Margot', 'Isaac', 'Nora', 'Jonas', 'Ambre', 'Kevin', 'Lisa',
    'Laurent', 'Mathilde', 'Marius', 'Noémie', 'Nicolas', 'Océane', 'Olivier', 'Pia', 'Pierre', 'Salomé',
    'Quentin', 'Tess', 'Rémi', 'Valentine', 'Romain', 'Yasmine', 'Sébastien', 'Ava', 'Thomas', 'Bianca',
    'Ulysse', 'Diane', 'Valentin', 'Elena', 'William', 'Flora', 'Xavier', 'Gina', 'Yanis', 'Hélène',
    'Zacharie', 'Ingrid', 'Alexandre', 'Joséphine', 'Benjamin', 'Katia', 'Cédric', 'Laura', 'David', 'Maëlle',
    'Erwan', 'Nadia', 'Fabien', 'Olivia', 'Guillaume', 'Pénélope', 'Hector', 'Rachel', 'Ivan', 'Sofia',
    'Jérémy', 'Tania', 'Kylian', 'Ursula', 'Loïc', 'Vera', 'Mehdi', 'Wendy', 'Nils', 'Yara',
    'Owen', 'Zélie', 'Patrick', 'Adèle', 'Robin', 'Bérénice', 'Stéphane', 'Cécile', 'Tristan', 'Delphine',
    'Vincent', 'Emilie', 'Yann', 'Fanny', 'Aaron', 'Garance', 'Basile', 'Héloïse', 'Charles', 'Isabelle',
    'Dylan', 'Jeanne', 'Edouard', 'Karine', 'Florian', 'Léonie', 'Gilles', 'Marine', 'Hugues', 'Nathalie',
    'Igor', 'Ophélie', 'Jordan', 'Perrine', 'Kenzo', 'Rebecca', 'Lenny', 'Sandra', 'Malo', 'Thaïs',
    'Nolan', 'Victoire', 'Oren', 'Aurore', 'Pablo', 'Brune', 'Rayan', 'Capucine', 'Sven', 'Dora',
    'Timéo', 'Estelle', 'Ugo', 'Frida', 'Vadim', 'Gabrielle', 'Warren', 'Hanna', 'Yoann', 'Irène',
    'Zayn', 'Jasmine', 'Alban', 'Kim', 'Bruno', 'Lola', 'Cyril', 'Maya', 'Dorian', 'Nell',
    'Eliott', 'Albane', 'Fabrice', 'Bertille', 'Gauthier', 'Coralie', 'Hadrien', 'Daphné', 'Ilan', 'Eléa',
    'Joachim', 'Faustine', 'Kilian', 'Gaëlle', 'Lilian', 'Hortense', 'Mattéo', 'Ilona', 'Noé', 'Judith',
    'Octave', 'Kenza', 'Philippe', 'Lise', 'Raoul', 'Mélina', 'Swann', 'Nolwenn', 'Thibault', 'Orane',
    'Ugolin', 'Prune', 'Wilfried', 'Roxane', 'Yvan', 'Solène', 'Ambroise', 'Tiphaine', 'Benoît', 'Vanille'
)

$filesNames = [System.Collections.Generic.List[System.Collections.Generic.HashSet[string]]]::new()
$driverNames = [System.Collections.Generic.List[System.Collections.Generic.HashSet[string]]]::new()
$isRace = [System.Collections.Generic.List[bool]]::new()
foreach ($sourceFile in $sourceFiles) {
    $content = [System.IO.File]::ReadAllText($sourceFile.FullName)
    $fileNames = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    [LmuFixtureAnonymizer]::CollectNames($content, $fileNames)
    $filesNames.Add($fileNames)
    $driverNames.Add([LmuFixtureAnonymizer]::CollectDriverNames($content))
    $isRace.Add($content.Contains('<Race>'))
}

$sortedNames = [string[]]@($filesNames | ForEach-Object { $_ } | Sort-Object -Unique)
if ($sortedNames.Length -eq 0) {
    throw "No driver names were found in the result fixtures under: $sourcePath"
}

$aliases = [LmuFixtureAnonymizer]::BuildAliases($filesNames, $driverNames, $isRace, $firstNames, $MergeSingleRaceDrivers.IsPresent)

[void](New-Item -ItemType Directory -Path $outputPath -Force)
$encoding = [System.Text.UTF8Encoding]::new($false)
$processedFiles = 0

foreach ($sourceFile in $sourceFiles) {
    $content = [System.IO.File]::ReadAllText($sourceFile.FullName)
    $destinationFile = Join-Path $outputPath $sourceFile.Name
    [System.IO.File]::WriteAllText($destinationFile, [LmuFixtureAnonymizer]::Anonymize($content, $aliases), $encoding)
    $processedFiles++
}

# Final check: no original name may remain where driver names are written (names, swaps, Driver
# attributes, event and chat texts). Vehicle and team names are not checked, because a driver
# nicknamed after a car brand would otherwise be reported.
$options = [System.Text.RegularExpressions.RegexOptions]::IgnoreCase -bor [System.Text.RegularExpressions.RegexOptions]::CultureInvariant
$realNamePattern = [regex]::new(
    '(?<![\p{L}\p{N}])(?:' + (($sortedNames | Sort-Object Length -Descending | ForEach-Object { [regex]::Escape($_) }) -join '|') + ')(?![\p{L}\p{N}])',
    $options)
$aliasPattern = [regex]::new(
    '\bDriver (?:' + (($firstNames | ForEach-Object { [regex]::Escape($_) }) -join '|') + ')(?: \d+)?(?![\p{L}\p{N}])',
    $options)
foreach ($destinationFile in Get-ChildItem -LiteralPath $outputPath -Filter '*.xml' -File) {
    $leak = [LmuFixtureAnonymizer]::FindLeak([System.IO.File]::ReadAllText($destinationFile.FullName), $realNamePattern, $aliasPattern)
    if ($null -ne $leak) {
        throw "A driver name was not anonymized in packaged fixture '$($destinationFile.Name)' (near '$leak')."
    }
}

$aliasCount = @($aliases.Values | Sort-Object -Unique).Count
Write-Output "Prepared $processedFiles anonymized result fixtures: $($sortedNames.Length) drivers under $aliasCount aliases."
