param(
    [Parameter(Mandatory = $true)]
    [string]$Source,
    [Parameter(Mandatory = $true)]
    [string]$Destination
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ($env:OS -ne "Windows_NT") {
    throw "Brand icon generation requires Windows."
}

$sourcePath = [IO.Path]::GetFullPath($Source)
$destinationPath = [IO.Path]::GetFullPath($Destination)
if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
    throw "Logo source not found: $sourcePath"
}

$destinationDirectory = Split-Path -Parent $destinationPath
New-Item -ItemType Directory -Force -Path $destinationDirectory | Out-Null

Add-Type -AssemblyName PresentationCore
Add-Type -AssemblyName WindowsBase

$sourceUri = [Uri]::new($sourcePath)
$decoder = [System.Windows.Media.Imaging.BitmapDecoder]::Create(
    $sourceUri,
    [System.Windows.Media.Imaging.BitmapCreateOptions]::PreservePixelFormat,
    [System.Windows.Media.Imaging.BitmapCacheOption]::OnLoad)
$sourceBitmap = $decoder.Frames[0]
$sizes = @(16, 24, 32, 48, 64, 128, 256)
$images = New-Object System.Collections.Generic.List[object]

foreach ($size in $sizes) {
    $scale = [Math]::Min($size / $sourceBitmap.PixelWidth, $size / $sourceBitmap.PixelHeight)
    $width = [Math]::Max(1, [Math]::Round($sourceBitmap.PixelWidth * $scale))
    $height = [Math]::Max(1, [Math]::Round($sourceBitmap.PixelHeight * $scale))
    $left = ($size - $width) / 2
    $top = ($size - $height) / 2

    $visual = New-Object System.Windows.Media.DrawingVisual
    $drawing = $visual.RenderOpen()
    $drawing.DrawRectangle(
        [System.Windows.Media.Brushes]::Transparent,
        $null,
        [System.Windows.Rect]::new(0, 0, $size, $size))
    $drawing.DrawImage(
        $sourceBitmap,
        [System.Windows.Rect]::new($left, $top, $width, $height))
    $drawing.Close()

    $rendered = [System.Windows.Media.Imaging.RenderTargetBitmap]::new(
        $size,
        $size,
        96,
        96,
        [System.Windows.Media.PixelFormats]::Pbgra32)
    $rendered.Render($visual)

    $encoder = [System.Windows.Media.Imaging.PngBitmapEncoder]::new()
    $encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($rendered))
    $stream = [IO.MemoryStream]::new()
    $encoder.Save($stream)
    $images.Add([PSCustomObject]@{
        Size = $size
        Bytes = $stream.ToArray()
    })
    $stream.Dispose()
}

$file = [IO.File]::Create($destinationPath)
$writer = [IO.BinaryWriter]::new($file)
try {
    $writer.Write([UInt16]0)
    $writer.Write([UInt16]1)
    $writer.Write([UInt16]$images.Count)

    $offset = 6 + (16 * $images.Count)
    foreach ($image in $images) {
        $dimension = if ($image.Size -ge 256) { [byte]0 } else { [byte]$image.Size }
        $writer.Write($dimension)
        $writer.Write($dimension)
        $writer.Write([byte]0)
        $writer.Write([byte]0)
        $writer.Write([UInt16]1)
        $writer.Write([UInt16]32)
        $writer.Write([UInt32]$image.Bytes.Length)
        $writer.Write([UInt32]$offset)
        $offset += $image.Bytes.Length
    }

    foreach ($image in $images) {
        $writer.Write([byte[]]$image.Bytes)
    }
}
finally {
    $writer.Dispose()
    $file.Dispose()
}

Write-Host "Generated Windows icon: $destinationPath"
