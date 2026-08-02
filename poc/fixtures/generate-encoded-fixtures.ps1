param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot 'markdown/edge')
)

$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$japaneseTitle = [string]([char]0x6587) + [char]0x5B57 + [char]0x30B3 + [char]0x30FC + [char]0x30C9 + [char]0x78BA + [char]0x8A8D
$japaneseBody = [string]([char]0x65E5) + [char]0x672C + [char]0x8A9E + ' encoding fixture'
$content = @(
    '---',
    'document_type: note',
    'schema_version: 1',
    ('title: ' + $japaneseTitle),
    '---',
    '',
    $japaneseBody
) -join [Environment]::NewLine

$utf8Bom = New-Object System.Text.UTF8Encoding($true)
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'encoding-utf8-bom.md'), $content, $utf8Bom)

$codePagesAssembly = Join-Path $PSHOME 'System.Text.Encoding.CodePages.dll'
[Reflection.Assembly]::LoadFrom($codePagesAssembly) | Out-Null
[Text.Encoding]::RegisterProvider([Text.CodePagesEncodingProvider]::Instance)
$shiftJis = [Text.Encoding]::GetEncoding(932)
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'encoding-shift-jis.md'), $content, $shiftJis)

Write-Output "Generated UTF-8 BOM and Shift_JIS fixtures in $OutputDirectory"
