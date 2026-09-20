[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$SourcePath,

    [Parameter(Mandatory)]
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$source = [System.Drawing.Bitmap]::FromFile((Resolve-Path -LiteralPath $SourcePath))
[System.IO.Directory]::CreateDirectory($OutputDirectory) | Out-Null

function Write-Asset {
    param(
        [Parameter(Mandatory)] [string]$Name,
        [Parameter(Mandatory)] [int]$Width,
        [Parameter(Mandatory)] [int]$Height
    )

    $bitmap = [System.Drawing.Bitmap]::new(
        $Width,
        $Height,
        [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)

    try {
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        try {
            $graphics.Clear([System.Drawing.Color]::Black)
            $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
            $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
            $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality

            $scale = [Math]::Min($Width / $source.Width, $Height / $source.Height)
            $drawWidth = [int][Math]::Round($source.Width * $scale)
            $drawHeight = [int][Math]::Round($source.Height * $scale)
            $x = [int](($Width - $drawWidth) / 2)
            $y = [int](($Height - $drawHeight) / 2)
            $graphics.DrawImage($source, $x, $y, $drawWidth, $drawHeight)
        }
        finally {
            $graphics.Dispose()
        }

        $outputPath = Join-Path $OutputDirectory $Name
        $bitmap.Save($outputPath, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $bitmap.Dispose()
    }
}

try {
    Write-Asset -Name 'StoreLogo.png' -Width 50 -Height 50
    Write-Asset -Name 'Square44x44Logo.png' -Width 44 -Height 44
    Write-Asset -Name 'Square150x150Logo.png' -Width 150 -Height 150
    Write-Asset -Name 'Wide310x150Logo.png' -Width 310 -Height 150
    Write-Asset -Name 'SplashScreen.png' -Width 620 -Height 300
}
finally {
    $source.Dispose()
}
