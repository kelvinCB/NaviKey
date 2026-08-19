$ErrorActionPreference = 'Stop'
$sourcePath = Join-Path $PSScriptRoot '..\NativeMouse\ModeAudio.cs'
$source = (Resolve-Path -LiteralPath $sourcePath).Path
$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('teclado-como-raton-audio-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tempRoot | Out-Null
$harnessPath = Join-Path $tempRoot 'AudioHarness.cs'
$exePath = Join-Path $tempRoot 'AudioHarness.exe'
$harness = @'
using System;
using System.Text;

static class AudioHarness
{
    static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    static void Main()
    {
        byte[] onWave = ModeAudio.CreateTone(880, 1320, 140);
        byte[] offWave = ModeAudio.CreateTone(440, 220, 180);
        Assert(onWave.Length > 44, "activation WAV has no PCM payload");
        Assert(offWave.Length > onWave.Length, "deactivation WAV should be longer");
        Assert(Encoding.ASCII.GetString(onWave, 0, 4) == "RIFF", "activation WAV has invalid RIFF header");
        Assert(Encoding.ASCII.GetString(onWave, 8, 4) == "WAVE", "activation WAV has invalid WAVE header");
        using (ModeAudio audio = new ModeAudio())
        {
            Assert(audio.Play(true), "activation cue did not complete");
            Assert(audio.Play(false), "deactivation cue did not complete");
            audio.Enabled = false;
            Assert(!audio.Play(true), "disabled audio should not play");
        }
        Console.WriteLine("ModeAudio tests passed");
    }
}
'@
[IO.File]::WriteAllText($harnessPath, $harness, [Text.Encoding]::UTF8)
$csc = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) { $csc = 'C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe' }
& $csc /nologo /target:exe /out:$exePath /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Configuration.dll $source $harnessPath
if ($LASTEXITCODE -ne 0) { throw "ModeAudio harness did not compile" }
& $exePath
if ($LASTEXITCODE -ne 0) { throw "ModeAudio harness failed" }
