using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using UnityEngine;
using System.Collections;

public class Templates
{
    public class TplHeader
    {
        public List<string> Headers = new List<string>();

        public void LoadFromStream(BinaryReader br)
        {
            ushort countHeaders = br.ReadUInt16();
            for (ushort i = 0; i < countHeaders; i++)
                Headers.Add(Core.ReadSmallString(br));
        }
    }

    public class TplSimpleReader
    {
        internal class FieldCountController : Attribute
        {
            public long JunkSize { get; private set; }

            public FieldCountController(long junk_size)
            {
                JunkSize = junk_size;
            }
        }

        internal class FieldNullController : Attribute
        {
            public FieldNullController() { }
        }

        public static bool Logging = false;

        internal static object LoadObjectFromStream(Type t, BinaryReader br)
        {
            if (t.IsAssignableFrom(typeof(string)))
                return Core.ReadSmallString(br);
            if (t.IsAssignableFrom(typeof(int)))
                return br.ReadInt32();
            if (t.IsAssignableFrom(typeof(uint)))
                return br.ReadUInt32();
            if (t.IsAssignableFrom(typeof(short)))
                return br.ReadInt16();
            if (t.IsAssignableFrom(typeof(ushort)))
                return br.ReadUInt16();
            if (t.IsAssignableFrom(typeof(sbyte)))
                return br.ReadSByte();
            if (t.IsAssignableFrom(typeof(byte)))
                return br.ReadByte();
            if (t == typeof(float)) // I'm not sure, but float is assignable from double right? and we need to differ.
                return (float)br.ReadDouble();
            if (t == typeof(double))
                return br.ReadDouble();
            Core.Abort("Unknown type {0} in data.bin serialization!", t.Name);
            return null;
        }

        public virtual void LoadFromStream(BinaryReader br)
        {
            FieldInfo[] fields = GetType().GetFields().OrderBy(f => f.MetadataToken).ToArray();
            foreach (FieldInfo field in fields)
            {
                //Debug.Log(string.Format("field = {0}", field.Name));
                FieldNullController[] nullController = (FieldNullController[])field.GetCustomAttributes(typeof(FieldNullController), false);
                if (nullController.Length > 0)
                {
                    byte isNotNull = br.ReadByte();
                    if (isNotNull == 0)
                    {
                        if (Logging)
                            Debug.LogFormat("{0} is skipped (null byte set)", field.Name);
                        continue;
                    }
                }

                if (field.FieldType.IsArray)
                {
                    IList arr = (IList)field.GetValue(this);
                    if (Logging)
                        Debug.LogFormat("{0} is {1} len {2}", field.Name, field.FieldType.Name, arr.Count);
                    for (int i = 0; i < arr.Count; i++)
                        arr[i] = LoadObjectFromStream(field.FieldType.GetElementType(), br);
                }
                else
                {
                    field.SetValue(this, LoadObjectFromStream(field.FieldType, br));
                    if (Logging)
                        Debug.LogFormat("{0} is {1}, value = {2}", field.Name, field.FieldType.Name, field.GetValue(this));
                    
                    // FieldCountController-marked fields control the "nullness" of this object.
                    // if the value read into this field was zero, we need to skip N bytes and not read the values further.
                    FieldCountController[] countController = (FieldCountController[])field.GetCustomAttributes(typeof(FieldCountController), false);
                    if (countController.Length > 0)
                    {
                        long value = (long)Convert.ChangeType(field.GetValue(this), typeof(long));
                        if (value <= 0)
                        {
                            br.BaseStream.Position += countController[0].JunkSize;
                            break;
                        }
                    }
                }
            }
        }

        public TplSimpleReader()
        {

        }

        public TplSimpleReader(BinaryReader br)
        {
            LoadFromStream(br);
        }
    }

    public class TplClass : TplSimpleReader
    {
        public string Name;
        public float JunkFloat1;
        public float JunkFloat2;
        public float Price;
        public float Weight;
        public float Damage;
        public float ToHit;
        public float Defense;
        public float Absorbtion;
        public float MagicVolume;

        public TplClass(BinaryReader br) : base(br) { }

        public override string ToString() { return string.Format("TplClass[{0}]", Name); }
    }

