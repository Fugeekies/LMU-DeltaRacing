param(
    [Parameter(Mandatory = $true)]
    [string]$SourceDirectory,

    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory
)

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
$names = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)

foreach ($sourceFile in $sourceFiles) {
    $content = [System.IO.File]::ReadAllText($sourceFile.FullName)
    foreach ($driverBlock in $driverBlockPattern.Matches($content)) {
        $nameMatch = $driverNamePattern.Match($driverBlock.Value)
        if ($nameMatch.Success) {
            $name = [System.Net.WebUtility]::HtmlDecode($nameMatch.Groups[2].Value).Trim()
            if (-not [string]::IsNullOrWhiteSpace($name)) {
                $normalizedName = $name.Normalize([System.Text.NormalizationForm]::FormKC).ToUpperInvariant()
                [void]$names.Add($normalizedName)
            }
        }
    }
}

if ($names.Count -eq 0) {
    throw "No driver names were found in the result fixtures under: $sourcePath"
}

$sortedNames = [string[]]@($names)
[System.Array]::Sort($sortedNames, [System.StringComparer]::Ordinal)
$aliases = @{}
for ($index = 0; $index -lt $sortedNames.Length; $index++) {
    $aliases[$sortedNames[$index]] = 'Driver {0:D4}' -f ($index + 1)
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

                    $name = [System.Net.WebUtility]::HtmlDecode($nameMatch.Groups[2].Value).Trim()
                    if ([string]::IsNullOrWhiteSpace($name)) {
                        return $nameMatch.Value
                    }

                    $normalizedName = $name.Normalize([System.Text.NormalizationForm]::FormKC).ToUpperInvariant()
                    $alias = [System.Security.SecurityElement]::Escape($aliases[$normalizedName])
                    return $nameMatch.Groups[1].Value + $alias + $nameMatch.Groups[3].Value
                },
                1
            )
        }
    )

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
}

Write-Output "Prepared $processedFiles anonymized result fixtures using $($aliases.Count) consistent driver aliases."
