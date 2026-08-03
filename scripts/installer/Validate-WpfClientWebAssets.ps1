[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)] [string]$PublishRoot,
    [Parameter(Mandatory = $true)] [string]$GeneratedFragment,
    [Parameter(Mandatory = $true)] [string]$MsiPath
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Get-RelativeWebAssetPath([string]$root, [string]$path) {
    return [System.IO.Path]::GetRelativePath((Join-Path $root 'WebAssets'), $path).Replace('\', '/')
}

function Get-FileHashes([string]$root) {
    $webRoot = Join-Path $root 'WebAssets'
    if (-not (Test-Path -LiteralPath (Join-Path $webRoot 'index.html') -PathType Leaf)) {
        throw "Web資産のエントリポイントがありません: $webRoot/index.html"
    }
    $files = Get-ChildItem -LiteralPath $webRoot -Recurse -File | Sort-Object FullName
    if ($files.Count -eq 0) { throw "WebAssetsが空です: $webRoot" }
    $result = [ordered]@{}
    foreach ($file in $files) {
        $relative = Get-RelativeWebAssetPath $root $file.FullName
        $result[$relative] = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
    }
    return $result
}

function Get-FragmentWebAssetSources([string]$fragment) {
    [xml]$xml = Get-Content -LiteralPath $fragment -Raw
    $sources = [ordered]@{}
    foreach ($file in $xml.SelectNodes('//*[local-name()="File"]')) {
        $source = [string]$file.Source
        if ($source -and ($source -match '[\\/]WebAssets[\\/]')) {
            $normalized = $source.Replace('/', '\')
            $marker = '\WebAssets\'
            $index = $normalized.IndexOf($marker, [System.StringComparison]::OrdinalIgnoreCase)
            $relative = $normalized.Substring($index + $marker.Length).Replace('\', '/')
            $sources[$relative] = $source
        }
    }
    return $sources
}

$publishHashes = Get-FileHashes $PublishRoot
$fragmentSources = Get-FragmentWebAssetSources $GeneratedFragment
$publishKeys = @($publishHashes.Keys | Sort-Object)
$fragmentKeys = @($fragmentSources.Keys | Sort-Object)
if (@(Compare-Object $publishKeys $fragmentKeys).Count -ne 0) {
    throw "Publish成果物とWiX FragmentのWebAssets構成が一致しません。Publish=$($publishKeys -join ',') Fragment=$($fragmentKeys -join ',')"
}
foreach ($relative in $publishKeys) {
    if (-not (Test-Path -LiteralPath $fragmentSources[$relative] -PathType Leaf)) {
        throw "WiX FragmentのSourceが存在しません: $($fragmentSources[$relative])"
    }
    $sourceHash = (Get-FileHash -LiteralPath $fragmentSources[$relative] -Algorithm SHA256).Hash
    if ($sourceHash -ne $publishHashes[$relative]) { throw "WiX Fragment Sourceの内容がPublishと一致しません: $relative" }
}

# Query the MSI File table directly. This verifies that the embedded MSI database
# contains every WebAssets file without starting Windows Installer or modifying
# the current user's installation.
$installer = New-Object -ComObject WindowsInstaller.Installer
$database = $installer.OpenDatabase($MsiPath, 0)
$view = $database.OpenView("SELECT FileName FROM File")
$view.Execute()
$msiFileNames = [System.Collections.Generic.List[string]]::new()
while ($record = $view.Fetch()) {
    $fileName = ([string]$record.StringData(1)).Split('|')[-1]
    $msiFileNames.Add($fileName)
}
$view.Close()
$expectedNames = @($publishKeys | ForEach-Object { [System.IO.Path]::GetFileName($_) })
foreach ($fileName in $expectedNames) {
    if (-not $msiFileNames.Contains($fileName)) { throw "MSI内部のFileテーブルにWebAssetsがありません: $fileName" }
}
if ($msiFileNames.Count -lt $publishKeys.Count) { throw 'MSI内部のWebAssetsファイル数がPublish成果物より少なくなっています。' }
Write-Output "WebAssets validation passed: $($publishKeys.Count) files; Publish, WiX Fragment and MSI File table match."
