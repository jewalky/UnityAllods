using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using ProtoBuf;
using UnityEngine;
using System.IO;

// this class is used by items, monsters and humans.
// this class should be optimized because it gets sent over network.
// current size is ~80 bytes.
[ProtoContract]
public class UnitStats
{
    [Flags]
    public enum ModifiedFlags
    {
        BRMS                = 0x0001,
        HealthMana          = 0x0002,
        Speed               = 0x0004, // speed/rotationspeed
        ScanRange           = 0x0008,
        Physical            = 0x0010, // ToHit/Damage/Absorption/Defense, DamageBonus
        MageSkill           = 0x0020, // fire/water/air/earth/astral
        FighterSkill        = 0x0040, // blade/axe/bludgeon/pike/shooting
        ProtPhysical        = 0x0080,
        ProtElemental       = 0x0100,
        Bless               = 0x0200,
    }

    private class ModFlags : Attribute
    {
        public ModifiedFlags Flags { get; private set; }

        public ModFlags(ModifiedFlags flags)
        {
            Flags = flags;
        }
    }

    private class PreserveStats : Attribute { }

    public bool TrySetHealth(int nh)
    {
        int oh = Health;
        Health = Math.Min(HealthMax, Math.Max(-10, nh));
        return (oh != Health);
    }

    public bool TrySetMana(int nm)
    {
        int om = Mana;
        Mana = Math.Min(ManaMax, Math.Max(0, nm));
        return (om != Mana);
    }

    [ProtoMember(1)] public byte NoneStat; // dummy value
    [ProtoMember(2)] public long Price;
    [ModFlags(ModifiedFlags.BRMS)][ProtoMember(3)] public short Body;
    [ModFlags(ModifiedFlags.BRMS)][ProtoMember(4)] public short Mind;
    [ModFlags(ModifiedFlags.BRMS)][ProtoMember(5)] public short Reaction;
    [ModFlags(ModifiedFlags.BRMS)][ProtoMember(6)] public short Spirit;
    [PreserveStats][ProtoMember(7)] public int Health;
    [ModFlags(ModifiedFlags.HealthMana)][ProtoMember(8)] public int HealthMax;
    [ModFlags(ModifiedFlags.HealthMana)][ProtoMember(9)] public short HealthRegeneration;
    [PreserveStats][ProtoMember(10)] public int Mana;
    [ModFlags(ModifiedFlags.HealthMana)][ProtoMember(11)] public int ManaMax;
    [ModFlags(ModifiedFlags.HealthMana)][ProtoMember(12)] public short ManaRegeneration;
    [ModFlags(ModifiedFlags.Physical)][ProtoMember(13)] public short ToHit;
    [ModFlags(ModifiedFlags.Physical)][ProtoMember(14)] public short DamageMin;
    [ModFlags(ModifiedFlags.Physical)][ProtoMember(15)] public short DamageMax;
    [ModFlags(ModifiedFlags.Physical)][ProtoMember(16)] public short Defence; // should use this name because it's used in data.bin
    [ModFlags(ModifiedFlags.Physical)][ProtoMember(17)] public short Absorbtion;
    [ProtoMember(18)] public byte Speed;
    [ProtoMember(19)] public byte RotationSpeed;
    [ModFlags(ModifiedFlags.ScanRange)][ProtoMember(20)] public float ScanRange;
    [ProtoMember(21)] public byte Protection0; // dummy value
    [ModFlags(ModifiedFlags.ProtElemental)][ProtoMember(22)] public byte ProtectionFire;
    [ModFlags(ModifiedFlags.ProtElemental)][ProtoMember(23)] public byte ProtectionWater;
    [ModFlags(ModifiedFlags.ProtElemental)][ProtoMember(24)] public byte ProtectionAir;
    [ModFlags(ModifiedFlags.ProtElemental)][ProtoMember(25)] public byte ProtectionEarth;
    [ModFlags(ModifiedFlags.ProtElemental)][ProtoMember(26)] public byte ProtectionAstral;
    [ProtoMember(27)] public byte FighterSkill0; // dummy value
    [ModFlags(ModifiedFlags.FighterSkill)][ProtoMember(28)] public byte SkillBlade;
    [ModFlags(ModifiedFlags.FighterSkill)][ProtoMember(29)] public byte SkillAxe;
    [ModFlags(ModifiedFlags.FighterSkill)][ProtoMember(30)] public byte SkillBludgeon;
    [ModFlags(ModifiedFlags.FighterSkill)][ProtoMember(31)] public byte SkillPike;
    [ModFlags(ModifiedFlags.FighterSkill)][ProtoMember(32)] public byte SkillShooting;
    [ProtoMember(33)] public byte MageSkill0; // dummy value
    [ModFlags(ModifiedFlags.MageSkill)][ProtoMember(34)] public byte SkillFire;
    [ModFlags(ModifiedFlags.MageSkill)][ProtoMember(35)] public byte SkillWater;
    [ModFlags(ModifiedFlags.MageSkill)][ProtoMember(36)] public byte SkillAir;
    [ModFlags(ModifiedFlags.MageSkill)][ProtoMember(37)] public byte SkillEarth;
    [ModFlags(ModifiedFlags.MageSkill)][ProtoMember(38)] public byte SkillAstral;
    [ProtoMember(39)] public byte ItemLore; // dummy value
    [ProtoMember(40)] public byte MagicLore; // dummy value
    [ProtoMember(41)] public byte CreatureLore; // dummy value
    [ProtoMember(42)] public byte CastSpell;
    [ProtoMember(43)] public byte TeachSpell;
    [ProtoMember(44)] public short Damage; // dummy value
    [ProtoMember(45)] public short DamageFire;
    [ProtoMember(46)] public short DamageWater;
    [ProtoMember(47)] public short DamageAir;
    [ProtoMember(48)] public short DamageEarth;
    [ProtoMember(49)] public short DamageAstral;
    [ModFlags(ModifiedFlags.Physical)][ProtoMember(50)] public short DamageBonus;
    // monster stats only
    [ModFlags(ModifiedFlags.ProtPhysical)][ProtoMember(51)] public byte ProtectionBlade;
    [ModFlags(ModifiedFlags.ProtPhysical)][ProtoMember(52)] public byte ProtectionAxe;
    [ModFlags(ModifiedFlags.ProtPhysical)][ProtoMember(53)] public byte ProtectionBludgeon;
    [ModFlags(ModifiedFlags.ProtPhysical)][ProtoMember(54)] public byte ProtectionPike;
    [ModFlags(ModifiedFlags.ProtPhysical)][ProtoMember(55)] public byte ProtectionShooting;
    [ModFlags(ModifiedFlags.Bless)] [ProtoMember(56)] public byte Bless;
    [ModFlags(ModifiedFlags.Bless)] [ProtoMember(57)] public byte Curse;

