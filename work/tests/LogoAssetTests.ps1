$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$assetRoot = Join-Path $root '..\outputs\assets'
$pngPath = Join-Path $assetRoot 'NaviKey-C.png'
$icoPath = Join-Path $assetRoot 'NaviKey-C.ico'
$programPath = Join-Path $root 'NativeMouse\Program.cs'

if (-not (Test-Path -LiteralPath $pngPath)) { throw "Logo C PNG missing: $pngPath" }
if (-not (Test-Path -LiteralPath $icoPath)) { throw "Logo C ICO missing: $icoPath" }

$program = Get-Content -LiteralPath $programPath -Raw
if ($program.IndexOf('Icon.ExtractAssociatedIcon', [StringComparison]::Ordinal) -lt 0) {
    throw 'Main window must load the embedded Logo C icon.'
}

$bytes = [IO.File]::ReadAllBytes($icoPath)
if ($bytes.Length -lt 22) { throw 'Logo C ICO is too small to be valid.' }
if ($bytes[0] -ne 0 -or $bytes[1] -ne 0 -or $bytes[2] -ne 1 -or $bytes[3] -ne 0) {
    throw 'Logo C ICO header is invalid.'
}
$imageCount = [BitConverter]::ToUInt16($bytes, 4)
if ($imageCount -lt 4) { throw "Logo C ICO should contain multiple Windows sizes, found $imageCount." }

$installedPath = Join-Path $env:LOCALAPPDATA 'NaviKey\native\NaviKey.exe'
if (-not (Test-Path -LiteralPath $installedPath)) { throw "Installed executable missing: $installedPath" }

Write-Output 'Logo asset contract tests passed'
