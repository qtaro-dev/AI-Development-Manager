[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$testRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repositoryRoot = (Resolve-Path (Join-Path $testRoot '..\..')).Path
$sourceRoot = Join-Path $repositoryRoot 'src'
$artifactRoot = Join-Path $repositoryRoot 'artifacts\bin'

$expectedProjects = @(
    'Adm.Core',
    'Adm.Application',
    'Adm.Server.Host',
    'Adm.Infrastructure.Windows',
    'Adm.Wpf'
)

$forbiddenByProject = @{
    'Adm.Core' = @('Adm.Application', 'Adm.Infrastructure.Windows', 'Adm.Server.Host', 'Adm.Wpf')
    'Adm.Application' = @('Adm.Infrastructure.Windows', 'Adm.Server.Host', 'Adm.Wpf')
}

function Get-ProjectReferences {
    param(
        [Parameter(Mandatory)]
        [string]$ProjectPath
    )

    [xml]$project = Get-Content -LiteralPath $ProjectPath -Raw
    $projectDirectory = Split-Path -Parent $ProjectPath
    $itemGroups = @($project.Project.ChildNodes | Where-Object { $_.LocalName -eq 'ItemGroup' })
    if ($itemGroups.Count -eq 0) {
        return
    }

    foreach ($itemGroup in $itemGroups) {
        if ($itemGroup.PSObject.Properties.Name -notcontains 'ProjectReference') {
            continue
        }

        foreach ($reference in @($itemGroup.ProjectReference)) {
        if ($null -eq $reference) {
            continue
        }

        $include = [string]$reference.Include
        $resolved = [System.IO.Path]::GetFullPath((Join-Path $projectDirectory $include))
        [pscustomobject]@{
            Include = $include
            ProjectName = [System.IO.Path]::GetFileNameWithoutExtension($resolved)
            ResolvedPath = $resolved
        }
        }
    }
}

function Assert-ProjectReferences {
    param(
        [Parameter(Mandatory)]
        [string]$ProjectName,
        [Parameter(Mandatory)]
        [string]$ProjectPath
    )

    $references = @(Get-ProjectReferences -ProjectPath $ProjectPath)
    foreach ($reference in $references) {
        if ($reference.Include -match '(?i)(^|[\\/])poc([\\/]|$)' -or $reference.ResolvedPath -match '(?i)[\\/]poc([\\/]|$)') {
            throw "$ProjectName has a forbidden PoC ProjectReference: $($reference.Include)"
        }

        if ($forbiddenByProject.ContainsKey($ProjectName) -and $forbiddenByProject[$ProjectName] -contains $reference.ProjectName) {
            throw "$ProjectName has a forbidden ProjectReference to $($reference.ProjectName)"
        }
    }
}

function Assert-ForbiddenNamespace {
    param(
        [Parameter(Mandatory)]
        [string]$ProjectName,
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [string[]]$SourceFiles
    )

    if (-not $forbiddenByProject.ContainsKey($ProjectName)) {
        return
    }

    $windowsNamespacePattern = '(?m)^\s*using\s+(System\.Windows|Microsoft\.Win32|Microsoft\.Web\.WebView2|Microsoft\.Extensions\.Hosting\.WindowsServices)\b'
    foreach ($sourceFile in $SourceFiles) {
        if ((Get-Content -LiteralPath $sourceFile -Raw) -match $windowsNamespacePattern) {
            throw "$ProjectName contains a forbidden Windows-specific namespace: $sourceFile"
        }
    }
}

function Assert-AssemblyReferences {
    param(
        [Parameter(Mandatory)]
        [string]$ProjectName,
        [Parameter(Mandatory)]
        [string]$TargetFramework
    )

    $assemblyPath = Join-Path $artifactRoot "$ProjectName\$Configuration\$TargetFramework\$ProjectName.dll"
    if (-not (Test-Path -LiteralPath $assemblyPath)) {
        throw "Built assembly was not found: $assemblyPath"
    }

    $assembly = [System.Reflection.Assembly]::LoadFrom($assemblyPath)
    foreach ($reference in $assembly.GetReferencedAssemblies()) {
        if ($reference.Name -match '(?i)(Poc|PoC)') {
            throw "$ProjectName has a forbidden PoC assembly reference: $($reference.Name)"
        }

        if ($ProjectName -eq 'Adm.Core' -and $reference.Name -match '^(Adm\.(Application|Infrastructure\.Windows|Server\.Host|Wpf))$') {
            throw "Adm.Core has a forbidden compiled assembly reference: $($reference.Name)"
        }

        if ($ProjectName -eq 'Adm.Application' -and $reference.Name -match '^Adm\.(Infrastructure\.Windows|Server\.Host|Wpf)$') {
            throw "Adm.Application has a forbidden compiled assembly reference: $($reference.Name)"
        }
    }
}

function Assert-IntentionalViolationDetected {
    $fixturePath = Join-Path $testRoot 'fixtures\CoreWithForbiddenReference.csproj'
    try {
        Assert-ProjectReferences -ProjectName 'Adm.Core' -ProjectPath $fixturePath
        throw 'The intentional forbidden ProjectReference fixture was not detected.'
    }
    catch [System.Management.Automation.RuntimeException] {
        if ($_.Exception.Message -notlike '*forbidden ProjectReference*') {
            throw
        }
    }

    $namespaceFixture = Join-Path $testRoot 'fixtures\CoreWithWindowsNamespace.cs'
    try {
        Assert-ForbiddenNamespace -ProjectName 'Adm.Core' -SourceFiles @($namespaceFixture)
        throw 'The intentional forbidden namespace fixture was not detected.'
    }
    catch [System.Management.Automation.RuntimeException] {
        if ($_.Exception.Message -notlike '*forbidden Windows-specific namespace*') {
            throw
        }
    }
}

$projectFiles = @{}
foreach ($projectName in $expectedProjects) {
    $projectPath = Join-Path $sourceRoot "$projectName\$projectName.csproj"
    if (-not (Test-Path -LiteralPath $projectPath)) {
        throw "Expected product project was not found: $projectPath"
    }

    $projectFiles[$projectName] = $projectPath
    Assert-ProjectReferences -ProjectName $projectName -ProjectPath $projectPath

    $sourceFiles = @(Get-ChildItem -LiteralPath (Split-Path -Parent $projectPath) -Filter '*.cs' -File -Recurse | ForEach-Object { $_.FullName })
    Assert-ForbiddenNamespace -ProjectName $projectName -SourceFiles $sourceFiles

    [xml]$project = Get-Content -LiteralPath $projectPath -Raw
    $targetFramework = @($project.Project.PropertyGroup | Where-Object { $_.PSObject.Properties.Name -contains 'TargetFramework' } | ForEach-Object { [string]$_.TargetFramework } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })[0]
    if ([string]::IsNullOrWhiteSpace($targetFramework)) {
        throw "TargetFramework was not found: $projectPath"
    }
    Assert-AssemblyReferences -ProjectName $projectName -TargetFramework $targetFramework
}

Assert-IntentionalViolationDetected
Write-Output "Architecture boundary tests passed: $($expectedProjects.Count) product projects ($Configuration)."
