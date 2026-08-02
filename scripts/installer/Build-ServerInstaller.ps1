[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$sdkVersion = (& dotnet --version).Trim()
if ($sdkVersion -ne '10.0.302') {
    throw "固定SDK 10.0.302が必要です。実測値: $sdkVersion"
}

$publishPath = Join-Path $repositoryRoot 'artifacts/package-input/server'
$packagePath = Join-Path $repositoryRoot 'artifacts/packages/server'
$generatedPath = Join-Path $repositoryRoot 'artifacts/installer-generated'
if (Test-Path -LiteralPath $publishPath) {
    Remove-Item -LiteralPath $publishPath -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $publishPath, $packagePath, $generatedPath | Out-Null

function ConvertTo-WixId([string]$prefix, [string]$value) {
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($value)
    $hash = [System.Security.Cryptography.SHA256]::HashData($bytes)
    return "$prefix$(([System.BitConverter]::ToString($hash).Replace('-', '')).Substring(0, 24))"
}

function ConvertTo-WixGuid([string]$value) {
    $bytes = [System.Security.Cryptography.MD5]::HashData([System.Text.Encoding]::UTF8.GetBytes($value))
    $guid = [Guid]::new($bytes)
    return $guid.ToString('D').ToUpperInvariant()
}

function Escape-Xml([string]$value) {
    return [System.Security.SecurityElement]::Escape($value)
}

function New-ServerFilesFragment([string]$sourceRoot, [string]$outputFile) {
    $files = Get-ChildItem -LiteralPath $sourceRoot -Recurse -File |
        Where-Object { $_.Name -ne 'Adm.Server.Host.exe' -and $_.Extension -ne '.pdb' } |
        Sort-Object FullName
    $directories = @{}
    foreach ($file in $files) {
        $relativeDirectory = [System.IO.Path]::GetRelativePath($sourceRoot, $file.DirectoryName)
        if ($relativeDirectory -eq '.') { continue }
        $parts = $relativeDirectory -split '[\\/]'
        $parentId = 'SERVERFOLDER'
        $pathParts = @()
        foreach ($part in $parts) {
            $pathParts += $part
            $directoryKey = ($pathParts -join '/')
            if (-not $directories.ContainsKey($directoryKey)) {
                $directories[$directoryKey] = [ordered]@{ Id = ConvertTo-WixId 'Dir_' $directoryKey; Parent = $parentId; Name = $part }
            }
            $parentId = $directories[$directoryKey].Id
        }
    }

    $lines = [System.Collections.Generic.List[string]]::new()
    $lines.Add('<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">')
    $lines.Add('  <Fragment>')
    $lines.Add('    <DirectoryRef Id="SERVERFOLDER">')
    $children = @{}
    foreach ($directory in $directories.Values) {
        if (-not $children.ContainsKey($directory.Parent)) { $children[$directory.Parent] = @() }
        $children[$directory.Parent] += $directory
    }
    function Add-Directories([string]$parentId, [int]$indent) {
        if (-not $children.ContainsKey($parentId)) { return }
        foreach ($directory in $children[$parentId] | Sort-Object Name) {
            $padding = (' ' * $indent)
            $lines.Add(('{0}<Directory Id="{1}" Name="{2}">' -f $padding, $directory.Id, (Escape-Xml $directory.Name)))
            Add-Directories $directory.Id ($indent + 2)
            $lines.Add("$padding</Directory>")
        }
    }
    Add-Directories 'SERVERFOLDER' 6
    $lines.Add('    </DirectoryRef>')
    $lines.Add('  </Fragment>')
    $lines.Add('  <Fragment>')
    $lines.Add('    <ComponentGroup Id="ServerFiles" Directory="SERVERFOLDER">')
    foreach ($file in $files) {
        $relativePath = [System.IO.Path]::GetRelativePath($sourceRoot, $file.FullName)
        $relativeDirectory = [System.IO.Path]::GetRelativePath($sourceRoot, $file.DirectoryName)
        $directoryId = if ($relativeDirectory -eq '.') { 'SERVERFOLDER' } else { $directories[($relativeDirectory -replace '\\', '/')].Id }
        $componentId = ConvertTo-WixId 'Cmp_' $relativePath
        $fileId = ConvertTo-WixId 'File_' $relativePath
        $guid = ConvertTo-WixGuid "server-file:$relativePath"
        $lines.Add(('      <Component Id="{0}" Directory="{1}" Guid="{2}">' -f $componentId, $directoryId, $guid))
        $lines.Add(('        <File Id="{0}" Source="{1}" KeyPath="yes" />' -f $fileId, (Escape-Xml $file.FullName)))
        $lines.Add('      </Component>')
    }
    $lines.Add('    </ComponentGroup>')
    $lines.Add('  </Fragment>')
    $lines.Add('</Wix>')
    $lines | Out-File -LiteralPath $outputFile -Encoding utf8
}

Push-Location $repositoryRoot
try {
    & dotnet restore '.\src\Adm.Server.Host\Adm.Server.Host.csproj' --runtime win-x64
    if ($LASTEXITCODE -ne 0) { throw "Server restore failed with exit code $LASTEXITCODE" }

    & dotnet publish '.\src\Adm.Server.Host\Adm.Server.Host.csproj' `
        --configuration $Configuration `
        --runtime win-x64 `
        --self-contained false `
        --no-restore `
        --output $publishPath
    if ($LASTEXITCODE -ne 0) { throw "Server publish failed with exit code $LASTEXITCODE" }

    New-ServerFilesFragment $publishPath (Join-Path $generatedPath 'ServerFiles.wxs')

    & dotnet build '.\installer\server\server.wixproj' `
        --configuration $Configuration `
        -p:ServerPublishDir=$publishPath `
        -p:GeneratedServerFiles=$(Join-Path $generatedPath 'ServerFiles.wxs') `
        -p:OutputPath=$packagePath
    if ($LASTEXITCODE -ne 0) { throw "Server MSI build failed with exit code $LASTEXITCODE" }
} finally {
    Pop-Location
}

$msi = Get-ChildItem -LiteralPath $packagePath -Filter '*.msi' -File | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($null -eq $msi) { throw "Server MSI was not created under $packagePath" }

$manifest = [ordered]@{
    product = 'AI Development Manager Server'
    package = $msi.Name
    architecture = 'x64'
    configuration = $Configuration
    dotnet_sdk = $sdkVersion
    sha256 = (Get-FileHash -LiteralPath $msi.FullName -Algorithm SHA256).Hash
    service_name = 'AIDevelopmentManagerServer'
    service_account = 'LocalService'
    install_scope = 'per-machine'
    data_policy = 'Config, Logs, and Data directories are retained by uninstall.'
}
$manifest | ConvertTo-Json -Depth 5 | Out-File -LiteralPath (Join-Path $packagePath 'manifest.json') -Encoding utf8
Write-Output "Server MSI created: $($msi.FullName)"
