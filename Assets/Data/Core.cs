using System;
using System.Text;
using System.Runtime.InteropServices;

class AllodsException : SystemException
{
    internal AllodsException(string text) : base(text) { /* stub */ }
}

class Core
{
    public static void Abort(string format, params object[] args)
    {
        throw new AllodsException(String.Format(format, args));
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
}
