$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$programPath = Join-Path $root 'NativeMouse\Program.cs'
$program = Get-Content -LiteralPath $programPath -Raw

function Assert-Contains([string] $token, [string] $message) {
    if ($program.IndexOf($token, [StringComparison]::Ordinal) -lt 0) { throw $message }
}

Assert-Contains 'sealed class ThemePalette' 'Appearance themes need a shared palette type.'
Assert-Contains 'void ApplyTheme(ThemePalette palette)' 'Appearance themes need an application method.'
Assert-Contains 'void SetTheme(ThemePalette palette)' 'Appearance selection needs a theme setter.'
Assert-Contains 'Command Center' 'The original Command Center theme must remain available.'
Assert-Contains 'Aurora' 'An Aurora theme must be available.'
Assert-Contains 'Alto contraste' 'A high-contrast theme must be available.'
Assert-Contains 'themeButton.Click' 'Theme choices must be actionable controls.'

Write-Output 'Appearance theme contract tests passed'
