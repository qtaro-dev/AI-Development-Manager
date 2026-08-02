[CmdletBinding()]
param(
    [string]$CandidatePath = 'design/openapi/adm-v1.openapi.json',
    [string]$BaselinePath = 'design/openapi/adm-v1.openapi.json'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Read-OpenApiDocument([string]$Path) {
    $resolved = (Resolve-Path -LiteralPath $Path).Path
    return Get-Content -LiteralPath $resolved -Raw | ConvertFrom-Json
}

function Get-HttpMethods($PathItem) {
    @($PathItem.PSObject.Properties.Name | Where-Object { $_ -in @('get', 'post', 'put', 'patch', 'delete', 'options', 'head', 'trace') })
}

$candidate = Read-OpenApiDocument $CandidatePath
$baseline = Read-OpenApiDocument $BaselinePath

if ($candidate.openapi -notmatch '^3\.') { throw "OpenAPI 3.x is required: $CandidatePath" }
if ([string]::IsNullOrWhiteSpace([string]$candidate.info.version)) { throw 'OpenAPI info.version is required.' }
if ($null -eq $candidate.paths) { throw 'OpenAPI paths is required.' }
if ($null -eq $candidate.paths.'/api/v1/version'.get) { throw 'GET /api/v1/version is required.' }

foreach ($baselinePathProperty in $baseline.paths.PSObject.Properties) {
    $candidatePathProperty = $candidate.paths.PSObject.Properties[$baselinePathProperty.Name]
    if ($null -eq $candidatePathProperty) { throw "Breaking change: path removed: $($baselinePathProperty.Name)" }

    foreach ($method in Get-HttpMethods $baselinePathProperty.Value) {
        if ($null -eq $candidatePathProperty.Value.PSObject.Properties[$method]) {
            throw "Breaking change: operation removed: $method $($baselinePathProperty.Name)"
        }
    }
}

Write-Output "OpenAPI contract passed: $CandidatePath"
