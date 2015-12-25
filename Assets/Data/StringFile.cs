using System;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using System.Collections.Generic;

public class StringFile
{
    public List<string> Strings { get; private set; }

    public StringFile(string filename)
    {
        Strings = new List<string>();
        using (MemoryStream ms = ResourceManager.OpenRead(filename))
        {
            StreamReader tr = new StreamReader(ms, Encoding.GetEncoding(1251));
            while (!tr.EndOfStream)
            {
                string line = tr.ReadLine().Trim();
                Strings.Add(line);
            }
        }
    }
}