    public class TplMaterial : TplClass
    {
        public TplMaterial(BinaryReader br) : base(br) { }

        public override string ToString() { return string.Format("TplMaterial[{0}]", Name); }
    }

    public class TplModifier : TplSimpleReader
    {
        public string Name;
        public ushort FieldCount;
        public int ManaCost;
        public int AffectMin;
        public int AffectMax;
        public int UsableBy;
        public int[] SlotsWarrior = new int[12];
        public int[] SlotsMage = new int[12];

        public TplModifier(BinaryReader br) : base(br) { }
    }

    public class TplArmor : TplSimpleReader
    {
        public string Name;
        public ushort FieldCount;
        public int Shape;
        public int Material;
        public int Price;
        public int Weight;
        public int Slot;
        public int AttackType;
        public int PhysicalMin;
        public int PhysicalMax;
        public int ToHit;
        public int Defense;
        public int Absorbtion;
        public int Range;
        public int Charge;
        public int Relax;
        public int TwoHanded;
        public int SuitableFor;
        public int OtherParam;
        public ushort[] ClassesAllowed = new ushort[8];

        public TplArmor(BinaryReader br) : base(br) { }

        public override string ToString() { return string.Format("TplArmor[{0}]", Name); }

        public bool IsAllowed(int cls, int material)
        {
            if (cls < 0)
            {
                for (int i = 0; i < 8; i++)
                    if (IsAllowed(i, material))
                        return true;
                return false;
            }

            return (ClassesAllowed[cls] & (1 << material)) != 0;
        }
    }

    public class TplMagicItem : TplSimpleReader
    {
        public string Name;
        public ushort FieldCount;
        public int Price;
        public int Weight;
        [FieldNullController]
        public string Effects = "";

        public TplMagicItem(BinaryReader br) : base(br) { }
    }

    public class TplMonster : TplSimpleReader
    {
        public bool IsIgnoringArmor { get { return (AttackType == 3); } }
        public bool IsWalking { get { return (MovementType == 1); } }
        public bool IsHovering { get { return (MovementType == 2); } }
        public bool IsFlying { get { return (MovementType == 3); } }

        public string Name;
        [FieldCountController(2)]
        public ushort FieldCount;
        public int Body;
        public int Reaction;
        public int Mind;
        public int Spirit;
        public int HealthMax;
        public int HealthRegeneration;
        public int ManaMax;
        public int ManaRegeneration;
        public int Speed;
        public int RotationSpeed;
        public int ScanRange;
        public int PhysicalMin;
        public int PhysicalMax;
        public int AttackType;
        public int ToHit;
        public int Defense;
        public int Absorbtion;
        public int Charge;
        public int Relax;
        public int ProtectionFire;
        public int ProtectionWater;
        public int ProtectionAir;
        public int ProtectionEarth;
        public int ProtectionAstral;
        public int ProtectionBlade;
        public int ProtectionAxe;
        public int ProtectionBludgeon;
        public int ProtectionPike;
        public int ProtectionShooting;
        public int TypeID;
        public int Face;
        public int TokenSize;
        public int MovementType;
        public int DyingTime;
        public int Withdraw;
        public int Wimpy;
        public int SeeInvisible;
        public int Experience;
        public int TreasureGold;
        public int TreasureGoldMin;
        public int TreasureGoldMax;
        public int TreasureItem;
        public int TreasureItemMin;
        public int TreasureItemMax;
        public int TreasureItemMask;
        public uint IntJunk1;
        public uint IntJunk2;
        public int Power;
        public int Spell1;
        public int Spell1Prob;
        public int Spell2;
        public int Spell2Prob;
        public int Spell3;
        public int Spell3Prob;
        public int SpellPower;
        public int ServerID;
        public uint KnownSpells;
        public int SkillFire;
        public int SkillWater;
        public int SkillAir;
        public int SkillEarth;
        public int SkillAstral;
        public string EquipItem1;
        public string EquipItem2;

        public TplMonster(BinaryReader br) : base(br) { }
    }

