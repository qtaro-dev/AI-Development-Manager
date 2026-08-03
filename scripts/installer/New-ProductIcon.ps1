[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$directory = Split-Path -Parent $OutputPath
New-Item -ItemType Directory -Force -Path $directory | Out-Null

# Deterministic 32x32 ICO matching src/Adm.Web/public/favicon.svg.
$width = 32
$height = 32
$xorBytes = $width * $height * 4
$andStride = [int](($width + 31) / 32) * 4
$andBytes = $andStride * $height
$imageBytes = 40 + $xorBytes + $andBytes

$stream = [System.IO.File]::Open($OutputPath, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write, [System.IO.FileShare]::None)
$writer = $null
try {
    $writer = [System.IO.BinaryWriter]::new($stream)
    $writer.Write([uint16]0)
    $writer.Write([uint16]1)
    $writer.Write([uint16]1)
    $writer.Write([byte]$width)
    $writer.Write([byte]$height)
    $writer.Write([byte]0)
    $writer.Write([byte]0)
    $writer.Write([uint16]1)
    $writer.Write([uint16]32)
    $writer.Write([uint32]$imageBytes)
    $writer.Write([uint32]22)

    $writer.Write([uint32]40)
    $writer.Write([int32]$width)
    $writer.Write([int32]($height * 2))
    $writer.Write([uint16]1)
    $writer.Write([uint16]32)
    $writer.Write([uint32]0)
    $writer.Write([uint32]$xorBytes)
    $writer.Write([int32]0)
    $writer.Write([int32]0)
    $writer.Write([uint32]0)
    $writer.Write([uint32]0)

    # Bottom-up BGRA pixels: blue background and white A glyph.
    for ($y = $height - 1; $y -ge 0; $y--) {
        for ($x = 0; $x -lt $width; $x++) {
            $inGlyph = (($x - 8) -ge 0 -and ($x - 8) -lt 16 -and (($y - 9) -ge 0 -and ($y - 9) -lt 18)) -and
                (($y - 9) -le 4 -or ($x -lt 13 -and $x -gt 9) -or ($x -ge 14 -and $x -lt 18) -or ($y -ge 17 -and $y -le 19 -and $x -ge 12 -and $x -lt 20))
            if ($inGlyph) {
                $writer.Write([byte]255); $writer.Write([byte]255); $writer.Write([byte]255); $writer.Write([byte]255)
            } else {
                $writer.Write([byte]246); $writer.Write([byte]130); $writer.Write([byte]59); $writer.Write([byte]255)
            }
        }
    }

    for ($i = 0; $i -lt $andBytes; $i++) { $writer.Write([byte]0) }
    $writer.Flush()
} finally {
    if ($null -ne $writer) { $writer.Dispose() } else { $stream.Dispose() }
}

Write-Output "Product icon created: $OutputPath"
