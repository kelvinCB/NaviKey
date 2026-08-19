$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$installedPath = Join-Path $env:LOCALAPPDATA 'NaviKey\native\NaviKey.exe'
if (-not (Test-Path -LiteralPath $installedPath)) { throw "Installed executable missing: $installedPath" }

$assembly = [Reflection.Assembly]::LoadFrom($installedPath)
$flags = [Reflection.BindingFlags]::Instance -bor [Reflection.BindingFlags]::Static -bor [Reflection.BindingFlags]::NonPublic -bor [Reflection.BindingFlags]::Public
$formType = $assembly.GetType('MainForm', $true)
$paletteType = $assembly.GetType('ThemePalette', $true)
$form = [Activator]::CreateInstance($formType, $true)
try {
    $showSection = $formType.GetMethod('ShowSection', $flags)
    $showSection.Invoke($form, @(3)) | Out-Null
    $form.Show()
    [Windows.Forms.Application]::DoEvents()

    $buttons = $formType.GetField('themeButtons', $flags).GetValue($form)
    if ($buttons.Length -ne 4) { throw "Expected four appearance choices, found $($buttons.Length)." }
    $aurora = $paletteType.GetField('Aurora', $flags).GetValue($null)
    $buttons[1].PerformClick()
    [Windows.Forms.Application]::DoEvents()
    if ($form.BackColor.ToArgb() -ne $aurora.Background.ToArgb()) { throw 'Selecting Aurora did not update the window palette.' }
    if ($buttons[1].Text.IndexOf('✓', [StringComparison]::Ordinal) -lt 0) { throw 'The selected appearance was not marked active.' }
    Write-Output 'Appearance theme behavior tests passed'
}
finally {
    $form.Close()
    $form.Dispose()
}