    public UnitStats()
    {
        // do nothing
    }

    public UnitStats(UnitStats other)
    {
        FieldInfo[] fields = typeof(UnitStats).GetFields();
        foreach (FieldInfo field in fields)
        {
            // we process byte, short, int, float and long.
            // handle overflows.
            if (field.FieldType == typeof(byte) ||
                field.FieldType == typeof(short) ||
                field.FieldType == typeof(int) ||
                field.FieldType == typeof(long) ||
                field.FieldType == typeof(float))
            {
                field.SetValue(this, field.GetValue(other));
            }
        }
    }

    public void MergeEffects(List<ItemEffect> effects)
    {
        foreach (ItemEffect eff in effects)
        {
            if (eff.Type1 == ItemEffect.Effects.CastSpell ||
                eff.Type1 == ItemEffect.Effects.TeachSpell) continue;

            // get stat
            FieldInfo field = typeof(UnitStats).GetField(eff.Type1.ToString());
            if (field == null)
                continue;

            if (field.FieldType == typeof(byte))
            {
                int fa = (byte)field.GetValue(this);
                int fb = eff.Value1;
                field.SetValue(this, (byte)Math.Min(fa + fb, 255));
            }
            else if (field.FieldType == typeof(short))
            {
                int fa = (short)field.GetValue(this);
                int fb = eff.Value1;
                field.SetValue(this, (short)Math.Max(Math.Min(fa + fb, 32767), -32768));
            }
            else if (field.FieldType == typeof(int))
            {
                long fa = (int)field.GetValue(this);
                long fb = eff.Value1;
                field.SetValue(this, (int)Math.Max(Math.Min(fa + fb, 2147483647), -2147483648));
            }
            else if (field.FieldType == typeof(long))
            {
                long fa = (long)field.GetValue(this);
                long fb = eff.Value1;
                field.SetValue(this, fa + fb);
            }
            else if (field.FieldType == typeof(float))
            {
                float fa = (float)field.GetValue(this);
                float fb = eff.Value1;
                field.SetValue(this, fa + fb);
            }
        }
    }

