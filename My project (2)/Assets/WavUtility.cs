using System.IO;
using UnityEngine;

public static class WavUtility
{
    public static byte[] AudioClipToWavBytes(AudioClip clip)
    {
        MemoryStream stream = new MemoryStream();
        int sampleCount = clip.samples * clip.channels;
        float[] samples = new float[sampleCount];
        clip.GetData(samples, 0);

        // Converter float [-1,1] em PCM16
        short[] intData = new short[sampleCount];
        byte[] bytesData = new byte[sampleCount * 2];
        int rescaleFactor = 32767;
        for (int i = 0; i < samples.Length; i++)
        {
            intData[i] = (short)(samples[i] * rescaleFactor);
            byte[] byteArr = System.BitConverter.GetBytes(intData[i]);
            byteArr.CopyTo(bytesData, i * 2);
        }

        // Cabeçalho WAV
        MemoryStream wav = new MemoryStream();
        BinaryWriter writer = new BinaryWriter(wav);

        writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + bytesData.Length);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)clip.channels);
        writer.Write(clip.frequency);
        writer.Write(clip.frequency * clip.channels * 2);
        writer.Write((short)(clip.channels * 2));
        writer.Write((short)16);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        writer.Write(bytesData.Length);
        writer.Write(bytesData);

        return wav.ToArray();
    }
}
