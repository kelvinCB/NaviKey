$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$programPath = Join-Path $root 'NativeMouse\Program.cs'
$audioPath = Join-Path $root 'NativeMouse\ModeAudio.cs'
$manifestPath = Join-Path $root 'NativeMouse\TecladoComoRaton.manifest'
$program = Get-Content -LiteralPath $programPath -Raw
$audio = Get-Content -LiteralPath $audioPath -Raw
$manifest = Get-Content -LiteralPath $manifestPath -Raw

function Assert-Contains([string] $text, [string] $token, [string] $message) {
    if ($text.IndexOf($token, [StringComparison]::Ordinal) -lt 0) { throw $message }
}

$requiredProgramTokens = @(
    'VK_X && ctrl && alt && down',
    'SetWindowsHookEx',
    'UnhookWindowsHookEx',
    'WM_KEYDOWN',
    'WM_KEYUP',
    'IsMovementKey',
    'SetMovementKey',
    'MOUSEEVENTF_WHEEL',
    'MOUSEEVENTF_HWHEEL',
    'VK_Z || key == VK_NUMPAD1',
    'VK_OEM_PERIOD || key == VK_NUMPAD3',
    'VK_BROWSER_BACK',
    'VK_BROWSER_FORWARD',
    'VK_BACK || key == VK_DELETE || key == VK_ESCAPE',
    'PowerModeChanged',
    'SessionSwitch',
    'EnsureCursorVisible',
    'ShowSection',
    'CreateNavButton',
    'modeAudio',
    'PlayModeSound',
    'BeginInvoke'
)
foreach ($token in $requiredProgramTokens) {
    Assert-Contains $program $token "Program contract missing: $token"
}

foreach ($token in @('CreateTone', 'PlaySync', 'SystemSounds.Asterisk', 'SystemSounds.Beep', 'Enabled', 'IDisposable')) {
    Assert-Contains $audio $token "Audio contract missing: $token"
}

Assert-Contains $manifest 'requestedExecutionLevel level="requireAdministrator"' 'Manifest must request administrator execution.'

$csc = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) { $csc = 'C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe' }
$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('teclado-como-raton-build-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tempRoot | Out-Null
$exePath = Join-Path $tempRoot 'TecladoComoRatonNative.exe'
$manifestCopy = Join-Path $tempRoot 'TecladoComoRaton.manifest'
Copy-Item -LiteralPath $manifestPath -Destination $manifestCopy
& $csc /nologo /target:winexe /optimize+ /out:$exePath /win32manifest:$manifestCopy /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Configuration.dll $programPath $audioPath
if ($LASTEXITCODE -ne 0 -or -not (Test-Path $exePath)) { throw 'Native application build contract failed.' }

$installedPath = Join-Path $env:LOCALAPPDATA 'TecladoComoRaton\native\TecladoComoRatonNative.exe'
if (-not (Test-Path $installedPath)) { throw "Installed executable missing: $installedPath" }
$instances = @(Get-Process TecladoComoRatonNative -ErrorAction SilentlyContinue)
if ($instances.Count -ne 1) { throw "Expected one running instance, found $($instances.Count)." }

Write-Output 'System contract tests passed'