    public class TplHuman : TplSimpleReader
    {
        public string Name;
        [FieldCountController(10)] // 10 junk bytes if FieldCount is 0
        public ushort FieldCount;
        public int Body;
        public int Reaction;
        public int Mind;
        public int Spirit;
        public int HealthMax;
        public int ManaMax;
        public int Speed;
        public int RotationSpeed;
        public int ScanRange;
        public int Defense;
        public int Skill0;
        public int SkillBladeFire;
        public int SkillAxeWater;
        public int SkillBludgeonAir;
        public int SkillPikeEarth;
        public int SkillShootingAstral;
        public int TypeID;
        public int Face;
        public int Gender;
        public int Charge;
        public int Relax;
        public int TokenSize;
        public int MovementType;
        public int DyingTime;
        public int ServerID;
        public uint KnownSpells;
        public string[] EquipItems = new string[10];

        public TplHuman(BinaryReader br) : base(br) { }
    }

    public class TplStructure : TplSimpleReader
    {
        public string Name;
        public ushort FieldCount;
        public int Width;
        public int Height;
        public int ScanRange;
        public int HealthMax;
        public uint CanPass;
        public uint CanNotPass;

        public TplStructure(BinaryReader br) : base(br) { }
    }

    public class TplSpell : TplSimpleReader
    {
        public string Name;
        [FieldCountController(2)]
        public ushort FieldCount;
        public int Level;
        public int ManaCost;
        public int Sphere;
        public int Item;
        public int SpellTarget;
        public int DeliverySystem;
        public int MaxRange;
        public int EffectSpeed;
        public int Distribution;
        public int Radius;
        public int AreaEffect;
        public int AreaDuration;
        public int AreaFrequency;
        public int ApplyMethod;
        public int Duration;
        public int Frequency;
        public int DamageMin;
        public int DamageMax;
        public int Defensive;
        public int SkillOffset;
        public int ScrollCost;
        public int BookCost;
        public string Effects;

        public bool IsAreaSpell
        {
            get
            {
                return Radius >= 1 || (SpellTarget == 2 && ApplyMethod == 1);
            }
        }

        public TplSpell(BinaryReader br) : base(br) { }
    }

    public void LoadFromFile(string filename)
    {
        MemoryStream ms = ResourceManager.OpenRead("world/data/data.bin");
        if (ms == null)
        {
            Core.Abort("Couldn't load \"world/data/data.bin\"!");
            return;
        }

        ms.Position = 0;
        BinaryReader br = new BinaryReader(ms);
        try
        {
            LoadFromStream(br);
        }
        finally
        {
            br.Close();
        }
    }


    /// fields
    public List<TplClass> Classes = new List<TplClass>();
    public List<TplMaterial> Materials = new List<TplMaterial>();
    public List<TplModifier> Modifiers = new List<TplModifier>();
    public List<TplArmor> Armor = new List<TplArmor>();
    public List<TplArmor> Shields = new List<TplArmor>();
    public List<TplArmor> Weapons = new List<TplArmor>();
    public List<TplMagicItem> MagicItems = new List<TplMagicItem>();
    public List<TplMonster> Monsters = new List<TplMonster>();
    public List<TplHuman> Humans = new List<TplHuman>();
    public List<TplStructure> Structures = new List<TplStructure>();
    public List<TplSpell> Spells = new List<TplSpell>();


    public void LoadFromStream(BinaryReader br)
    {
        TplHeader headers = new TplHeader();

        headers.LoadFromStream(br);
        int numClasses = br.ReadInt32();
        for (int i = 0; i < numClasses; i++)
            Classes.Add(new TplClass(br));

        int numMaterials = br.ReadInt32();
        for (int i = 0; i < numMaterials; i++)
            Materials.Add(new TplMaterial(br));

        headers.LoadFromStream(br);
        int numModifiers = br.ReadInt32();
        for (int i = 0; i < numModifiers; i++)
            Modifiers.Add(new TplModifier(br));

        headers.LoadFromStream(br);
        int numArmor = br.ReadInt32() - 1;
        for (int i = 0; i < numArmor; i++)
            Armor.Add(new TplArmor(br));

        int numShields = br.ReadInt32() - 1;
        for (int i = 0; i < numShields; i++)
            Shields.Add(new TplArmor(br));

        int numWeapons = br.ReadInt32() - 1;
        for (int i = 0; i < numWeapons; i++)
            Weapons.Add(new TplArmor(br));

        headers.LoadFromStream(br);
        int numMagicItems = br.ReadInt32() - 1;
        for (int i = 0; i < numMagicItems; i++)
            MagicItems.Add(new TplMagicItem(br));

        headers.LoadFromStream(br);
        int numMonsters = br.ReadInt32() - 1;
        for (int i = 0; i < numMonsters; i++)
            Monsters.Add(new TplMonster(br));

        headers.LoadFromStream(br);
        int numHumans = br.ReadInt32() - 1;
        for (int i = 0; i < numHumans; i++)
            Humans.Add(new TplHuman(br));

        headers.LoadFromStream(br);
        int numStructures = br.ReadInt32() - 1;
        for (int i = 0; i < numStructures; i++)
            Structures.Add(new TplStructure(br));

        headers.LoadFromStream(br);
        int numSpells = br.ReadInt32() - 2;
        for (int i = 0; i < numSpells; i++)
            Spells.Add(new TplSpell(br));
    }
}

