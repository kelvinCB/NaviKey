$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$programPath = Join-Path $root 'NativeMouse\Program.cs'
$program = Get-Content -LiteralPath $programPath -Raw
$readme = Get-Content -LiteralPath (Join-Path $root '..\README.md') -Raw
$product = Get-Content -LiteralPath (Join-Path $root '..\PRODUCT.md') -Raw

if ($program.IndexOf('NaviKey', [StringComparison]::Ordinal) -lt 0) { throw 'Native UI must use the NaviKey product name.' }
if ($program.IndexOf('Teclado como ratón', [StringComparison]::Ordinal) -ge 0) { throw 'Native UI still exposes the old product name.' }
if ($program.IndexOf('github.com/kelvinCB/NaviKey', [StringComparison]::Ordinal) -lt 0) { throw 'Native About screen must link to the renamed repository.' }
if ($readme.IndexOf('# NaviKey', [StringComparison]::Ordinal) -ne 0) { throw 'README must use the NaviKey product name.' }
if ($product.IndexOf('# NaviKey', [StringComparison]::Ordinal) -ne 0) { throw 'Product document must use the NaviKey product name.' }

$outputExe = Join-Path $root '..\outputs\NaviKey.exe'
$iconPath = Join-Path $root '..\outputs\assets\NaviKey-C.ico'
$pngPath = Join-Path $root '..\outputs\assets\NaviKey-C.png'
foreach ($path in @($outputExe, $iconPath, $pngPath)) {
    if (-not (Test-Path -LiteralPath $path)) { throw "NaviKey production asset missing: $path" }
}

$installedPath = Join-Path $env:LOCALAPPDATA 'NaviKey\native\NaviKey.exe'
if (-not (Test-Path -LiteralPath $installedPath)) { throw "NaviKey installed executable missing: $installedPath" }
if (@(Get-Process NaviKey -ErrorAction SilentlyContinue).Count -ne 1) { throw 'Expected one running NaviKey instance.' }

Write-Output 'Branding contract tests passed'
