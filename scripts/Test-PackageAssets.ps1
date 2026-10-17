param(
    [string]$ResultsDirectory = 'assets/Results'
)

$ErrorActionPreference = 'Stop'
$resultsPath = [System.IO.Path]::GetFullPath($ResultsDirectory)

if (-not (Test-Path -LiteralPath $resultsPath -PathType Container)) {
    throw "The anonymized package results directory does not exist: $resultsPath"
}

$resultFiles = @(Get-ChildItem -LiteralPath $resultsPath -Filter '*.xml' -File | Sort-Object Name)
if ($resultFiles.Count -eq 0) {
    throw "No packaged result XML files were found in: $resultsPath"
}

$driverAliases = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
foreach ($resultFile in $resultFiles) {
    $document = [System.Xml.XmlDocument]::new()
    $document.Load($resultFile.FullName)

    foreach ($driverName in $document.SelectNodes("//*[local-name()='Driver']/*[local-name()='Name']")) {
        $alias = $driverName.InnerText.Trim()
        if ($alias -notmatch '^Driver \d{4,}$') {
            throw "Non-anonymized driver name '$alias' found in package fixture '$($resultFile.Name)'."
        }

        [void]$driverAliases.Add($alias)
    }
}

if ($driverAliases.Count -eq 0) {
    throw "No driver aliases were found in the packaged result XML files under: $resultsPath"
}

$expectedAliases = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
for ($index = 1; $index -le $driverAliases.Count; $index++) {
    [void]$expectedAliases.Add('Driver {0:D4}' -f $index)
}

if (-not $driverAliases.SetEquals($expectedAliases)) {
    throw "Packaged driver aliases must form a consistent sequence from Driver 0001 to Driver $($driverAliases.Count.ToString('D4'))."
}

Write-Output "Verified $($resultFiles.Count) packaged result XML files and $($driverAliases.Count) consistent anonymized driver aliases."
