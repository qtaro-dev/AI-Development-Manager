[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [string]$OutputPath = 'artifacts/package-input/wpf-client'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$sdkVersion = (& dotnet --version).Trim()
if ($sdkVersion -ne '10.0.302') {
    throw "固定SDK 10.0.302が必要です。実測値: $sdkVersion"
}

$resolvedOutputPath = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $OutputPath))
$nugetPackagesPath = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot 'artifacts/nuget-packages/wpf-client'))
$nugetScratchPath = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot 'artifacts/nuget-scratch'))
if (Test-Path -LiteralPath $resolvedOutputPath) {
    Remove-Item -LiteralPath $resolvedOutputPath -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $resolvedOutputPath | Out-Null
New-Item -ItemType Directory -Force -Path $nugetScratchPath | Out-Null
$env:NUGET_SCRATCH = $nugetScratchPath

Push-Location $repositoryRoot
try {
    & dotnet restore '.\src\Adm.Wpf\Adm.Wpf.csproj' --runtime win-x64 --disable-parallel --configfile '.\eng\NuGet.ClientPublish.config' --packages $nugetPackagesPath
    if ($LASTEXITCODE -ne 0) { throw "WPF restore failed with exit code $LASTEXITCODE" }

    & dotnet publish '.\src\Adm.Wpf\Adm.Wpf.csproj' `
        --configuration $Configuration `
        --runtime win-x64 `
        --self-contained true `
        -p:PublishSingleFile=false `
        -p:PublishTrimmed=false `
        -p:DebugType=None `
        -p:DebugSymbols=false `
        -p:RestorePackagesPath=$nugetPackagesPath `
        --no-restore `
        --output $resolvedOutputPath
    if ($LASTEXITCODE -ne 0) { throw "WPF self-contained publish failed with exit code $LASTEXITCODE" }
} finally {
    Pop-Location
}

$executable = Join-Path $resolvedOutputPath 'Adm.Wpf.exe'
if (-not (Test-Path -LiteralPath $executable)) { throw "Self-contained executable was not created: $executable" }
$runtimeConfig = Join-Path $resolvedOutputPath 'Adm.Wpf.runtimeconfig.json'
if (-not (Test-Path -LiteralPath $runtimeConfig)) { throw "runtimeconfig.json was not created: $runtimeConfig" }
$runtimeConfigText = Get-Content -LiteralPath $runtimeConfig -Raw
if ($runtimeConfigText -match '"framework"\s*:') {
    throw 'Framework-dependent runtimeconfig detected in Self-contained publish output.'
}
$includedFrameworks = [regex]::Match($runtimeConfigText, '"includedFrameworks"\s*:')
if (-not $includedFrameworks.Success) {
    throw 'Self-contained runtimeconfig did not declare includedFrameworks.'
}
$forbiddenDebugFiles = @(Get-ChildItem -LiteralPath $resolvedOutputPath -Recurse -File | Where-Object { $_.Extension -eq '.pdb' })
if ($forbiddenDebugFiles.Count -gt 0) {
    throw 'Debug symbols or XML documentation were included in the release publish output.'
}

$files = @(Get-ChildItem -LiteralPath $resolvedOutputPath -Recurse -File | Sort-Object FullName | ForEach-Object {
    $relative = [System.IO.Path]::GetRelativePath($resolvedOutputPath, $_.FullName).Replace('\', '/')
    [ordered]@{ path = $relative; size = $_.Length; sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant() }
})
$totalSize = [int64]0
foreach ($fileEntry in $files) { $totalSize += [int64]$fileEntry.size }
$props = Get-Content -LiteralPath (Join-Path $repositoryRoot 'Directory.Build.props') -Raw
$productVersion = ([regex]::Match($props, '<ProductVersion[^>]*>([^<]+)</ProductVersion>')).Groups[1].Value
$buildNumber = ([regex]::Match($props, '<BuildNumber[^>]*>([^<]+)</BuildNumber>')).Groups[1].Value
$project = Get-Content -LiteralPath (Join-Path $repositoryRoot 'src\Adm.Wpf\Adm.Wpf.csproj') -Raw
$targetFramework = ([regex]::Match($project, '<TargetFramework>([^<]+)</TargetFramework>')).Groups[1].Value
$manifest = [ordered]@{
    product = 'AI Development Manager Client'; product_version = $productVersion; build_number = $buildNumber
    configuration = $Configuration; target_framework = $targetFramework; runtime = 'Microsoft.NETCore.App 10.0.x included'
    dotnet_sdk = $sdkVersion; rid = 'win-x64'; self_contained = $true; publish_single_file = $false; publish_trimmed = $false
    webview2_runtime = 'Microsoft Edge WebView2 Evergreen Runtime remains an independent Windows prerequisite.'
    total_size_bytes = $totalSize; files = $files
}
$manifest | ConvertTo-Json -Depth 8 | Out-File -LiteralPath (Join-Path $resolvedOutputPath 'publish-manifest.json') -Encoding utf8
$bomFiles = @($files | ForEach-Object { [ordered]@{ type = 'file'; 'bom-ref' = "file:$($_.path)"; name = $_.path; hashes = @([ordered]@{ alg = 'SHA-256'; content = $_.sha256 }) } })
$sbom = [ordered]@{
    bomFormat = 'CycloneDX'; specVersion = '1.5'; version = 1
    metadata = [ordered]@{ timestamp = [DateTime]::UtcNow.ToString('o'); component = [ordered]@{ type = 'application'; name = 'AI Development Manager Client'; version = "$productVersion+build$buildNumber" }; properties = @([ordered]@{ name = 'dotnet.sdk'; value = $sdkVersion }, [ordered]@{ name = 'runtime.identifier'; value = 'win-x64' }, [ordered]@{ name = 'self.contained'; value = 'true' }) }
    components = $bomFiles
}
$sbom | ConvertTo-Json -Depth 10 | Out-File -LiteralPath (Join-Path $resolvedOutputPath 'sbom.cdx.json') -Encoding utf8
Write-Output "Self-contained WPF Client published: $resolvedOutputPath"
