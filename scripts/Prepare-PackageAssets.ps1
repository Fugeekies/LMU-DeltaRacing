param(
    [Parameter(Mandatory = $true)]
    [string]$SourceDirectory,

    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory
)

# Anonymizes LMU result files so they can be published as fixtures.
# - Driver names (<Name>), driver swaps (<Swap>) and TrackLimits event names (Driver="...") share one alias map, so the
#   same person gets the same "Driver NNNN" alias everywhere and in every file.
# - Stream events containing free text (contacts, chat, penalties, scoring...) are removed: the
#   application does not use them and their text cannot be anonymized reliably.
# - Names that are already aliases are kept unchanged, so the script can be re-run on fixtures
#   that were anonymized before (new names get aliases numbered after the highest existing one).

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

$driverBlockPattern = [regex]'(?s)<Driver\b[^>]*>.*?</Driver>'
$driverNamePattern = [regex]'(?s)(<Name\b[^>]*>)(.*?)(</Name>)'
$trackLimitsDriverPattern = [regex]'(<TrackLimits\b[^>]*?\bDriver=")([^"]*)(")'
$swapPattern = [regex]'(<Swap\b[^>]*>)([^<]*)(</Swap>)'
$freeTextEventPattern = [regex]'(?m)^[ \t]*<(Score|Sent|Sector|Incident|Penalty|ChatMessage)\b[^>]*>.*?</\1>[ \t]*\r?\n'
$freeTextEventTagPattern = [regex]'<(Score|Sent|Sector|Incident|Penalty|ChatMessage)\b'
$aliasPattern = [regex]'^DRIVER (\d{4,})$'

function Get-NormalizedName([string]$rawName) {
    $name = [System.Net.WebUtility]::HtmlDecode($rawName).Trim()
    if ([string]::IsNullOrWhiteSpace($name)) {
        return $null
    }

    return $name.Normalize([System.Text.NormalizationForm]::FormKC).ToUpperInvariant()
}

$names = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
foreach ($sourceFile in $sourceFiles) {
    $content = [System.IO.File]::ReadAllText($sourceFile.FullName)
    foreach ($driverBlock in $driverBlockPattern.Matches($content)) {
        $nameMatch = $driverNamePattern.Match($driverBlock.Value)
        if ($nameMatch.Success) {
            $normalizedName = Get-NormalizedName $nameMatch.Groups[2].Value
            if ($null -ne $normalizedName) {
                [void]$names.Add($normalizedName)
            }
        }
    }

    foreach ($eventMatch in @($trackLimitsDriverPattern.Matches($content)) + @($swapPattern.Matches($content))) {
        $normalizedName = Get-NormalizedName $eventMatch.Groups[2].Value
        if ($null -ne $normalizedName) {
            [void]$names.Add($normalizedName)
        }
    }
}

if ($names.Count -eq 0) {
    throw "No driver names were found in the result fixtures under: $sourcePath"
}

# Existing aliases keep their number; real names are sorted and numbered after them.
$aliases = @{}
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

function Get-Alias([string]$rawName) {
    $normalizedName = Get-NormalizedName $rawName
    if ($null -eq $normalizedName) {
        return $null
    }

    return [System.Security.SecurityElement]::Escape($aliases[$normalizedName])
}

[void](New-Item -ItemType Directory -Path $outputPath -Force)
$encoding = [System.Text.UTF8Encoding]::new($false)
$processedFiles = 0

foreach ($sourceFile in $sourceFiles) {
    $content = [System.IO.File]::ReadAllText($sourceFile.FullName)
    $anonymizedContent = $driverBlockPattern.Replace(
        $content,
        [System.Text.RegularExpressions.MatchEvaluator]{
            param($driverBlock)

            $driverNamePattern.Replace(
                $driverBlock.Value,
                [System.Text.RegularExpressions.MatchEvaluator]{
                    param($nameMatch)

                    $alias = Get-Alias $nameMatch.Groups[2].Value
                    if ($null -eq $alias) {
                        return $nameMatch.Value
                    }

                    return $nameMatch.Groups[1].Value + $alias + $nameMatch.Groups[3].Value
                },
                1
            )
        }
    )

    $replaceNameEvaluator = [System.Text.RegularExpressions.MatchEvaluator]{
        param($eventMatch)

        $alias = Get-Alias $eventMatch.Groups[2].Value
        if ($null -eq $alias) {
            return $eventMatch.Value
        }

        return $eventMatch.Groups[1].Value + $alias + $eventMatch.Groups[3].Value
    }
    $anonymizedContent = $trackLimitsDriverPattern.Replace($anonymizedContent, $replaceNameEvaluator)
    $anonymizedContent = $swapPattern.Replace($anonymizedContent, $replaceNameEvaluator)

    $anonymizedContent = $freeTextEventPattern.Replace($anonymizedContent, '')

    $destinationFile = Join-Path $outputPath $sourceFile.Name
    [System.IO.File]::WriteAllText($destinationFile, $anonymizedContent, $encoding)
    $processedFiles++
}

foreach ($destinationFile in Get-ChildItem -LiteralPath $outputPath -Filter '*.xml' -File) {
    $content = [System.IO.File]::ReadAllText($destinationFile.FullName)
    foreach ($driverBlock in $driverBlockPattern.Matches($content)) {
        $nameMatch = $driverNamePattern.Match($driverBlock.Value)
        if ($nameMatch.Success) {
            $name = [System.Net.WebUtility]::HtmlDecode($nameMatch.Groups[2].Value).Trim()
            if (-not [string]::IsNullOrWhiteSpace($name) -and $name -notmatch '^Driver \d{4,}$') {
                throw "A driver name was not anonymized in packaged fixture '$($destinationFile.Name)'."
            }
        }
    }

    foreach ($eventMatch in @($trackLimitsDriverPattern.Matches($content)) + @($swapPattern.Matches($content))) {
        $name = [System.Net.WebUtility]::HtmlDecode($eventMatch.Groups[2].Value).Trim()
        if (-not [string]::IsNullOrWhiteSpace($name) -and $name -notmatch '^Driver \d{4,}$') {
            throw "A track-limits or driver-swap name was not anonymized in packaged fixture '$($destinationFile.Name)'."
        }
    }

    if ($freeTextEventTagPattern.IsMatch($content)) {
        throw "A free-text stream event remains in packaged fixture '$($destinationFile.Name)'."
    }
}

Write-Output "Prepared $processedFiles anonymized result fixtures using $($aliases.Count) consistent driver aliases."
