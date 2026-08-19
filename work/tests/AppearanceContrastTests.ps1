$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$installedPath = Join-Path $env:LOCALAPPDATA 'TecladoComoRaton\native\TecladoComoRatonNative.exe'
if (-not (Test-Path -LiteralPath $installedPath)) { throw "Installed executable missing: $installedPath" }

function Get-EffectiveBackColor($control) {
    $current = $control
    while ($null -ne $current) {
        if ($current.BackColor -ne [Drawing.Color]::Transparent) { return $current.BackColor }
        $current = $current.Parent
    }
    return [Drawing.Color]::White
}
function Get-RelativeLuminance([Drawing.Color] $color) {
    $channels = @($color.R, $color.G, $color.B)
    $linear = @()
    foreach ($channel in $channels) {
        $value = $channel / 255.0
        $linear += $(if ($value -le 0.03928) { $value / 12.92 } else { [Math]::Pow(($value + 0.055) / 1.055, 2.4) })
    }
    return (0.2126 * $linear[0]) + (0.7152 * $linear[1]) + (0.0722 * $linear[2])
}
function Get-ContrastRatio([Drawing.Color] $foreground, [Drawing.Color] $background) {
    $first = Get-RelativeLuminance $foreground
    $second = Get-RelativeLuminance $background
    $light = [Math]::Max($first, $second)
    $dark = [Math]::Min($first, $second)
    return ($light + 0.05) / ($dark + 0.05)
}
function Get-TextControls($control) {
    $items = @()
    if ($control.Text -and $control.Text.Trim().Length -gt 0) { $items += $control }
    foreach ($child in $control.Controls) { $items += Get-TextControls $child }
    return $items
}

$assembly = [Reflection.Assembly]::LoadFrom($installedPath)
$flags = [Reflection.BindingFlags]::Instance -bor [Reflection.BindingFlags]::Static -bor [Reflection.BindingFlags]::NonPublic -bor [Reflection.BindingFlags]::Public
$formType = $assembly.GetType('MainForm', $true)
$paletteType = $assembly.GetType('ThemePalette', $true)
$form = [Activator]::CreateInstance($formType, $true)
try {
    $setTheme = $formType.GetMethod('SetTheme', $flags)
    $showSection = $formType.GetMethod('ShowSection', $flags)
    $themeNames = @('CommandCenter', 'Aurora', 'HighContrast', 'Claro')
    foreach ($themeName in $themeNames) {
        $palette = $paletteType.GetField($themeName, $flags).GetValue($null)
        $setTheme.Invoke($form, @($palette)) | Out-Null
        for ($section = 0; $section -le 5; $section++) {
            $showSection.Invoke($form, @($section)) | Out-Null
            foreach ($control in (Get-TextControls $form)) {
                $ratio = Get-ContrastRatio $control.ForeColor (Get-EffectiveBackColor $control)
                if ($ratio -lt 3.0) { throw "Unreadable text in theme '$themeName', section $section, control '$($control.Text.Trim())': contrast $([Math]::Round($ratio, 2))." }
            }
        }
    }
    Write-Output 'Appearance contrast tests passed'
}
finally {
    $form.Close()
    $form.Dispose()
}