public static class TemplateLoader
{
    private static Templates _Templates = null;
    public static Templates Templates
    {
        get
        {
            if (_Templates == null)
                LoadTemplates();
            return _Templates;
        }
    }

    public static void LoadTemplates()
    {
        if (_Templates == null)
        {
            _Templates = new Templates();
            _Templates.LoadFromFile("world/data/data.bin");
        }
    }

    public static Templates.TplStructure GetStructureById(int typeId)
    {
        typeId--;
        // typeid now equals to index in this array.
        if (typeId >= 0 && typeId < Templates.Structures.Count)
            return Templates.Structures[typeId];
        return null;
    }

    public static Templates.TplStructure GetStructureByName(string name)
    {
        name = name.ToLower();
        foreach (Templates.TplStructure struc in Templates.Structures)
        {
            if (struc.Name.ToLower() == name)
                return struc;
        }

        return null;
    }

    public static Templates.TplMonster GetMonsterById(int serverId)
    {
        foreach (Templates.TplMonster unit in Templates.Monsters)
        {
            if (unit.ServerID == serverId)
                return unit;
        }

        return null;
    }

    public static Templates.TplMonster GetMonsterByName(string name)
    {
        name = name.ToLower();
        foreach (Templates.TplMonster unit in Templates.Monsters)
        {
            if (unit.Name.ToLower() == name)
                return unit;
        }

        return null;
    }

    public static Templates.TplHuman GetHumanById(int serverId)
    {
        foreach (Templates.TplHuman human in Templates.Humans)
        {
            if (human.ServerID == serverId)
                return human;
        }

        return null;
    }

    public static Templates.TplHuman GetHumanByName(string name)
    {
        name = name.ToLower();
        foreach (Templates.TplHuman human in Templates.Humans)
        {
            if (human.Name.ToLower() == name)
                return human;
        }

        return null;
    }

    public static Templates.TplMaterial GetMaterialById(int id)
    {
        if (id < 0 || id >= Templates.Materials.Count)
            return null;
        return Templates.Materials[id];
    }

    public static Templates.TplClass GetClassById(int id)
    {
        if (id < 0 || id >= Templates.Classes.Count)
            return null;
        return Templates.Classes[id];
    }

    public static Templates.TplArmor GetOptionByIdAndSlot(int id, int slot)
    {
        id -= 1;
        List<Templates.TplArmor> oList;

        switch (slot)
        {
            case 1:
                oList = Templates.Weapons;
                break;
            case 2:
                oList = Templates.Shields;
                break;
            default:
                oList = Templates.Armor;
                break;
        }

        if (id < 0 || id >= oList.Count)
            return null;
        return oList[id];
    }

    public static Templates.TplMagicItem GetMagicItemById(int id)
    {
        if (id < 0 || id >= Templates.MagicItems.Count)
            return null;

        return Templates.MagicItems[id];
    }

    public static Templates.TplModifier GetModifierById(int id)
    {
        if (id < 0 || id >= Templates.Modifiers.Count)
            return null;

        return Templates.Modifiers[id];
    }

    public static Templates.TplSpell GetSpellById(int id)
    {
        if (id < 0 || id >= Templates.Spells.Count)
            return null;

        return Templates.Spells[id];
    }
}