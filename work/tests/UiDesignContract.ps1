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

Write-Output 'UI design contract passed'
