[CmdletBinding()]
param(
    [string]$EvidenceRoot = 'artifacts/ci-evidence/ui-runtime-compatibility'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$evidencePath = Join-Path $repositoryRoot $EvidenceRoot
$outputPath = Join-Path $repositoryRoot 'output/phase1/ui-runtime-compatibility'
New-Item -ItemType Directory -Force -Path $evidencePath, $outputPath | Out-Null

function Invoke-Recorded {
    param([string]$FilePath, [string[]]$Arguments, [string]$OutputFile, [string]$WorkingDirectory = $repositoryRoot)
    Push-Location $WorkingDirectory
    try {
        $result = & $FilePath @Arguments 2>&1
        $exitCode = $LASTEXITCODE
        $result | Out-File -LiteralPath $OutputFile -Encoding utf8
        if ($exitCode -ne 0) { throw "$FilePath failed with exit code $exitCode. See $OutputFile" }
    } finally { Pop-Location }
}

function Get-VersionOrUnknown([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) { return $null }
    return (Get-Item -LiteralPath $Path).VersionInfo.ProductVersion
}

$edgePath = 'C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe'
$chromePath = 'C:\Program Files\Google\Chrome\Application\chrome.exe'
$webViewVersions = @(
    foreach ($root in @('HKLM:\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients', 'HKCU:\Software\Microsoft\EdgeUpdate\Clients')) {
        if (Test-Path -LiteralPath $root) {
            foreach ($key in Get-ChildItem -LiteralPath $root) {
                $property = Get-ItemProperty -LiteralPath $key.PSPath
                if ($property.name -eq 'Microsoft Edge WebView2 Runtime') { $property.pv }
            }
        }
    }
)
$inventory = [ordered]@{
    collected_at_utc = [DateTime]::UtcNow.ToString('o')
    os = [System.Environment]::OSVersion.VersionString
    architecture = $env:PROCESSOR_ARCHITECTURE
    dotnet_sdk = (& dotnet --version).Trim()
    node = (& node --version).Trim()
    npm = (& npm.cmd --version).Trim()
    edge = [ordered]@{ path = $edgePath; version = Get-VersionOrUnknown $edgePath; available = (Test-Path -LiteralPath $edgePath) }
    chrome = [ordered]@{ path = $chromePath; version = Get-VersionOrUnknown $chromePath; available = (Test-Path -LiteralPath $chromePath) }
    webview2 = [ordered]@{ versions = @($webViewVersions | Sort-Object -Unique); available = ($webViewVersions.Count -gt 0) }
    dpi_matrix = @(
        [ordered]@{ percent = 100; device_scale_factor = 1; mode = 'browser-emulated' },
        [ordered]@{ percent = 125; device_scale_factor = 1.25; mode = 'browser-emulated' },
        [ordered]@{ percent = 150; device_scale_factor = 1.5; mode = 'browser-emulated' },
        [ordered]@{ percent = 200; device_scale_factor = 2; mode = 'browser-emulated' }
    )
}
$inventory | ConvertTo-Json -Depth 8 | Out-File -LiteralPath (Join-Path $evidencePath 'runtime-inventory.json') -Encoding utf8
$inventory | ConvertTo-Json -Depth 8 | Out-File -LiteralPath (Join-Path $outputPath 'runtime-inventory.json') -Encoding utf8
if (-not $inventory.edge.available -or -not $inventory.chrome.available -or -not $inventory.webview2.available) { throw 'Edge, Chrome, and WebView2 Runtime must be installed before compatibility testing.' }

$e2eDirectory = Join-Path $repositoryRoot 'tests/Adm.Web.E2E'
Invoke-Recorded npm.cmd @('ci') (Join-Path $evidencePath 'npm-ci.log') $e2eDirectory
Invoke-Recorded npm.cmd @('run', 'test:compat') (Join-Path $evidencePath 'playwright-compatibility.log') $e2eDirectory

$wpfExe = Join-Path $repositoryRoot 'artifacts/bin/Adm.Wpf/Debug/net10.0-windows/Adm.Wpf.exe'
if (-not (Test-Path -LiteralPath $wpfExe)) { Invoke-Recorded dotnet @('build', 'AIDevelopmentManager.sln', '--configuration', 'Debug', '--no-restore') (Join-Path $evidencePath 'dotnet-build-debug.log') }
$serverDll = Join-Path $repositoryRoot 'artifacts/bin/Adm.Server.Host/Debug/net10.0/Adm.Server.Host.dll'
$serverProcess = Start-Process -FilePath 'dotnet' -ArgumentList $serverDll, '--Server:Port=5199' -PassThru
Start-Sleep -Seconds 2
$wpfProcess = Start-Process -FilePath $wpfExe -ArgumentList '--server-url=http://127.0.0.1:5199/' -PassThru
try {
    Start-Sleep -Seconds 5
    if ($null -eq (Get-Process -Id $wpfProcess.Id -ErrorAction SilentlyContinue)) { throw 'Adm.Wpf exited before the WebView2 startup smoke completed.' }
    [ordered]@{ started = $true; process_id = $wpfProcess.Id; webview2_runtime_versions = @($webViewVersions | Sort-Object -Unique); server_origin = 'http://127.0.0.1:5199/'; result = 'process remained active after startup smoke' } | ConvertTo-Json -Depth 5 | Out-File -LiteralPath (Join-Path $evidencePath 'webview2-smoke.json') -Encoding utf8
} finally {
    if (-not $wpfProcess.HasExited) { Stop-Process -Id $wpfProcess.Id -Force }
    if (-not $serverProcess.HasExited) { Stop-Process -Id $serverProcess.Id -Force }
}

@"
# P1-023 UI Runtime Compatibility Result

- Edge／Chrome: Playwright実ブラウザで実行
- WebView2: Windows WPF Shellの起動スモークとRuntime検出
- DPI: 100／125／150／200%をPlaywrightのdeviceScaleFactorで再現。Windows表示倍率そのものの変更は行わない
- Server: `http://127.0.0.1:5199/`、テスト中のみ自動起動・終了
- 証拠: `artifacts/ci-evidence/ui-runtime-compatibility/`

Playwrightの全テスト、WPF起動スモーク、重大なconsole／HTTPエラー検査が成功した場合に合格とする。IMEの実入力、OS表示倍率変更、WebView2内の詳細DOM操作は、P1-027で目視確認する。
"@ | Out-File -LiteralPath (Join-Path $outputPath 'README.md') -Encoding utf8
Write-Output "UI runtime compatibility passed. Evidence: $evidencePath"
