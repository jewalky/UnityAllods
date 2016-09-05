using UnityEngine;
using System;
using System.IO;

public class Audio
{
    private static float MakeFloatFromShort(byte b1, byte b2)
    {
        // convert two bytes to one short (little endian)
        short s = (short)(((int)b2 << 8) | b1);
        // convert to range from -1 to (just below) 1
        return s / 32768f;
    }

    public static AudioClip LoadWAV(string filename)
    {
        // uses code from http://stackoverflow.com/questions/8754111/how-to-read-the-data-in-a-wav-file-to-an-array
        MemoryStream ms = ResourceManager.OpenRead(filename);
        if (ms == null)
        {
            Core.Abort("Couldn't load \"{0}\"", filename);
            return null;
        }

        byte[] wav = new byte[ms.Length];
        ms.Read(wav, 0, (int)ms.Length);
        ms.Close();

        int channels = wav[22];
        int pos = 12;

        try
        {
            while (!(wav[pos] == 100 && wav[pos + 1] == 97 && wav[pos + 2] == 116 && wav[pos + 3] == 97))
            {
                pos += 4;
                int chunkSize = wav[pos] + wav[pos + 1] * 256 + wav[pos + 2] * 65536 + wav[pos + 3] * 16777216;
                pos += 4 + chunkSize;
            }
        }
        catch (IndexOutOfRangeException)
        {
            Core.Abort("Couldn't load \"{0}\" (invalid WAV)", filename);
            return null;
        }

        pos += 8;

        int samples = (wav.Length - pos) / 2;     // 2 bytes per sample (16 bit sound mono)
        if (channels == 2) samples /= 2;

        float[] fsamples = new float[samples*channels];

        int i = 0;
        while (pos < wav.Length)
        {
            fsamples[i] = MakeFloatFromShort(wav[pos], wav[pos + 1]);
            pos += 2;
            i++;
            if (channels == 2)
            {
                fsamples[i] = MakeFloatFromShort(wav[pos], wav[pos + 1]);
                pos += 2;
                i++;
            }
        }

        // allods wav files are 22k
        AudioClip clip = AudioClip.Create(filename, samples, channels, 22050, false);
        clip.SetData(fsamples, 0);
        return clip;
    }
}