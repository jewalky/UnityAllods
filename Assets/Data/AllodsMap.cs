using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

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

    public void Calculate(sbyte[] heights, double solarAngle)
    {
        double sunang = solarAngle * (Math.PI / 180.0);

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
                double p1x = x * 32.0;
                double p1y = y * 32.0;
                double p1z = heights[y * Width + x];

                double p2x = x * 32.0 + 32.0;
                double p2y = y * 32.0;
                double p2z = heights[y * Width + (x + 1)];

                double p3x = x * 32.0;
                double p3y = y * 32.0 + 32.0;
                double p3z = heights[(y + 1) * Width + x];

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
    public class AlmData
    {
        public uint Width;
        public uint Height;
        public float SolarAngle;
        public uint TimeOfDay;
        public uint Darkness;
        public uint Contrast;
        public uint UseTiles;

        public uint CountPlayers;
        public uint CountStructures;
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
            CountStructures = reader.ReadUInt32();
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
    }

    public class AlmPlayer
    {
        public int Color;
        public uint Flags;//1=AI,2=quest kill
        public int Money;
        public string Name;
        public ushort[] Diplomacy;

        public void LoadFromStream(BinaryReader reader)
        {
            Color = reader.ReadInt32();
            Flags = reader.ReadUInt32();
            Money = reader.ReadInt32();
            Name = Core.UnpackByteString(1251, reader.ReadBytes(0x20));
            Diplomacy = new ushort[16];
            for (int i = 0; i < 16; i++)
                Diplomacy[i] = reader.ReadUInt16();
        }
    }

    public class AlmStructure
    {
        public float X;
        public float Y;
        public int TypeID;
        public short Health;
        public int Player;
        public int ID;
        public bool IsBridge;
        public int Width;
        public int Height;

        public void LoadFromStream(BinaryReader reader)
        {
            int xRaw = reader.ReadInt32();
            int yRaw = reader.ReadInt32();
            X = ((xRaw & 0x0000FF00) >> 8) + (float)(xRaw & 0x00FF) / 256; // 00 10 00 80 = 16.5
            Y = ((yRaw & 0x0000FF00) >> 8) + (float)(yRaw & 0x00FF) / 256;
            TypeID = reader.ReadInt32();
            Health = reader.ReadInt16();
            Player = reader.ReadInt32();
            ID = reader.ReadInt16();
            if ((TypeID & 0x01000000) != 0)
            {
                Width = reader.ReadInt32();
                Height = reader.ReadInt32();
                IsBridge = true;
            }
            TypeID &= 0xFFFF;
        }
    }

    public class AlmUnit
    {
        public float X;
        public float Y;
        public ushort TypeID;
        public ushort Face;
        public uint Flags;
        public uint Flags2;
        public int ServerID;
        public int Player;
        public int Sack;
        public int Angle;
        public short Health;
        public short HealthMax;
        public int ID;
        public int Group;

        public void LoadFromStream(BinaryReader reader)
        {
            int xRaw = reader.ReadInt32();
            int yRaw = reader.ReadInt32();
            X = ((xRaw & 0x0000FF00) >> 8) + (float)(xRaw & 0x00FF) / 256; // 00 10 00 80 = 16.5
            Y = ((yRaw & 0x0000FF00) >> 8) + (float)(yRaw & 0x00FF) / 256;
            TypeID = reader.ReadUInt16();
            Face = reader.ReadUInt16();
            Flags = reader.ReadUInt32();
            Flags2 = reader.ReadUInt32();
            ServerID = reader.ReadInt32();
            Player = reader.ReadInt32();
            Sack = reader.ReadInt32();
            Angle = reader.ReadInt32();
            Health = reader.ReadInt16();
            HealthMax = reader.ReadInt16();
            ID = reader.ReadInt32() & 0xFFFF;
            Group = reader.ReadInt32();
        }
    }

    public AlmData Data = new AlmData();
    public ushort[] Tiles;
    public sbyte[] Heights;
    public byte[] Objects;
    public AlmPlayer[] Players;
    public AlmStructure[] Structures;
    public AlmUnit[] Units;

    // ====================================
    //  constructors
    // ====================================
    //
    private AllodsMap()
    {
        /* stub */
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

                //Debug.Log(string.Format("id = {0}, junk1 = {1}, junk2 = {2}", sec_id, sec_junk1, sec_junk2));

                if (sec_id == 0) // data
                {
                    alm.Data.LoadFromStream(msb);
                    DataLoaded = true;
                }
                else if (sec_id == 1) // tiles
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
                else if (sec_id == 2) // heights
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
                else if (sec_id == 3) // objects (obstacles)
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
                else if (sec_id == 5) // players
                {
                    if (!DataLoaded)
                    {
                        ms.Close();
                        return null;
                    }

                    alm.Players = new AlmPlayer[alm.Data.CountPlayers];
                    for (uint j = 0; j < alm.Data.CountPlayers; j++)
                    {
                        alm.Players[j] = new AlmPlayer();
                        alm.Players[j].LoadFromStream(msb);
                    }
                }
                else if (sec_id == 4) // structures
                {
                    if (!DataLoaded)
                    {
                        ms.Close();
                        return null;
                    }

                    alm.Structures = new AlmStructure[alm.Data.CountStructures];
                    for (uint j = 0; j < alm.Data.CountStructures; j++)
                    {
                        alm.Structures[j] = new AlmStructure();
                        alm.Structures[j].LoadFromStream(msb);
                    }
                }
                else if (sec_id == 6) // units
                {
                    if (!DataLoaded)
                    {
                        ms.Close();
                        return null;
                    }

                    alm.Units = new AlmUnit[alm.Data.CountUnits];
                    for (uint j = 0; j < alm.Data.CountUnits; j++)
                    {
                        alm.Units[j] = new AlmUnit();
                        alm.Units[j].LoadFromStream(msb);
                    }
                }
                else
                {
                    ms.Position += sec_size;
                }
            }

            return alm;
        }
        catch (Exception e)
        {
            return null;
        }
    }
}
