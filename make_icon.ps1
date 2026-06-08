# 将 PNG 转换为 ICO 文件
# 用法：把你的图片保存为 source.png，放在同目录，然后运行此脚本

param(
    [string]$SourcePng = "$PSScriptRoot\source.png",
    [string]$DestIco  = "$PSScriptRoot\src\MedicalProgress.App\app.ico"
)

Add-Type -AssemblyName System.Drawing

if (-not (Test-Path $SourcePng)) {
    Write-Error "找不到源图片：$SourcePng`n请把你的图片保存为 source.png 放在此脚本同目录。"
    exit 1
}

$src = [System.Drawing.Image]::FromFile($SourcePng)

# 生成 256x256 和 48x48 和 32x32 和 16x16 四种尺寸
$sizes = @(256, 48, 32, 16)
$bitmaps = foreach ($sz in $sizes) {
    $bmp = New-Object System.Drawing.Bitmap($sz, $sz)
    $g   = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.DrawImage($src, 0, 0, $sz, $sz)
    $g.Dispose()
    $bmp
}
$src.Dispose()

# 手工写 ICO 二进制格式
$ms = New-Object System.IO.MemoryStream

# 每个 PNG 图像先编码到 byte[]
$pngBytes = foreach ($bmp in $bitmaps) {
    $tmp = New-Object System.IO.MemoryStream
    $bmp.Save($tmp, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    ,$tmp.ToArray()
}

$count  = $sizes.Count
$headerSize = 6
$dirEntrySize = 16
$imageOffset = $headerSize + $dirEntrySize * $count

$writer = New-Object System.IO.BinaryWriter($ms)

# ICONDIR header
$writer.Write([uint16]0)       # reserved
$writer.Write([uint16]1)       # type: ICO
$writer.Write([uint16]$count)

# ICONDIRENTRY for each image
$offset = $imageOffset
for ($i = 0; $i -lt $count; $i++) {
    $sz  = $sizes[$i]
    $len = $pngBytes[$i].Length
    $writer.Write([byte]($sz -eq 256 ? 0 : $sz))  # width  (0 means 256)
    $writer.Write([byte]($sz -eq 256 ? 0 : $sz))  # height
    $writer.Write([byte]0)                          # color count
    $writer.Write([byte]0)                          # reserved
    $writer.Write([uint16]1)                        # color planes
    $writer.Write([uint16]32)                       # bits per pixel
    $writer.Write([uint32]$len)
    $writer.Write([uint32]$offset)
    $offset += $len
}

# image data
foreach ($bytes in $pngBytes) {
    $writer.Write($bytes)
}

$writer.Flush()

$dir = Split-Path $DestIco
if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir | Out-Null }
[System.IO.File]::WriteAllBytes($DestIco, $ms.ToArray())

Write-Host "图标已生成：$DestIco"
