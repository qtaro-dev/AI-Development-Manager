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

$publishPath = Join-Path $repositoryRoot 'artifacts/package-input/wpf-client'
$packagePath = Join-Path $repositoryRoot 'artifacts/packages/client'
$generatedPath = Join-Path $repositoryRoot 'artifacts/installer-generated'
if (Test-Path -LiteralPath $publishPath) {
    Remove-Item -LiteralPath $publishPath -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $publishPath, $packagePath, $generatedPath | Out-Null

function ConvertTo-WixId([string]$prefix, [string]$value) {
    $hash = [System.Security.Cryptography.SHA256]::HashData([System.Text.Encoding]::UTF8.GetBytes($value))
    return "$prefix$(([System.BitConverter]::ToString($hash).Replace('-', '')).Substring(0, 24))"
}

function ConvertTo-WixGuid([string]$value) {
    $bytes = [System.Security.Cryptography.MD5]::HashData([System.Text.Encoding]::UTF8.GetBytes($value))
    return ([Guid]::new($bytes)).ToString('D').ToUpperInvariant()
}

function Escape-Xml([string]$value) {
    return [System.Security.SecurityElement]::Escape($value)
}

function New-ClientFilesFragment([string]$sourceRoot, [string]$outputFile) {
    $files = Get-ChildItem -LiteralPath $sourceRoot -Recurse -File |
        Where-Object { $_.Extension -ne '.pdb' } |
        Sort-Object FullName
    $directories = @{}
    foreach ($file in $files) {
        $relativeDirectory = [System.IO.Path]::GetRelativePath($sourceRoot, $file.DirectoryName)
        if ($relativeDirectory -eq '.') { continue }
        $parts = $relativeDirectory -split '[\\/]'
        $parentId = 'CLIENTFOLDER'
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
    $lines.Add('    <DirectoryRef Id="CLIENTFOLDER">')
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
    Add-Directories 'CLIENTFOLDER' 6
    $lines.Add('    </DirectoryRef>')
    $lines.Add('  </Fragment>')
    $lines.Add('  <Fragment>')
    $lines.Add('    <ComponentGroup Id="ClientFiles" Directory="CLIENTFOLDER">')
    foreach ($file in $files) {
        $relativePath = [System.IO.Path]::GetRelativePath($sourceRoot, $file.FullName)
        $relativeDirectory = [System.IO.Path]::GetRelativePath($sourceRoot, $file.DirectoryName)
        $directoryId = if ($relativeDirectory -eq '.') { 'CLIENTFOLDER' } else { $directories[($relativeDirectory -replace '\\', '/')].Id }
        $componentId = ConvertTo-WixId 'Cmp_' $relativePath
        $fileId = ConvertTo-WixId 'File_' $relativePath
        $guid = ConvertTo-WixGuid "client-file:$relativePath"
        $lines.Add(('      <Component Id="{0}" Directory="{1}" Guid="{2}">' -f $componentId, $directoryId, $guid))
        $lines.Add(('        <RegistryValue Root="HKCU" Key="Software\AI Development Manager\Client\InstallerComponents\{0}" Name="Installed" Value="1" Type="integer" KeyPath="yes" />' -f $componentId))
        $lines.Add(('        <File Id="{0}" Source="{1}" />' -f $fileId, (Escape-Xml $file.FullName)))
        $lines.Add('      </Component>')
    }
    $lines.Add('    </ComponentGroup>')
    $lines.Add('    <ComponentGroup Id="ClientDirectoryCleanup" Directory="CLIENTROOT">')
    $cleanupDirectories = [System.Collections.Generic.List[object]]::new()
    $cleanupDirectories.Add([ordered]@{ Id = 'CLIENTROOT'; Key = 'root' })
    $cleanupDirectories.Add([ordered]@{ Id = 'CLIENTFOLDER'; Key = 'client' })
    $cleanupDirectories.Add([ordered]@{ Id = 'CLIENTUSERDATA'; Key = 'userdata' })
    foreach ($directory in $directories.Values) {
        $cleanupDirectories.Add([ordered]@{ Id = $directory.Id; Key = $directory.Id })
    }
    foreach ($directory in $cleanupDirectories) {
        $componentId = ConvertTo-WixId 'DirCmp_' $directory.Key
        $guid = ConvertTo-WixGuid "client-directory:$($directory.Id)"
        $removeId = ConvertTo-WixId 'Remove_' $directory.Key
        $lines.Add(('      <Component Id="{0}" Directory="{1}" Guid="{2}">' -f $componentId, $directory.Id, $guid))
        $lines.Add(('        <CreateFolder />'))
        $lines.Add(('        <RegistryValue Root="HKCU" Key="Software\AI Development Manager\Client\InstallerDirectories\{0}" Name="Installed" Value="1" Type="integer" KeyPath="yes" />' -f $componentId))
        $lines.Add(('        <RemoveFolder Id="{0}" On="uninstall" />' -f $removeId))
        $lines.Add('      </Component>')
    }
    $lines.Add('    </ComponentGroup>')
    $lines.Add('  </Fragment>')
    $lines.Add('</Wix>')
    $lines | Out-File -LiteralPath $outputFile -Encoding utf8
}

Push-Location $repositoryRoot
try {
    & dotnet restore '.\src\Adm.Wpf\Adm.Wpf.csproj' --runtime win-x64
    if ($LASTEXITCODE -ne 0) { throw "WPF restore failed with exit code $LASTEXITCODE" }

    & dotnet publish '.\src\Adm.Wpf\Adm.Wpf.csproj' `
        --configuration $Configuration `
        --runtime win-x64 `
        --self-contained false `
        --no-restore `
        --output $publishPath
    if ($LASTEXITCODE -ne 0) { throw "WPF publish failed with exit code $LASTEXITCODE" }

    New-ClientFilesFragment $publishPath (Join-Path $generatedPath 'ClientFiles.wxs')

    & dotnet build '.\installer\wpf-client\wpf-client.wixproj' `
        --configuration $Configuration `
        -p:ClientPublishDir=$publishPath `
        -p:GeneratedClientFiles=$(Join-Path $generatedPath 'ClientFiles.wxs') `
        -p:OutputPath=$packagePath
    if ($LASTEXITCODE -ne 0) { throw "WPF Client MSI build failed with exit code $LASTEXITCODE" }
} finally {
    Pop-Location
}

$msi = Get-ChildItem -LiteralPath $packagePath -Filter '*.msi' -File | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($null -eq $msi) { throw "WPF Client MSI was not created under $packagePath" }

$manifest = [ordered]@{
    product = 'AI Development Manager Client'
    package = $msi.Name
    architecture = 'x64'
    configuration = $Configuration
    dotnet_sdk = $sdkVersion
    sha256 = (Get-FileHash -LiteralPath $msi.FullName -Algorithm SHA256).Hash
    install_scope = 'per-user'
    runtime_prerequisite = 'Microsoft Edge WebView2 Evergreen Runtime'
    runtime_missing_action = 'Display Japanese prerequisite guidance; do not install or elevate silently.'
    server_data_policy = 'The client package does not contain or remove Server data.'
}
$manifest | ConvertTo-Json -Depth 5 | Out-File -LiteralPath (Join-Path $packagePath 'manifest.json') -Encoding utf8
Write-Output "WPF Client MSI created: $($msi.FullName)"
