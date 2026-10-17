# Render the source vector into the Windows application/package icon assets.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationCore, WindowsBase
$assets = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../Assets'))
[xml]$source = Get-Content -LiteralPath (Join-Path $assets 'FieldSyncMark.svg') -Raw
$watermark = $source.OuterXml.Replace('fill="url(#brand)"', 'fill="#808080"')
[IO.File]::WriteAllText((Join-Path $assets 'FieldSyncWatermark.svg'), $watermark)
$visual = [Windows.Media.DrawingVisual]::new()
$context = $visual.RenderOpen()
$plate = [Windows.Media.SolidColorBrush]::new([Windows.Media.ColorConverter]::ConvertFromString('#07111F'))
$context.DrawRoundedRectangle($plate, $null, [Windows.Rect]::new(0,0,512,512), 100,100)
$transform = [Windows.Media.TransformGroup]::new()
$transform.Children.Add([Windows.Media.ScaleTransform]::new(2.55,2.20))
$transform.Children.Add([Windows.Media.TranslateTransform]::new(1,22))
$context.PushTransform($transform)
$brush = [Windows.Media.LinearGradientBrush]::new([Windows.Media.ColorConverter]::ConvertFromString('#00DCEB'), [Windows.Media.ColorConverter]::ConvertFromString('#0755E8'), 25)
foreach ($path in $source.svg.g.path) { $context.DrawGeometry($brush, $null, [Windows.Media.Geometry]::Parse($path.d)) }
foreach ($circle in $source.svg.g.circle) { $context.DrawEllipse($brush, $null, [Windows.Point]::new([double]$circle.cx,[double]$circle.cy), [double]$circle.r, [double]$circle.r) }
$context.Pop()
$context.Close()
$bitmap = [Windows.Media.Imaging.RenderTargetBitmap]::new(512,512,96,96,[Windows.Media.PixelFormats]::Pbgra32)
$bitmap.Render($visual)
$encoder = [Windows.Media.Imaging.PngBitmapEncoder]::new()
$encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
$stream = [IO.File]::Create((Join-Path $assets 'BrandAppIcon.png'))
try { $encoder.Save($stream) } finally { $stream.Dispose() }
# ICO conversion keeps the source vector's rendered artwork at multiple sizes.
python -c "from PIL import Image; from pathlib import Path; p=Path(r'$assets'); im=Image.open(p/'BrandAppIcon.png'); im.save(p/'AppIcon.ico',sizes=[(16,16),(24,24),(32,32),(48,48),(64,64),(128,128),(256,256)])"
if ($LASTEXITCODE -ne 0) { throw 'ICO export failed.' }
