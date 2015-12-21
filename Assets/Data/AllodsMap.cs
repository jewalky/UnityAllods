using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;

public class TerrainLighting
{
    public readonly int Width;
    public readonly int Height;
    public readonly byte[] Result = null;

    public TerrainLighting(int w, int h)
    {
        Width = w;
        Height = h;
        Result = new byte[w * h];
    }

    public void Calculate(sbyte[] heights, double SolarAngle)
    {
        double sunang = SolarAngle * (Math.PI / 180.0);

        double sunx = Math.Cos(sunang) * 1.0;
        double suny = Math.Sin(sunang) * 1.0;
        double sunz = -0.75;

        //
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if ((x <= 1) || (y <= 1) ||
                    (x >= Width - 2) || (y >= Height - 2))
                {
                    Result[y * Width + x] = 0;
                    continue;
                }

                // 
                double p1x = (double)x * 32.0;
                double p1y = (double)y * 32.0;
                double p1z = (double)heights[y * Width + x];

                double p2x = (double)x * 32.0 + 32.0;
                double p2y = (double)y * 32.0;
                double p2z = (double)heights[y * Width + (x + 1)];

                double p3x = (double)x * 32.0;
                double p3y = (double)y * 32.0 + 32.0;
                double p3z = (double)heights[(y + 1) * Width + x];

                //
                double ux = p2x - p1x;
                double uy = p2y - p1y;
                double uz = p2z - p1z;

                double vx = p3x - p1x;
                double vy = p3y - p1y;
                double vz = p3z - p1z;

                //
                double nx = (uy * vz) - (uz * vy);
                double ny = (uz * vx) - (ux * vz);
                double nz = (ux * vy) - (uy * vx);

                //
                double nl = Math.Sqrt((nx * nx) + (ny * ny) + (nz * nz));
                nx /= nl;
                ny /= nl;
                nz /= nl;

                //
                //double dot = Math.Abs(nx * sunx + ny * suny + nz * sunz) * 64.0 + 128.0;
                double dot = Math.Abs(nx * sunx + ny * suny + nz * sunz) * 64.0 + 96.0;
                /*double dot = Math.Abs(nx * sunx + ny * suny + nz * sunz) * 62.0;
                int dot_i = (int)dot;
                uint dot_ab = (uint)Math.Abs(dot_i-31);
                // pow dot_ab
                dot_ab = (uint)(1 << (int)dot_ab);
                dot_ab = Math.Min(31, dot_ab / 16777216 / 8);
                if (dot_i < 32)
                    dot = 128.0 - ((double)dot_ab * 2);
                else dot = 128.0 + ((double)dot_ab * 2);*/

                //double dot = Math.Abs(nx * sunx + ny * suny + nz * sunz) * 128.0 + 64.0;
                Result[y * Width + x] = (byte)dot;
                //Console.WriteLine("dot = {0}", dot);
            }
        }
    }
}

public class AllodsMap
{
    // ====================================
    //  properties
    // ====================================
    //
    public struct AlmData
    {
        public uint Width;
        public uint Height;
        public float SolarAngle;
        public uint TimeOfDay;
        public uint Darkness;
        public uint Contrast;
        public uint UseTiles;

        public uint CountPlayers;
        public uint CountBuildings;
        public uint CountUnits;
        public uint CountTriggers;
        public uint CountSacks;
        public uint CountGroups;
        public uint CountInns;
        public uint CountShops;
        public uint CountPointers;
        public uint CountMusic;
        public string Name;
        public uint RecPlayers;
        public uint Level;
        public uint Junk1;
        public uint Junk2;
        public string Author;

