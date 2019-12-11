using System;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using UnityEngine;

public class Core
{
    public static void Abort(string format, params object[] args)
    {
        if (!Application.isPlaying)
            return;
        //throw new AllodsException(String.Format(format, args));
        string error = string.Format(format, args);
        // now since we can't abort with exception, call quit manually (+ add stack trace)
        string stack = new Exception().ToString();
        Debug.LogErrorFormat("Abort: {0}\nStack trace:{1}\n\n", error, stack);
        Application.Quit();
    }

    public static string UnpackByteString(int encoding, byte[] bytes)
    {
        string str_in = Encoding.GetEncoding(encoding).GetString(bytes);
        string str_out = str_in;
        int i = str_out.IndexOf('\0');
        if (i >= 0) str_out = str_out.Substring(0, i);
        return str_out;
    }

    public static byte[] PackByteString(int encoding, string str, uint length)
    {
        byte[] b_out = new byte[length];
        byte[] b_bb = Encoding.GetEncoding(encoding).GetBytes(str);
        for (int i = 0; i < length; i++)
        {
            if (i < b_bb.Length)
                b_out[i] = b_bb[i];
            else b_out[i] = 0;
        }
        return b_out;
    }

    public static string ReadSmallString(BinaryReader br)
    {
        byte size = br.ReadByte();
        return UnpackByteString(866, br.ReadBytes(size));
    }

    public static string ReadMediumString(BinaryReader br)
    {
        ushort size = br.ReadUInt16();
        return UnpackByteString(866, br.ReadBytes(size));
    }

    public static string ReadBigString(BinaryReader br)
    {
        uint size = br.ReadUInt32();
        return UnpackByteString(866, br.ReadBytes((int)size));
    }

    public static void WriteSmallString(BinaryWriter bw, string str)
    {
        bw.Write((byte)str.Length);
        bw.Write(PackByteString(866, str, (byte)str.Length));
    }

    public static void WriteMediumString(BinaryWriter bw, string str)
    {
        bw.Write((ushort)str.Length);
        bw.Write(PackByteString(866, str, (ushort)str.Length));
    }

    public static void WriteBigString(BinaryWriter bw, string str)
    {
        bw.Write((uint)str.Length);
        bw.Write(PackByteString(866, str, (uint)str.Length));
    }
}
