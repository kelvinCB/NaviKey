$sourcePath = Join-Path $PSScriptRoot '..\NativeMouse\Program.cs'
$source = Get-Content -LiteralPath $sourcePath -Raw

$requiredTokens = @(
    'new Size(980, 700)',
    'Color.FromArgb(4, 14, 30)',
    'Centro',
    'Velocidad del puntero',
    'Accesos rápidos de teclado',
    'ShowSection',
    'Ajustes de accesibilidad',
    'sectionOverlay',
    'navButtons',
    'ensureCursorOnResume'
)

foreach ($token in $requiredTokens) {
    if ($source.IndexOf($token, [StringComparison]::Ordinal) -lt 0) {
        throw "UI design contract missing token: $token"
    }
}

function Get-PanelTop([string] $variableName) {
    $match = [Regex]::Match($source, "var $variableName = CreateSurface\(Surface,\s*\d+,\s*(\d+),")
    if (-not $match.Success) { throw "UI layout contract missing panel: $variableName" }
    return [int]$match.Groups[1].Value
}

$quickTop = Get-PanelTop 'quickPanel'
$speedTop = Get-PanelTop 'speedPanel'
$curveTop = Get-PanelTop 'curvePanel'
if ($quickTop -ge $speedTop -or $quickTop -ge $curveTop) {
    throw "Keyboard shortcuts panel must be above pointer speed and progressive acceleration panels."
}

Write-Output 'UI design contract passed'
