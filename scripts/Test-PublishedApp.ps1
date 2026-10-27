param(
    [Parameter(Mandatory = $true)]
    [string]$PublishDirectory,

    [ValidateRange(5, 120)]
    [int]$StartupTimeoutSeconds = 30
)

$ErrorActionPreference = 'Stop'
$publishPath = [System.IO.Path]::GetFullPath($PublishDirectory)
$executablePath = Join-Path $publishPath 'LMU-DeltaRacing.UI.exe'

if (-not (Test-Path -LiteralPath $executablePath -PathType Leaf)) {
    throw "The self-contained dashboard executable was not found: $executablePath"
}

$externalAssemblies = @(Get-ChildItem -LiteralPath $publishPath -Filter '*.dll' -File -Recurse)
if ($externalAssemblies.Count -gt 0) {
    $assemblyNames = $externalAssemblies | ForEach-Object { $_.FullName }
    throw "The published application still requires external assemblies:`n$($assemblyNames -join "`n")"
}

$fixtureDirectory = Join-Path $publishPath 'TestAssets\Results'
$fixtureFiles = @(Get-ChildItem -LiteralPath $fixtureDirectory -Filter '*.xml' -File -ErrorAction Stop)
if ($fixtureFiles.Count -eq 0) {
    throw "No packaged fallback result fixtures were found in: $fixtureDirectory"
}

$arguments = @(
    '--LMU:OpenBrowser=false',
    "--LMU:ResultsDirectory=$fixtureDirectory"
)
$server = Start-Process `
    -FilePath $executablePath `
    -ArgumentList $arguments `
    -WorkingDirectory $publishPath `
    -PassThru

try {
    $deadline = [DateTime]::UtcNow.AddSeconds($StartupTimeoutSeconds)
    $listener = $null

    while ([DateTime]::UtcNow -lt $deadline) {
        $server.Refresh()
        if ($server.HasExited) {
            throw "The published dashboard exited during startup with code $($server.ExitCode)."
        }

        $listener = @(
            Get-NetTCPConnection -State Listen -OwningProcess $server.Id -ErrorAction SilentlyContinue |
                Where-Object { $_.LocalAddress -eq '127.0.0.1' }
        ) | Select-Object -First 1

        if ($null -ne $listener) {
            break
        }

        Start-Sleep -Milliseconds 250
    }

    if ($null -eq $listener) {
        throw "The published dashboard did not open a loopback listener within $StartupTimeoutSeconds seconds."
    }

    # The startup loading screen replaces the whole layout until the initial import is over, so the
    # home page is requested again until the navigation (and its "Mes courses" link) is rendered.
    $loadingScreen = 'class="startup-loading"'
    do {
        $response = Invoke-WebRequest `
            -Uri "http://127.0.0.1:$($listener.LocalPort)/" `
            -UseBasicParsing `
            -SkipHttpErrorCheck `
            -TimeoutSec 10
        if ($response.StatusCode -ne 200) {
            throw "The published dashboard returned HTTP $($response.StatusCode) instead of 200."
        }

        $isLoading = $response.Content -match [regex]::Escape($loadingScreen)
        if ($isLoading) {
            Start-Sleep -Milliseconds 500
        }
    } while ($isLoading -and [DateTime]::UtcNow -lt $deadline)

    if ($isLoading) {
        throw "The published dashboard still showed its startup loading screen after $StartupTimeoutSeconds seconds."
    }
    if ($response.Content -notmatch 'Mes courses') {
        throw "The published dashboard did not render its expected home page."
    }
    if ($response.Content -match '<h1[^>]*>\s*(Une erreur est survenue|The dashboard encountered an error)\s*</h1>') {
        throw "The published dashboard rendered its error page instead of the home page."
    }

    Write-Output "Published dashboard launched without external DLLs and returned HTTP 200."
}
finally {
    $server.Refresh()
    if (-not $server.HasExited) {
        Stop-Process -Id $server.Id -Force
        [void]$server.WaitForExit(5000)
    }
}