        public void LoadFromStream(BinaryReader reader)
        {
            Width = reader.ReadUInt32();
            Height = reader.ReadUInt32();
            SolarAngle = reader.ReadSingle();
            TimeOfDay = reader.ReadUInt32();
            Darkness = reader.ReadUInt32();
            Contrast = reader.ReadUInt32();
            UseTiles = reader.ReadUInt32();
            CountPlayers = reader.ReadUInt32();
            CountBuildings = reader.ReadUInt32();
            CountUnits = reader.ReadUInt32();
            CountTriggers = reader.ReadUInt32();
            CountSacks = reader.ReadUInt32();
            CountGroups = reader.ReadUInt32();
            CountInns = reader.ReadUInt32();
            CountShops = reader.ReadUInt32();
            CountPointers = reader.ReadUInt32();
            CountMusic = reader.ReadUInt32();
            Name = Core.UnpackByteString(1251, reader.ReadBytes(0x40));
            RecPlayers = reader.ReadUInt32();
            Level = reader.ReadUInt32();
            Junk1 = reader.ReadUInt32();
            Junk2 = reader.ReadUInt32();
            //Console.WriteLine("data junk1 = {0}, junk2 = {1}, light = {2}", Junk1, Junk2, Darkness);
            Author = Core.UnpackByteString(1251, reader.ReadBytes(0x200));
        }

        public void SaveToStream(BinaryWriter writer)
        {
            writer.Write(Width);
            writer.Write(Height);
            writer.Write(SolarAngle);
            writer.Write(TimeOfDay);
            writer.Write(Darkness);
            writer.Write(Contrast);
            writer.Write(UseTiles);
            writer.Write(CountPlayers);
            writer.Write(CountBuildings);
            writer.Write(CountUnits);
            writer.Write(CountTriggers);
            writer.Write(CountSacks);
            writer.Write(CountGroups);
            writer.Write(CountInns);
            writer.Write(CountShops);
            writer.Write(CountPointers);
            writer.Write(CountMusic);
            writer.Write(Core.PackByteString(1251, Name, 0x40));
            writer.Write(RecPlayers);
            writer.Write(Level);
            writer.Write(Junk1);
            writer.Write(Junk2);
            writer.Write(Core.PackByteString(1251, Author, 0x200));
        }
    }

    public AlmData Data;
    public ushort[] Tiles;
    public sbyte[] Heights;
    public byte[] Objects;

    // ====================================
    //  constructors
    // ====================================
    //
    private AllodsMap()
    {
        /* stub */
    }

    public static AllodsMap New(int width, int height)
    {
        width += 16;
        height += 16;

        AllodsMap alm = new AllodsMap();

        alm.Data.Width = (uint)width;
        alm.Data.Height = (uint)height;
        alm.Data.SolarAngle = -45.0f;
        alm.Data.TimeOfDay = 0;
        alm.Data.Darkness = 21;
        alm.Data.Contrast = 128;
        alm.Data.UseTiles = 0x1FFF;

        alm.Data.CountPlayers = 0;
        alm.Data.CountBuildings = 0;
        alm.Data.CountUnits = 0;
        alm.Data.CountTriggers = 0;
        alm.Data.CountSacks = 0;
        alm.Data.CountGroups = 0;
        alm.Data.CountInns = 0;
        alm.Data.CountShops = 0;
        alm.Data.CountPointers = 0;
        alm.Data.CountMusic = 0;

        alm.Data.Name = "";
        alm.Data.RecPlayers = 16;
        alm.Data.Level = 0;
        alm.Data.Junk1 = 0;
        alm.Data.Junk2 = 0;
        alm.Data.Author = "";

        alm.Tiles = new ushort[width * height];
        alm.Heights = new sbyte[width * height];
        alm.Objects = new byte[width * height];

        for (int i = 0; i < width * height; i++)
        {
            alm.Tiles[i] = 0x011; // grass
            alm.Heights[i] = 0;
            alm.Objects[i] = 0;
        }

        return alm;
    }