    //public static UnitStats operator +(UnitStats a, UnitStats b)
    public void MergeStats(UnitStats b)
    {
        UnitStats a = this;
        UnitStats outObj = this;
        FieldInfo[] fields = typeof(UnitStats).GetFields();
        foreach (FieldInfo field in fields)
        {
            PreserveStats[] ps = (PreserveStats[])field.GetCustomAttributes(typeof(PreserveStats), false);
            if (ps.Length > 0)
                continue;

            ModFlags[] mf = (ModFlags[])field.GetCustomAttributes(typeof(ModFlags), false);
            ModifiedFlags applyMF = 0;
            foreach (var singleMF in mf)
                applyMF |= singleMF.Flags;

            // we process byte, short, int, float and long.
            // handle overflows.
            if (field.FieldType == typeof(byte))
            {
                int fa = (byte)field.GetValue(a);
                int fb = (byte)field.GetValue(b);
                field.SetValue(outObj, (byte)Math.Min(fa + fb, 255));
            }
            else if (field.FieldType == typeof(short))
            {
                int fa = (short)field.GetValue(a);
                int fb = (short)field.GetValue(b);
                field.SetValue(outObj, (short)Math.Max(Math.Min(fa + fb, 32767), -32768));
            }
            else if (field.FieldType == typeof(int))
            {
                long fa = (int)field.GetValue(a);
                long fb = (int)field.GetValue(b);
                field.SetValue(outObj, (int)Math.Max(Math.Min(fa + fb, 2147483647), -2147483648));
            }
            else if (field.FieldType == typeof(long))
            {
                long fa = (long)field.GetValue(a);
                long fb = (long)field.GetValue(b);
                field.SetValue(outObj, fa + fb);
            }
            else if (field.FieldType == typeof(float))
            {
                float fa = (float)field.GetValue(a);
                float fb = (float)field.GetValue(b);
                field.SetValue(outObj, fa + fb);
            }
        }
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        foreach (var field in typeof(UnitStats).GetFields())
        {
            if (field.FieldType == typeof(byte))
                sb.Append(string.Format("byte {0} = {1}\n", field.Name, field.GetValue(this)));
            else if (field.FieldType == typeof(short))
                sb.Append(string.Format("short {0} = {1}\n", field.Name, field.GetValue(this)));
            else if (field.FieldType == typeof(int))
                sb.Append(string.Format("int {0} = {1}\n", field.Name, field.GetValue(this)));
            else if (field.FieldType == typeof(long))
                sb.Append(string.Format("long {0} = {1}\n", field.Name, field.GetValue(this)));
            else if (field.FieldType == typeof(float))
                sb.Append(string.Format("float {0} = {1}\n", field.Name, field.GetValue(this)));
        }
        return sb.ToString().Trim();
    }
    
    // PackStats creates a compacted serialized UnitStats struct (for networking) which can be later merged using MergePackedStats
    public byte[] PackStats(ModifiedFlags flags)
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter bw = new BinaryWriter(stream))
        {
            bw.Write((uint)flags);
            FieldInfo[] fields = typeof(UnitStats).GetFields();
            foreach (FieldInfo field in fields)
            {
                ModFlags[] mf = (ModFlags[])field.GetCustomAttributes(typeof(ModFlags), false);
                if (mf.Length <= 0)
                    continue;

                ModifiedFlags applyMF = 0;
                foreach (var singleMF in mf)
                    applyMF |= singleMF.Flags;

                if ((applyMF & flags) == 0)
                    continue;

                // we process byte, short, int, float and long.
                if (field.FieldType == typeof(byte))
                    bw.Write((byte)field.GetValue(this));
                else if (field.FieldType == typeof(short))
                    bw.Write((short)field.GetValue(this));
                else if (field.FieldType == typeof(int))
                    bw.Write((int)field.GetValue(this));
                else if (field.FieldType == typeof(float))
                    bw.Write((float)field.GetValue(this));
                else if (field.FieldType == typeof(long))
                    bw.Write((long)field.GetValue(this));
            }

            return stream.ToArray();
        }
    }

    // applies packed stats structure produced by PackStats
    public void MergePackedStats(byte[] packedStats)
    {
        using (MemoryStream stream = new MemoryStream(packedStats))
        using (BinaryReader br = new BinaryReader(stream))
        {
            ModifiedFlags flags = (ModifiedFlags)br.ReadUInt32();
            FieldInfo[] fields = typeof(UnitStats).GetFields();
            foreach (FieldInfo field in fields)
            {
                ModFlags[] mf = (ModFlags[])field.GetCustomAttributes(typeof(ModFlags), false);
                if (mf.Length <= 0)
                    continue;

                ModifiedFlags applyMF = 0;
                foreach (var singleMF in mf)
                    applyMF |= singleMF.Flags;

                if ((applyMF & flags) == 0)
                    continue;

                // we process byte, short, int, float and long.
                if (field.FieldType == typeof(byte))
                    field.SetValue(this, br.ReadByte());
                else if (field.FieldType == typeof(short))
                    field.SetValue(this, br.ReadInt16());
                else if (field.FieldType == typeof(int))
                    field.SetValue(this, br.ReadInt32());
                else if (field.FieldType == typeof(float))
                    field.SetValue(this, br.ReadSingle());
                else if (field.FieldType == typeof(long))
                    field.SetValue(this, br.ReadInt64());
            }
        }
    }

    // compares with another UnitStats, returns flags. checks only fields with ModFlags attribute
    public ModifiedFlags CompareStats(UnitStats other)
    {
        ModifiedFlags flags = 0;
        FieldInfo[] fields = typeof(UnitStats).GetFields();
        foreach (FieldInfo field in fields)
        {
            ModFlags[] mf = (ModFlags[])field.GetCustomAttributes(typeof(ModFlags), false);
            if (mf.Length <= 0)
                continue;

            ModifiedFlags applyMF = 0;
            foreach (var singleMF in mf)
                applyMF |= singleMF.Flags;

            if ((flags & applyMF) != 0) // already checked
                continue;

            if (field.GetValue(this) != field.GetValue(other))
                flags |= applyMF;
        }

        return flags;
    }
}