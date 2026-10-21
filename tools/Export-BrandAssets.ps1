# Run in Windows PowerShell with -STA. The SVG path is the single geometry source.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationCore, WindowsBase
$repo = Split-Path $PSScriptRoot -Parent
$assets = Join-Path $repo 'docs/assets'
[xml]$svg = Get-Content (Join-Path $assets 'mark.svg')
$geometry = [System.Windows.Media.Geometry]::Parse($svg.svg.path.d)
$blue = [System.Windows.Media.BrushConverter]::new().ConvertFromString('#2347D9')
$cream = [System.Windows.Media.BrushConverter]::new().ConvertFromString('#F6F0DF')
$ink = [System.Windows.Media.BrushConverter]::new().ConvertFromString('#171A21')

function Render-Mark([int]$size, $brush, [bool]$tile) {
    $visual = [System.Windows.Media.DrawingVisual]::new()
    $drawing = $visual.RenderOpen()
    if ($tile) {
        $drawing.DrawRoundedRectangle($blue, $null, [System.Windows.Rect]::new(0, 0, $size, $size), $size * .16, $size * .16)
    }
    $scale = $size * $(if ($tile) { .72 } else { .90 }) / 334
    $drawing.PushTransform([System.Windows.Media.TranslateTransform]::new(($size - 334 * $scale) / 2, ($size - 312 * $scale) / 2))
    $drawing.PushTransform([System.Windows.Media.ScaleTransform]::new($scale, $scale))
    $drawing.DrawGeometry($brush, $null, $geometry)
    $drawing.Pop(); $drawing.Pop(); $drawing.Close()
    $bitmap = [System.Windows.Media.Imaging.RenderTargetBitmap]::new($size, $size, 96, 96, [System.Windows.Media.PixelFormats]::Pbgra32)
    $bitmap.Render($visual)
    $encoder = [System.Windows.Media.Imaging.PngBitmapEncoder]::new()
    $encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
    $stream = [System.IO.MemoryStream]::new()
    $encoder.Save($stream)
    $bytes = $stream.ToArray()
    $stream.Dispose()
    return ,$bytes
}

function Export-Icon([string]$path, $brush, [bool]$tile) {
    $sizes = @(16, 20, 24, 32, 48, 64, 128, 256)
    $frames = @($sizes | ForEach-Object { ,(Render-Mark $_ $brush $tile) })
    $stream = [System.IO.File]::Create($path)
    $writer = [System.IO.BinaryWriter]::new($stream)
    try {
        $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$sizes.Count)
        $offset = 6 + 16 * $sizes.Count
        for ($i = 0; $i -lt $sizes.Count; $i++) {
            $dimension = [byte]$(if ($sizes[$i] -eq 256) { 0 } else { $sizes[$i] })
            $writer.Write($dimension); $writer.Write($dimension)
            $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]32)
            $writer.Write([uint32]$frames[$i].Length); $writer.Write([uint32]$offset)
            $offset += $frames[$i].Length
        }
        foreach ($frame in $frames) { $writer.Write([byte[]]$frame) }
    } finally { $writer.Dispose(); $stream.Dispose() }
    $decoded = [System.Windows.Media.Imaging.IconBitmapDecoder]::new([uri]$path, [System.Windows.Media.Imaging.BitmapCreateOptions]::None, [System.Windows.Media.Imaging.BitmapCacheOption]::OnLoad)
    if ($decoded.Frames.Count -ne $sizes.Count) { throw "Invalid icon frames: $path" }
}

[System.IO.File]::WriteAllBytes((Join-Path $assets 'logo.png'), (Render-Mark 512 $blue $false))
[System.IO.File]::WriteAllBytes((Join-Path $assets 'app-icon.png'), (Render-Mark 512 $cream $true))
[System.IO.File]::WriteAllBytes((Join-Path $assets 'monochrome.png'), (Render-Mark 512 $ink $false))
Export-Icon (Join-Path $repo 'SpotiFloat/icon.ico') $cream $true
Export-Icon (Join-Path $repo 'SpotiFloat/tray-dark.ico') $cream $false
Export-Icon (Join-Path $repo 'SpotiFloat/tray-light.ico') $ink $false
Write-Output 'PASS: brand PNGs and eight-frame application/light/dark tray icons exported and decoded.'
