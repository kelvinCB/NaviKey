using System;
using System.IO;
using System.Media;

sealed class ModeAudio : IDisposable
{
    readonly MemoryStream onStream;
    readonly MemoryStream offStream;
    readonly SoundPlayer onPlayer;
    readonly SoundPlayer offPlayer;
    bool disposed;

    public bool Enabled { get; set; }

    public ModeAudio()
    {
        Enabled = true;
        onStream = new MemoryStream(CreateTone(880, 1320, 140));
        offStream = new MemoryStream(CreateTone(440, 220, 180));
        onPlayer = new SoundPlayer(onStream);
        offPlayer = new SoundPlayer(offStream);
        onPlayer.Load();
        offPlayer.Load();
    }

    public bool Play(bool enabled)
    {
        if (!Enabled || disposed) return false;
        try
        {
            (enabled ? onPlayer : offPlayer).PlaySync();
            return true;
        }
        catch
        {
            try
            {
                (enabled ? SystemSounds.Asterisk : SystemSounds.Beep).Play();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public static byte[] CreateTone(int firstFrequency, int secondFrequency, int durationMs)
    {
        const int sampleRate = 44100, bits = 16, channels = 1;
        int samples = sampleRate * durationMs / 1000, dataSize = samples * 2;
        using (MemoryStream stream = new MemoryStream(44 + dataSize))
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            writer.Write(new byte[] { 0x52, 0x49, 0x46, 0x46 });
            writer.Write(36 + dataSize);
            writer.Write(new byte[] { 0x57, 0x41, 0x56, 0x45 });
            writer.Write(new byte[] { 0x66, 0x6D, 0x74, 0x20 });
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)channels);
            writer.Write(sampleRate);
            writer.Write(sampleRate * channels * bits / 8);
            writer.Write((short)(channels * bits / 8));
            writer.Write((short)bits);
            writer.Write(new byte[] { 0x64, 0x61, 0x74, 0x61 });
            writer.Write(dataSize);
            for (int i = 0; i < samples; i++)
            {
                double phase = (double)i / samples;
                int frequency = phase < 0.5 ? firstFrequency : secondFrequency;
                short sample = (short)(Math.Sin(2 * Math.PI * frequency * i / sampleRate) * 9000);
                writer.Write(sample);
            }
            return stream.ToArray();
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        onPlayer.Dispose();
        offPlayer.Dispose();
        onStream.Dispose();
        offStream.Dispose();
    }
}
