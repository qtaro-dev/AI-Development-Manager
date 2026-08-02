[CmdletBinding()]
param(
    [string]$EvidenceRoot = 'artifacts/ci-evidence'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
Set-Location $repositoryRoot
$evidencePath = Join-Path $repositoryRoot $EvidenceRoot
New-Item -ItemType Directory -Force -Path $evidencePath | Out-Null

function Invoke-RecordedCommand {
    param(
        [Parameter(Mandatory)] [string]$FilePath,
        [Parameter(Mandatory)] [string[]]$Arguments,
        [Parameter(Mandatory)] [string]$OutputFile
    )

    $output = & $FilePath @Arguments 2>&1
    $exitCode = $LASTEXITCODE
    $output | Out-File -LiteralPath $OutputFile -Encoding utf8
    if ($exitCode -ne 0) {
        throw "$FilePath failed with exit code $exitCode. See $OutputFile"
    }
}

function Assert-NoForbiddenTrackedFiles {
    $tracked = @(git -c "safe.directory=$repositoryRoot" ls-files)
    $tracked | Out-File (Join-Path $evidencePath 'tracked-files.txt') -Encoding utf8
    $forbidden = @($tracked | Where-Object {
        $_ -match '(^|/)(bin|obj|node_modules|artifacts|TestResults|coverage)(/|$)' -or
        $_ -match '(^|/)(\.env|secrets\.json)(\.|$)' -or
        $_ -match '\.(pfx|p12|key|pem|dmp|sqlite|db)$'
    })
    if ($forbidden.Count -gt 0) {
        $forbidden | Out-File (Join-Path $evidencePath 'forbidden-tracked-files.txt') -Encoding utf8
        throw 'Forbidden generated, runtime, or secret files are tracked.'
    }
}

function New-DependencyEvidence {
    $packageRows = [System.Collections.Generic.List[object]]::new()
    $projects = @(Get-ChildItem -Path (Join-Path $repositoryRoot 'src'), (Join-Path $repositoryRoot 'tests') -Filter '*.csproj' -File -Recurse | Where-Object { $_.FullName -notmatch '[\\/]fixtures[\\/]' })

    foreach ($projectFile in $projects) {
        $assetsPath = Join-Path $repositoryRoot "artifacts/obj/$($projectFile.BaseName)/project.assets.json"
        if (-not (Test-Path -LiteralPath $assetsPath)) {
            throw "Assets file not found for $($projectFile.BaseName): $assetsPath"
        }

        $assets = Get-Content -LiteralPath $assetsPath -Raw | ConvertFrom-Json
        foreach ($library in $assets.libraries.PSObject.Properties) {
            if ($library.Value.type -ne 'package') {
                continue
            }

            $packageId, $version = $library.Name -split '/', 2
            $packageRoot = Join-Path (Join-Path $env:USERPROFILE '.nuget\packages') "$($packageId.ToLowerInvariant())\$($version.ToLowerInvariant())"
            $nuspec = Get-ChildItem -LiteralPath $packageRoot -Filter '*.nuspec' -File -ErrorAction SilentlyContinue | Select-Object -First 1
            $license = 'UNKNOWN'
            if ($null -ne $nuspec) {
                [xml]$nuspecXml = Get-Content -LiteralPath $nuspec.FullName -Raw
                $expression = $nuspecXml.SelectSingleNode("//*[local-name()='licenseExpression']")
                $licenseNode = $nuspecXml.SelectSingleNode("//*[local-name()='license']")
                if ($null -ne $expression -and -not [string]::IsNullOrWhiteSpace($expression.InnerText)) {
                    $license = $expression.InnerText
                } elseif ($null -ne $licenseNode -and $null -ne $licenseNode.type) {
                    $license = "file:$($licenseNode.InnerText)"
                }
            }

            $packageRows.Add([pscustomobject]@{
                    project = $projectFile.BaseName
                    id = $packageId
                    version = $version
                    license = $license
                })
        }
    }

    $packageRows | ConvertTo-Json -Depth 5 | Out-File (Join-Path $evidencePath 'licenses.json') -Encoding utf8
    $components = @($packageRows | ForEach-Object {
            [pscustomobject]@{
                type = 'library'
                'bom-ref' = "$($_.id)@$($_.version)"
                name = $_.id
                version = $_.version
                licenses = @([pscustomobject]@{ license = [pscustomobject]@{ id = $_.license } })
            }
        })
    [pscustomobject]@{
        bomFormat = 'CycloneDX'
        specVersion = '1.5'
        serialNumber = "urn:uuid:$([guid]::NewGuid())"
        version = 1
        metadata = [pscustomobject]@{ timestamp = [DateTime]::UtcNow.ToString('o'); tools = @([pscustomobject]@{ vendor = 'AI Development Manager'; name = 'Invoke-QualityGates.ps1' }) }
        components = $components
    } | ConvertTo-Json -Depth 10 | Out-File (Join-Path $evidencePath 'sbom.cdx.json') -Encoding utf8
}

Invoke-RecordedCommand dotnet @('--version') (Join-Path $evidencePath 'dotnet-version.log')
Invoke-RecordedCommand node @('--version') (Join-Path $evidencePath 'node-version.log')
Invoke-RecordedCommand npm.cmd @('--version') (Join-Path $evidencePath 'npm-version.log')
Assert-NoForbiddenTrackedFiles
Invoke-RecordedCommand dotnet @('restore', 'AIDevelopmentManager.sln') (Join-Path $evidencePath 'dotnet-restore.log')
Invoke-RecordedCommand dotnet @('build', 'AIDevelopmentManager.sln', '--configuration', 'Debug', '--no-restore') (Join-Path $evidencePath 'dotnet-build-debug.log')
Invoke-RecordedCommand dotnet @('build', 'AIDevelopmentManager.sln', '--configuration', 'Release', '--no-restore') (Join-Path $evidencePath 'dotnet-build-release.log')

New-Item -ItemType Directory -Force -Path (Join-Path $evidencePath 'test-results') | Out-Null
Invoke-RecordedCommand dotnet @('test', 'AIDevelopmentManager.sln', '--configuration', 'Debug', '--no-build', '--no-restore', '--logger', 'trx', '--results-directory', (Join-Path $evidencePath 'test-results')) (Join-Path $evidencePath 'dotnet-test.log')
Invoke-RecordedCommand dotnet @('test', 'AIDevelopmentManager.sln', '--configuration', 'Release', '--no-build', '--no-restore', '--logger', 'trx', '--results-directory', (Join-Path $evidencePath 'test-results')) (Join-Path $evidencePath 'dotnet-test-release.log')
Invoke-RecordedCommand pwsh @('-NoProfile', '-File', '.\tests\Adm.Architecture.Tests\Invoke-ArchitectureBoundaryTests.ps1', '-Configuration', 'Debug') (Join-Path $evidencePath 'architecture-debug.log')
Invoke-RecordedCommand pwsh @('-NoProfile', '-File', '.\tests\Adm.Architecture.Tests\Invoke-ArchitectureBoundaryTests.ps1', '-Configuration', 'Release') (Join-Path $evidencePath 'architecture-release.log')
Invoke-RecordedCommand pwsh @('-NoProfile', '-File', '.\scripts\api\Validate-OpenApiContract.ps1') (Join-Path $evidencePath 'openapi-contract.log')

$auditFiles = @()
foreach ($projectFile in @(Get-ChildItem -Path (Join-Path $repositoryRoot 'src'), (Join-Path $repositoryRoot 'tests') -Filter '*.csproj' -File -Recurse | Where-Object { $_.FullName -notmatch '[\\/]fixtures[\\/]' })) {
    $safeName = $projectFile.BaseName
    $auditFile = Join-Path $evidencePath "nuget-vulnerabilities-$safeName.log"
    Invoke-RecordedCommand dotnet @('list', $projectFile.FullName, 'package', '--vulnerable', '--include-transitive', '--no-restore') $auditFile
    $auditFiles += $auditFile
}
$highFindings = @($auditFiles | ForEach-Object { Select-String -Path $_ -Pattern '(?i)High|Critical' })
if ($highFindings.Count -gt 0) {
    $highFindings | Out-File (Join-Path $evidencePath 'high-critical-vulnerabilities.txt') -Encoding utf8
    throw 'High or Critical NuGet vulnerability findings were detected.'
}

$webPackage = Join-Path $repositoryRoot 'src/Adm.Web/package.json'
if (Test-Path -LiteralPath $webPackage) {
    Invoke-RecordedCommand npm.cmd @('--prefix', 'src/Adm.Web', 'ci') (Join-Path $evidencePath 'npm-ci.log')
    Invoke-RecordedCommand npm.cmd @('--prefix', 'src/Adm.Web', 'audit', '--audit-level=high', '--json') (Join-Path $evidencePath 'npm-audit.json')
    Invoke-RecordedCommand npm.cmd @('--prefix', 'src/Adm.Web', 'run', 'typecheck') (Join-Path $evidencePath 'npm-typecheck.log')
    Invoke-RecordedCommand npm.cmd @('--prefix', 'src/Adm.Web', 'run', 'lint') (Join-Path $evidencePath 'npm-lint.log')
    Invoke-RecordedCommand npm.cmd @('--prefix', 'src/Adm.Web', 'run', 'format:check') (Join-Path $evidencePath 'npm-format-check.log')
    Invoke-RecordedCommand npm.cmd @('--prefix', 'src/Adm.Web', 'run', 'build') (Join-Path $evidencePath 'npm-build.log')
    Invoke-RecordedCommand npm.cmd @('--prefix', 'src/Adm.Web', 'run', 'verify:bundle') (Join-Path $evidencePath 'npm-bundle-validation.log')
    Invoke-RecordedCommand npm.cmd @('--prefix', 'src/Adm.Web', 'run', 'test', '--if-present') (Join-Path $evidencePath 'npm-test.log')
} else {
    'src/Adm.Web/package.json is not present; Web product foundation is scheduled for P1-013.' | Out-File (Join-Path $evidencePath 'web-not-present.txt') -Encoding utf8
}

New-DependencyEvidence
Write-Output "Quality gates passed. Evidence: $evidencePath"