    public static AllodsMap LoadFrom(string filename)
    {
        try
        {
            MemoryStream ms = ResourceManager.OpenRead(filename);
            BinaryReader msb = new BinaryReader(ms);

            // first, read in the global header
            uint alm_signature = msb.ReadUInt32();
            uint alm_headersize = msb.ReadUInt32();
            uint alm_offsetplayers = msb.ReadUInt32();
            uint alm_sectioncount = msb.ReadUInt32();
            uint alm_version = msb.ReadUInt32();

            if ((alm_signature != 0x0052374D) ||
                (alm_headersize != 0x14) ||
                (alm_sectioncount < 3))
            {
                ms.Close();
                return null;
            }

            AllodsMap alm = new AllodsMap();

            bool DataLoaded = false;
            bool TilesLoaded = false;
            bool HeightsLoaded = false;
            bool ObjectsLoaded = false;

            for (uint i = 0; i < alm_sectioncount; i++)
            {
                uint sec_junk1 = msb.ReadUInt32();
                uint sec_headersize = msb.ReadUInt32();
                uint sec_size = msb.ReadUInt32();
                uint sec_id = msb.ReadUInt32();
                uint sec_junk2 = msb.ReadUInt32();

                //Console.WriteLine("id = {0}, junk1 = {1}, junk2 = {2}", sec_id, sec_junk1, sec_junk2);

                if (sec_id == 0)
                {
                    alm.Data.LoadFromStream(msb);
                    DataLoaded = true;
                }
                else if (sec_id == 1)
                {
                    if (!DataLoaded)
                    {
                        ms.Close();
                        return null;
                    }

                    alm.Tiles = new ushort[alm.Data.Width * alm.Data.Height];
                    for (uint j = 0; j < alm.Data.Width * alm.Data.Height; j++)
                        alm.Tiles[j] = msb.ReadUInt16();
                    TilesLoaded = true;
                }
                else if (sec_id == 2)
                {
                    if (!TilesLoaded)
                    {
                        ms.Close();
                        return null;
                    }

                    alm.Heights = new sbyte[alm.Data.Width * alm.Data.Height];
                    for (uint j = 0; j < alm.Data.Width * alm.Data.Height; j++)
                        alm.Heights[j] = msb.ReadSByte();
                    HeightsLoaded = true;
                }
                else if (sec_id == 3)
                {
                    if (!HeightsLoaded)
                    {
                        ms.Close();
                        return null;
                    }

                    alm.Objects = new byte[alm.Data.Width * alm.Data.Height];
                    for (uint j = 0; j < alm.Data.Width * alm.Data.Height; j++)
                        alm.Objects[j] = msb.ReadByte();
                    ObjectsLoaded = true;
                }
                else break;
            }

            return alm;
        }
        catch (Exception)
        {
            return null;
        }
    }

    // ====================================
    //  methods
    // ====================================
    //
    private void WriteSection(BinaryWriter fsb, int id, MemoryStream section)
    {
        fsb.Write((uint)7);
        fsb.Write((uint)0x14);
        fsb.Write((uint)section.Length);
        fsb.Write((uint)id);
        fsb.Write((uint)336592946);
        fsb.Write(section.ToArray());
    }

    public bool SaveTo(string filename)
    {
        try
        {
            FileStream fs = File.OpenWrite(filename);
            BinaryWriter fsb = new BinaryWriter(fs);

            // first, write ALM header
            // section count is 4 now (0, 1, 2, 3)
            fsb.Write((uint)0x0052374D);
            fsb.Write((uint)0x14);
            fsb.Write((uint)0);
            fsb.Write((uint)4);
            fsb.Write((uint)0x640);

            MemoryStream ms = new MemoryStream();
            BinaryWriter msb = new BinaryWriter(ms);

            // first, write section 0 (data)
            Data.SaveToStream(msb);
            WriteSection(fsb, 0, ms);

            ms.SetLength(0);
            for (uint i = 0; i < Data.Width * Data.Height; i++)
                msb.Write(Tiles[i]);
            WriteSection(fsb, 1, ms);

            ms.SetLength(0);
            for (uint i = 0; i < Data.Width * Data.Height; i++)
                msb.Write(Heights[i]);
            WriteSection(fsb, 2, ms);

            ms.SetLength(0);
            for (uint i = 0; i < Data.Width * Data.Height; i++)
                msb.Write(Objects[i]);
            WriteSection(fsb, 3, ms);

            fs.Close();
            return true;
        }
        catch (IOException e)
        {
            Console.WriteLine(e.ToString());
            return false;
        }
    }
}
