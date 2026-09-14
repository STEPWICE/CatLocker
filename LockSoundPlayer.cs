using System.Media;

namespace CatLocker;

internal static class LockSoundPlayer
{
    private static readonly byte[] LockSound = CreateToneSequence(
        new[]
        {
            new Tone(440d, 42),
            new Tone(330d, 58)
        },
        0.055d);

    private static readonly byte[] UnlockSound = CreateToneSequence(
        new[]
        {
            new Tone(392d, 42),
            new Tone(523.25d, 64)
        },
        0.06d);

    public static void PlayLock()
    {
        Play(LockSound);
    }

    public static void PlayUnlock()
    {
        Play(UnlockSound);
    }

    private static void Play(byte[] soundBytes)
    {
        _ = Task.Run(() =>
        {
            try
            {
                using MemoryStream stream = new(soundBytes, writable: false);
                using SoundPlayer player = new(stream);
                player.PlaySync();
            }
            catch
            {
            }
        });
    }

    private static byte[] CreateToneSequence(Tone[] tones, double volume)
    {
        const int sampleRate = 44100;
        const short channels = 1;
        const short bitsPerSample = 16;
        const int gapMs = 12;
        const int fadeMs = 5;

        short amplitude = (short)(short.MaxValue * Math.Clamp(volume, 0d, 1d));
        int gapSamples = sampleRate * gapMs / 1000;
        int totalSamples = 0;

        for (int i = 0; i < tones.Length; i++)
        {
            totalSamples += sampleRate * tones[i].DurationMs / 1000;
            if (i < tones.Length - 1)
            {
                totalSamples += gapSamples;
            }
        }

        int dataSize = totalSamples * channels * bitsPerSample / 8;
        using MemoryStream stream = new(44 + dataSize);
        using BinaryWriter writer = new(stream);

        writer.Write(new byte[] { (byte)'R', (byte)'I', (byte)'F', (byte)'F' });
        writer.Write(36 + dataSize);
        writer.Write(new byte[] { (byte)'W', (byte)'A', (byte)'V', (byte)'E' });
        writer.Write(new byte[] { (byte)'f', (byte)'m', (byte)'t', (byte)' ' });
        writer.Write(16);
        writer.Write((short)1);
        writer.Write(channels);
        writer.Write(sampleRate);
        writer.Write(sampleRate * channels * bitsPerSample / 8);
        writer.Write((short)(channels * bitsPerSample / 8));
        writer.Write(bitsPerSample);
        writer.Write(new byte[] { (byte)'d', (byte)'a', (byte)'t', (byte)'a' });
        writer.Write(dataSize);

        int fadeSamples = sampleRate * fadeMs / 1000;
        for (int toneIndex = 0; toneIndex < tones.Length; toneIndex++)
        {
            Tone tone = tones[toneIndex];
            int toneSamples = sampleRate * tone.DurationMs / 1000;
            double angularFrequency = 2d * Math.PI * tone.Frequency / sampleRate;

            for (int sampleIndex = 0; sampleIndex < toneSamples; sampleIndex++)
            {
                double fadeIn = fadeSamples == 0 ? 1d : Math.Min(1d, sampleIndex / (double)fadeSamples);
                double fadeOut = fadeSamples == 0 ? 1d : Math.Min(1d, (toneSamples - sampleIndex) / (double)fadeSamples);
                double envelope = Math.Min(fadeIn, fadeOut);
                short sample = (short)(Math.Sin(sampleIndex * angularFrequency) * amplitude * envelope);
                writer.Write(sample);
            }

            if (toneIndex < tones.Length - 1)
            {
                for (int sampleIndex = 0; sampleIndex < gapSamples; sampleIndex++)
                {
                    writer.Write((short)0);
                }
            }
        }

        return stream.ToArray();
    }

    private readonly record struct Tone(double Frequency, int DurationMs);
}
