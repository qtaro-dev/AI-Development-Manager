[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [string]$EvidenceRoot = 'artifacts/p1-029-webview2-offline-ui'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$sdk = (& dotnet --version).Trim()
if ($sdk -ne '10.0.302') { throw "固定SDK 10.0.302が必要です。実測値: $sdk" }

$evidence = Join-Path $repositoryRoot $EvidenceRoot
$assets = Join-Path $evidence 'assets'
$measurements = Join-Path $evidence 'measurements'
New-Item -ItemType Directory -Force -Path $evidence, $assets, $measurements | Out-Null

Push-Location $repositoryRoot
try {
    npm.cmd --prefix .\src\Adm.Web ci
    npm.cmd --prefix .\src\Adm.Web run build
    if ($LASTEXITCODE -ne 0) { throw "Web build failed with exit code $LASTEXITCODE" }
    if (Test-Path -LiteralPath $assets) { Remove-Item -LiteralPath $assets -Recurse -Force }
    Copy-Item -LiteralPath .\src\Adm.Web\dist -Destination $assets -Recurse

    # Restore is intentionally a separate prerequisite so the five-run measurement
    # is not affected by a transient machine-wide NuGetScratch lock.
    & dotnet build .\poc\p1-029-webview2-offline-ui\OfflineUiPoc.csproj --configuration $Configuration --no-restore
    if ($LASTEXITCODE -ne 0) { throw "PoC build failed with exit code $LASTEXITCODE" }
} finally {
    Pop-Location
}

$exe = Join-Path $repositoryRoot "artifacts/bin/OfflineUiPoc/$Configuration/net10.0-windows/OfflineUiPoc.exe"
if (-not (Test-Path -LiteralPath $exe)) { throw "PoC executable was not found: $exe" }

for ($run = 1; $run -le 5; $run++) {
    $measurement = Join-Path $measurements "run-$run.json"
    $arguments = @(
        "--assets=`"$assets`"",
        "--evidence=`"$evidence/run-$run`"",
        "--measurement=`"$measurement`"",
        '--auto-exit-ms=500'
    )
    $process = Start-Process -FilePath $exe -ArgumentList $arguments -WorkingDirectory $repositoryRoot -PassThru
    $watch = [System.Diagnostics.Stopwatch]::StartNew()
    Wait-Process -Id $process.Id
    $watch.Stop()
    if ($process.ExitCode -ne 0) { throw "PoC run $run failed with exit code $($process.ExitCode)" }
    [ordered]@{ run = $run; process_elapsed_ms = $watch.ElapsedMilliseconds; exit_code = $process.ExitCode } |
        ConvertTo-Json | Out-File -LiteralPath (Join-Path $measurements "run-$run-process.json") -Encoding utf8
}

Write-Output "P1-029 PoC completed. Evidence: $evidence"
