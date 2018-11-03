using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;

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

        public void LoadFromStream(BinaryReader br)
        {
            Width = br.ReadUInt32();
            Height = br.ReadUInt32();
            SolarAngle = br.ReadSingle();
            TimeOfDay = br.ReadUInt32();
            Darkness = br.ReadUInt32();
            Contrast = br.ReadUInt32();
            UseTiles = br.ReadUInt32();
            CountPlayers = br.ReadUInt32();
            CountStructures = br.ReadUInt32();
            CountUnits = br.ReadUInt32();
            CountTriggers = br.ReadUInt32();
            CountSacks = br.ReadUInt32();
            CountGroups = br.ReadUInt32();
            CountInns = br.ReadUInt32();
            CountShops = br.ReadUInt32();
            CountPointers = br.ReadUInt32();
            CountMusic = br.ReadUInt32();
            Name = Core.UnpackByteString(1251, br.ReadBytes(0x40));
            RecPlayers = br.ReadUInt32();
            Level = br.ReadUInt32();
            Junk1 = br.ReadUInt32();
            Junk2 = br.ReadUInt32();
            //Console.WriteLine("data junk1 = {0}, junk2 = {1}, light = {2}", Junk1, Junk2, Darkness);
            Author = Core.UnpackByteString(1251, br.ReadBytes(0x200));
        }
    }

    public class AlmPlayer
    {
        public int Color;
        public uint Flags;//1=AI,2=quest kill
        public int Money;
        public string Name;
        public ushort[] Diplomacy;

        public void LoadFromStream(BinaryReader br)
        {
            Color = br.ReadInt32();
            Flags = br.ReadUInt32();
            Money = br.ReadInt32();
            Name = Core.UnpackByteString(1251, br.ReadBytes(0x20));
            Diplomacy = new ushort[16];
            for (int i = 0; i < 16; i++)
                Diplomacy[i] = br.ReadUInt16();
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

        public void LoadFromStream(BinaryReader br)
        {
            int xRaw = br.ReadInt32();
            int yRaw = br.ReadInt32();
            X = ((xRaw & 0x0000FF00) >> 8) + (float)(xRaw & 0x00FF) / 256; // 00 10 00 80 = 16.5
            Y = ((yRaw & 0x0000FF00) >> 8) + (float)(yRaw & 0x00FF) / 256;
            TypeID = br.ReadInt32();
            Health = br.ReadInt16();
            Player = br.ReadInt32();
            ID = br.ReadInt16();
            if ((TypeID & 0x01000000) != 0)
            {
                Width = br.ReadInt32();
                Height = br.ReadInt32();
                IsBridge = true;
            }
            TypeID &= 0xFFFF;
        }
    }

    public class AlmGroup
    {
        public enum AlmGroupFlags
        {
            AiInstantRnabled = 1, RandomPositions = 2,
            QuestKill = 4, QuestIntercept = 8 
        }

        public void LoadFromStream(BinaryReader br)
        {
            GroupId = br.ReadUInt32();
            RepopTime = br.ReadUInt32();
            GroupFlag = (AlmGroupFlags)br.ReadUInt32();
            InstanceId = br.ReadUInt32();
        }

        public uint InstanceId { get; set; }
        public AlmGroupFlags GroupFlag { get; set; }
        public uint RepopTime { get; set; }
        public uint GroupId { get; set; }
    }

    public class AlmSack
    {
        public void LoadFromStream(BinaryReader br)
        {
            NumberOfItems = br.ReadUInt32();
            UnitId = br.ReadUInt32();
            X = br.ReadUInt32();
            Y = br.ReadUInt32();
            Gold = br.ReadUInt32();
            Items = new AlmSackItem[NumberOfItems];
            for (int i = 0; i < NumberOfItems; i++)
            {
                Items[i] = new AlmSackItem();
                Items[i].LoadFromStream(br);
            }
        }

        public uint Gold { get; set; }
        public uint Y { get; set; }
        public uint X { get; set; }
        public uint UnitId { get; set; }
        public uint NumberOfItems { get; set; }
        public AlmSackItem[] Items { get; set; }
    }

    public class AlmMagazine
    {
        public enum AlmMagazineItemMaterial
        {
            Iron = 0x00000001,
            Bronze = 0x00000002,
            Steel = 0x00000004,
            Silver = 0x00000008,
            Gold = 0x00000010,
            Mithril = 0x00000020,
            Adamantium = 0x00000040,
            Meteorite = 0x00000080,
            Wood = 0x00000100,
            MagicWood = 0x00000200,
            Leather = 0x00000400,
            BigLeather = 0x00000800,
            DragonLeather = 0x00001000,
            Crystall = 0x00002000,
            None	 = 0x00004000
        }

        public enum AlmMagazineItemType
        {
            Weapon = 0x00400000,
            Shield = 0x00800000,
            Armor = 0x01000000,
            ArmorMage = 0x02000000,
            Other = 0x04000000,
            Wands = 0x08000000,
        }

        public enum AlmMagazineItemExtra
        {
            Common = 0x10000000,
            Magic = 0x20000000
        }

        public enum AlmMagazineItemRareType
        {
            Common = 0x00008000,
            Uncommon = 0x00010000,
            Rare = 0x00020000,
            VeryRare = 0x00040000,
            Elven = 0x00080000,
            Bad = 0x00100000,
            Good = 0x00200000,
        }

        public void LoadFromStream(BinaryReader br)
        {
            Id = br.ReadUInt32();
            ShelfFlags1 = br.ReadUInt32();
            ShelfFlags2 = br.ReadUInt32();
            ShelfFlags3 = br.ReadUInt32();
            ShelfFlags4 = br.ReadUInt32();
            MinPrice1 = br.ReadUInt32();
            MinPrice2 = br.ReadUInt32();
            MinPrice3 = br.ReadUInt32();
            MinPrice4 = br.ReadUInt32();
            MaxPrice1 = br.ReadUInt32();
            MaxPrice2 = br.ReadUInt32();
            MaxPrice3 = br.ReadUInt32();
            MaxPrice4 = br.ReadUInt32();
            NumberOfItems1 = br.ReadUInt32();
            NumberOfItems2 = br.ReadUInt32();
            NumberOfItems3 = br.ReadUInt32();
            NumberOfItems4 = br.ReadUInt32();
            MaxNumberOneType1 = br.ReadUInt32();
            MaxNumberOneType2 = br.ReadUInt32();
            MaxNumberOneType3 = br.ReadUInt32();
            MaxNumberOneType4 = br.ReadUInt32();
        }

        public uint MaxNumberOneType4 { get; set; }
        public uint MaxNumberOneType3 { get; set; }
        public uint MaxNumberOneType2 { get; set; }
        public uint MaxNumberOneType1 { get; set; }
        public uint NumberOfItems4 { get; set; }
        public uint NumberOfItems3 { get; set; }
        public uint NumberOfItems2 { get; set; }
        public uint NumberOfItems1 { get; set; }
        public uint MaxPrice4 { get; set; }
        public uint MaxPrice3 { get; set; }
        public uint MaxPrice2 { get; set; }
        public uint MaxPrice1 { get; set; }
        public uint MinPrice4 { get; set; }
        public uint MinPrice3 { get; set; }
        public uint MinPrice2 { get; set; }
        public uint MinPrice1 { get; set; }
        public uint ShelfFlags4 { get; set; }
        public uint ShelfFlags3 { get; set; }
        public uint ShelfFlags2 { get; set; }
        public uint ShelfFlags1 { get; set; }
        public uint Id { get; set; }
    }

    public class AlmOptionPointer
    {
        public void LoadFromStream(BinaryReader br)
        {
            Id = br.ReadUInt32();
            InstanceOn = br.ReadUInt32();
            InstanceId = br.ReadUInt32();
        }

        public uint InstanceId { get; set; }
        public uint InstanceOn { get; set; }
        public uint Id { get; set; }
    }

    public class AlmTavernInfo
    {
        public enum QuestType
        {
            Delivery = 0x02,
            RaiseDead = 0x04,
            KillAllHumans = 0x10,
            KillAllMonsters = 0x20,
            KillAllUndeadNecro = 0x40
        }

        public void LoadFromStream(BinaryReader br)
        {
            Id = br.ReadUInt32();
            Flag = (QuestType)br.ReadUInt32();
            DeliveryId = br.ReadUInt32();
        }

        public uint DeliveryId { get; set; }
        public QuestType Flag { get; set; }
        public uint Id { get; set; }
    }

    public class AlmSackItem
    {
        public void LoadFromStream(BinaryReader br)
        {
            ItemId = br.ReadUInt32();
            Wielded = br.ReadUInt16();
            EffectNumber = br.ReadUInt32();
        }

        public uint EffectNumber { get; set; }
        public ushort Wielded { get; set; }
        public uint ItemId { get; set; }
    }

    public class AlmMusic
    {
        public void LoadFromStream(BinaryReader br)
        {
            X = br.ReadUInt32();
            Y = br.ReadUInt32();
            Radius = br.ReadUInt32();
            TypeId1 = br.ReadUInt32();
            TypeId2 = br.ReadUInt32();
            TypeId3 = br.ReadUInt32();
            TypeId4 = br.ReadUInt32();
        }

        public uint TypeId4 { get; set; }
        public uint TypeId3 { get; set; }
        public uint TypeId2 { get; set; }
        public uint TypeId1 { get; set; }
        public uint Radius { get; set; }
        public uint Y { get; set; }
        public uint X { get; set; }
    }

    public class AlmEffectModifier
    {
        public void LoadFromStream(BinaryReader br)
        {
            TypeOfMod = br.ReadUInt16();
            Value = br.ReadUInt32();
        }

        public uint Value { get; set; }
        public ushort TypeOfMod { get; set; }
    }

    public class AlmEffect
    {
        public bool IsItem { get; set; }

        public void LoadFromStream(BinaryReader br)
        {
            Id = br.ReadUInt32();
            X = br.ReadUInt32();
            Y = br.ReadUInt32();
            IsItem = X == 0 && Y == 0;
            MagicOrFlag = br.ReadUInt16();
            if (!IsItem)
            {
                StructureId = br.ReadUInt32();
            }
            else
            {
                MagicMinDmg = br.ReadUInt16();
                AvgDmg = br.ReadUInt16();
            }
            TypeId = br.ReadUInt16();
            MagicPower = br.ReadUInt16();
            CountOfModifier = br.ReadUInt32();
            EffectModifiers = new AlmEffectModifier[CountOfModifier];
            for (int i = 0; i < CountOfModifier; i++)
            {
                EffectModifiers[i] = new AlmEffectModifier();
                EffectModifiers[i].LoadFromStream(br);
            }
        }

        public uint CountOfModifier { get; set; }
        public ushort MagicPower { get; set; }
        public ushort TypeId { get; set; }
        public ushort AvgDmg { get; set; }
        public ushort MagicMinDmg { get; set; }
        public uint StructureId { get; set; }
        public uint MagicOrFlag { get; set; }
        public uint Y { get; set; }
        public uint X { get; set; }
        public uint Id { get; set; }
        public AlmEffectModifier[] EffectModifiers { get; set; }
    }

    public class AlmLogic
    {
        public AlmLogicItem[] LogicItems { get; set; }
        public AlmCheckItem[] CheckItems { get; set; }
        public AlmTrigger[] TriggerItems { get; set; }
        public enum InctanceType
        {
            Num = 1, Group = 2,
            Player = 3, Unit = 4,
            X = 5, Y = 6,
            Item = 8, Building = 9
        }

        public void LoadFromStream(BinaryReader br)
        {
            NumberOfItems = br.ReadUInt32();
            LogicItems = new AlmLogicItem[NumberOfItems];
            for (int i = 0; i < NumberOfItems; i++)
            {
                LogicItems[i] = new AlmLogicItem();
                LogicItems[i].LoadFromStream(br);
            }
            NumberOfChecks = br.ReadUInt32();
            CheckItems = new AlmCheckItem[NumberOfChecks];
            for (int i = 0; i < NumberOfChecks; i++)
            {
                CheckItems[i] = new AlmCheckItem();
                CheckItems[i].LoadFromStream(br);
            }
            NumberOfTriggers = br.ReadUInt32();
            TriggerItems = new AlmTrigger[NumberOfTriggers];
            for (int i = 0; i < NumberOfTriggers; i++)
            {
                TriggerItems[i] = new AlmTrigger();
                TriggerItems[i].LoadFromStream(br);
            }
        }

        public class AlmLogicItem
        {
            public uint ExecOnceFlag { get; set; }
            public uint TypeId { get; set; }
            public uint TypeIndex { get; set; }
            public string Name { get; set; }
            public LogicVal[] Values { get; set; }
            public struct LogicVal
            {
                public string Name { get; set; }
                public uint Value { get; set; }
                public InctanceType InstType { get; set; }
            }

            public void LoadFromStream(BinaryReader br)
            {
                Name = Core.UnpackByteString(1251, br.ReadBytes(0x40));
                TypeId = br.ReadUInt32();
                TypeIndex = br.ReadUInt32();
                ExecOnceFlag = br.ReadUInt32();

                Values = new LogicVal[10];
                for (int i = 0; i < 10; i++)
                {
                    Values[i].Value = br.ReadUInt32();
                }
                for (int i = 0; i < 10; i++)
                {
                    Values[i].InstType = (InctanceType)br.ReadUInt32();
                }
                for (int i = 0; i < 10; i++)
                {
                    Values[i].Name = Core.UnpackByteString(1251, br.ReadBytes(0x40));
                }
            }
        }

        public class AlmCheckItem : AlmLogicItem{}

        public class AlmTrigger
        {
            public enum TriggerOperator
            {
                Equal = 0, NotEqual = 1,
                MoreThan = 2, LowerThan = 3,
                MoreOrEq = 4, LowerOrEq = 5
            }

            public void LoadFromStream(BinaryReader br)
            {
                Name = Core.UnpackByteString(1251, br.ReadBytes(0x80));

                Check1 = br.ReadUInt32();
                Check2 = br.ReadUInt32();
                Check3 = br.ReadUInt32();
                Check4 = br.ReadUInt32();
                Check5 = br.ReadUInt32();
                Check6 = br.ReadUInt32();

                Instance1 = br.ReadUInt32();
                Instance2 = br.ReadUInt32();
                Instance3 = br.ReadUInt32();
                Instance4 = br.ReadUInt32();

                Operator12 = (TriggerOperator)br.ReadUInt32();
                Operator34 = (TriggerOperator)br.ReadUInt32();
                Operator56 = (TriggerOperator)br.ReadUInt32();

                RunOnce = br.ReadUInt32();
            }

            public string Name { get; set; }

            public uint RunOnce { get; set; }
            public TriggerOperator Operator56 { get; set; }
            public TriggerOperator Operator34 { get; set; }
            public TriggerOperator Operator12 { get; set; }
            public uint Instance4 { get; set; }
            public uint Instance3 { get; set; }
            public uint Instance2 { get; set; }
            public uint Instance1 { get; set; }
            public uint Check6 { get; set; }
            public uint Check5 { get; set; }
            public uint Check4 { get; set; }
            public uint Check3 { get; set; }
            public uint Check2 { get; set; }
            public uint Check1 { get; set; }
        }

        public uint NumberOfTriggers { get; set; }
        public uint NumberOfChecks { get; set; }
        public uint NumberOfItems { get; set; }
        

        public Vector2 GetDropLocation()
        {
            if (LogicItems != null && LogicItems.Length > 0)
            {
                var dropLoc = LogicItems.Where(x => x.Name.ToLower() == "drop location")
                                        .OrderBy(x => Guid.NewGuid())
                                        .FirstOrDefault();
                if (dropLoc != null)
                {
                    return new Vector2(dropLoc.Values.Where(x => x.InstType == InctanceType.X).Select(x => x.Value).FirstOrDefault(),
                                       dropLoc.Values.Where(x => x.InstType == InctanceType.Y).Select(x => x.Value).FirstOrDefault());
                }
            }
            return new Vector2(6,6);
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

        public void LoadFromStream(BinaryReader br)
        {
            int xRaw = br.ReadInt32();
            int yRaw = br.ReadInt32();
            X = ((xRaw & 0x0000FF00) >> 8) + (float)(xRaw & 0x00FF) / 256; // 00 10 00 80 = 16.5
            Y = ((yRaw & 0x0000FF00) >> 8) + (float)(yRaw & 0x00FF) / 256;
            TypeID = br.ReadUInt16();
            Face = br.ReadUInt16();
            Flags = br.ReadUInt32();
            Flags2 = br.ReadUInt32();
            ServerID = br.ReadInt32();
            Player = br.ReadInt32();
            Sack = br.ReadInt32();
            Angle = br.ReadInt32();
            Health = br.ReadInt16();
            HealthMax = br.ReadInt16();
            ID = br.ReadInt32() & 0xFFFF;
            Group = br.ReadInt32();
        }
    }

    public AlmData Data = new AlmData();
    public ushort[] Tiles;
    public sbyte[] Heights;
    public byte[] Objects;
    public AlmPlayer[] Players;
    public AlmStructure[] Structures;
    public AlmUnit[] Units;
    public AlmLogic Logic;
    public AlmMusic[] Music;
    public AlmGroup[] Groups;
    public AlmSack[] Sacks;
    public AlmEffect[] Effects;
    public AlmMagazine[] Magazines;
    public AlmOptionPointer[] OptionPointers;
    public AlmTavernInfo[] AlmTavernInfos;

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
            BinaryReader br = new BinaryReader(ms);

            // first, read in the global header
            uint alm_signature = br.ReadUInt32();
            uint alm_headersize = br.ReadUInt32();
            br.BaseStream.Position += 4;  // uint alm_offsetplayers = msb.ReadUInt32();
            uint alm_sectioncount = br.ReadUInt32();
            br.BaseStream.Position += 4; // uint alm_version = msb.ReadUInt32();

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

            for (uint i = 0; i < alm_sectioncount; i++)
            {
                br.BaseStream.Position += 4; // uint sec_junk1 = msb.ReadUInt32();
                br.BaseStream.Position += 4; // uint sec_headersize = msb.ReadUInt32();
                uint sec_size = br.ReadUInt32();
                uint sec_id = br.ReadUInt32();
                br.BaseStream.Position += 4; // uint sec_junk2 = msb.ReadUInt32();

                //Debug.Log(string.Format("id = {0}, junk1 = {1}, junk2 = {2}", sec_id, sec_junk1, sec_junk2));

                switch (sec_id)
                {
                    case 0: // data
                        alm.Data.LoadFromStream(br);
                        DataLoaded = true;
                        break;
                    case 1: // tiles
                        if (!DataLoaded)
                        {
                            ms.Close();
                            return null;
                        }

                        alm.Tiles = new ushort[alm.Data.Width * alm.Data.Height];
                        for (uint j = 0; j < alm.Data.Width * alm.Data.Height; j++)
                            alm.Tiles[j] = br.ReadUInt16();
                        TilesLoaded = true;
                        break;
                    case 2: // heights
                        if (!TilesLoaded)
                        {
                            ms.Close();
                            return null;
                        }

                        alm.Heights = new sbyte[alm.Data.Width * alm.Data.Height];
                        for (uint j = 0; j < alm.Data.Width * alm.Data.Height; j++)
                            alm.Heights[j] = br.ReadSByte();
                        HeightsLoaded = true;
                        break;
                    case 3: // objects (obstacles)
                        if (!HeightsLoaded)
                        {
                            ms.Close();
                            return null;
                        }

                        alm.Objects = new byte[alm.Data.Width * alm.Data.Height];
                        for (uint j = 0; j < alm.Data.Width * alm.Data.Height; j++)
                            alm.Objects[j] = br.ReadByte();
                        break;
                    case 4: // structures
                        if (!DataLoaded)
                        {
                            ms.Close();
                            return null;
                        }

                        alm.Structures = new AlmStructure[alm.Data.CountStructures];
                        for (uint j = 0; j < alm.Data.CountStructures; j++)
                        {
                            alm.Structures[j] = new AlmStructure();
                            alm.Structures[j].LoadFromStream(br);
                        }
                        break;
                    case 5: // players
                        if (!DataLoaded)
                        {
                            ms.Close();
                            return null;
                        }

                        alm.Players = new AlmPlayer[alm.Data.CountPlayers];
                        for (uint j = 0; j < alm.Data.CountPlayers; j++)
                        {
                            alm.Players[j] = new AlmPlayer();
                            alm.Players[j].LoadFromStream(br);
                        }
                        break;
                    case 6: // units
                        if (!DataLoaded)
                        {
                            ms.Close();
                            return null;
                        }

                        alm.Units = new AlmUnit[alm.Data.CountUnits];
                        for (uint j = 0; j < alm.Data.CountUnits; j++)
                        {
                            alm.Units[j] = new AlmUnit();
                            alm.Units[j].LoadFromStream(br);
                        }
                        break;
                    case 7: // Logic
                        if (!DataLoaded)
                        {
                            ms.Close();
                            return null;
                        }
                        alm.Logic = new AlmLogic();
                        alm.Logic.LoadFromStream(br);
                        break;
                    case 8: // Sack
                        if (!DataLoaded)
                        {
                            ms.Close();
                            return null;
                        }
                        alm.Sacks = new AlmSack[alm.Data.CountSacks];
                        for (int j = 0; j < alm.Data.CountSacks; j++)
                        {
                            alm.Sacks[j] = new AlmSack();
                            alm.Sacks[j].LoadFromStream(br);
                        }
                        break;
                    case 9: // Effects
                        if (!DataLoaded)
                        {
                            ms.Close();
                            return null;
                        }
                        var numberOfEffects = br.ReadUInt32();
                        alm.Effects = new AlmEffect[numberOfEffects];
                        for (int j = 0; j < numberOfEffects; j++)
                        {
                            alm.Effects[j] = new AlmEffect();
                            alm.Effects[j].LoadFromStream(br);
                        }
                        break;
                    case 10: // Groups 
                        if (!DataLoaded)
                        {
                            ms.Close();
                            return null;
                        }
                        alm.Groups = new AlmGroup[alm.Data.CountGroups];
                        for (int j = 0; j < alm.Data.CountGroups; j++)
                        {
                            alm.Groups[j] = new AlmGroup();
                            alm.Groups[j].LoadFromStream(br);
                        }
                        break;
                    case 11: // Options
                        if (!DataLoaded)
                        {
                            ms.Close();
                            return null;
                        }
                        alm.AlmTavernInfos = new AlmTavernInfo[alm.Data.CountInns];
                        for (int j = 0; j < alm.Data.CountInns; j++)
                        {
                            alm.AlmTavernInfos[j] = new AlmTavernInfo();
                            alm.AlmTavernInfos[j].LoadFromStream(br);
                        }
                        alm.Magazines = new AlmMagazine[alm.Data.CountShops];
                        for (int j = 0; j < alm.Data.CountShops; j++)
                        {
                            alm.Magazines[j] = new AlmMagazine();
                            alm.Magazines[j].LoadFromStream(br);
                        }
                        alm.OptionPointers = new AlmOptionPointer[alm.Data.CountPointers];
                        for (int j = 0; j < alm.Data.CountPointers; j++)
                        {
                            alm.OptionPointers[j] = new AlmOptionPointer();
                            alm.OptionPointers[j].LoadFromStream(br);
                        }
                        break;
                    case 12: // Music
                        alm.Music = new AlmMusic[alm.Data.CountMusic+1];
                        alm.Music[0] = new AlmMusic();
                        alm.Music[0].LoadFromStream(br);
                        for (int j = 0; j < alm.Data.CountMusic; j++)
                        {
                            alm.Music[j+1] = new AlmMusic();
                            alm.Music[j + 1].LoadFromStream(br);
                        }
                        break;
                    default:
                        ms.Position += sec_size;
                        break;
                }
            }

            return alm;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
