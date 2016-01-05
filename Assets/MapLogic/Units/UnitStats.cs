using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using ProtoBuf;

// this class is used by items, monsters and humans.
// this class should be optimized because it gets sent over network.
// current size is ~80 bytes.
[ProtoContract]
public struct UnitStats
{
    public bool TrySetHealth(int nh)
    {
        int oh = Health;
        Health = Math.Min(HealthMax, Math.Max(-10, nh));
        return (oh != Health);
    }

    public bool TrySetMana(int nm)
    {
        int om = Mana;
        Mana = Math.Min(HealthMax, Math.Max(0, nm));
        return (om != Mana);
    }

    [ProtoMember(1)] public byte NoneStat; // dummy value
    [ProtoMember(2)] public long Price;
    [ProtoMember(3)] public short Body;
    [ProtoMember(4)] public short Mind;
    [ProtoMember(5)] public short Reaction;
    [ProtoMember(6)] public short Spirit;
    [ProtoMember(7)] public int Health;
    [ProtoMember(8)] public int HealthMax;
    [ProtoMember(9)] public short HealthRegeneration;
    [ProtoMember(10)] public int Mana;
    [ProtoMember(11)] public int ManaMax;
    [ProtoMember(12)] public short ManaRegeneration;
    [ProtoMember(13)] public short ToHit;
    [ProtoMember(14)] public short DamageMin;
    [ProtoMember(15)] public short DamageMax;
    [ProtoMember(16)] public short Defence; // should use this name because it's used in data.bin
    [ProtoMember(17)] public short Absorbtion;
    [ProtoMember(18)] public byte Speed;
    [ProtoMember(19)] public byte RotationSpeed;
    [ProtoMember(20)] public float ScanRange;
    [ProtoMember(21)] public byte Protection0; // dummy value
    [ProtoMember(22)] public byte ProtectionFire;
    [ProtoMember(23)] public byte ProtectionWater;
    [ProtoMember(24)] public byte ProtectionAir;
    [ProtoMember(25)] public byte ProtectionEarth;
    [ProtoMember(26)] public byte ProtectionAstral;
    [ProtoMember(27)] public byte FighterSkill0; // dummy value
    [ProtoMember(28)] public byte SkillBlade;
    [ProtoMember(29)] public byte SkillAxe;
    [ProtoMember(30)] public byte SkillBludgeon;
    [ProtoMember(31)] public byte SkillPike;
    [ProtoMember(32)] public byte SkillShooting;
    [ProtoMember(33)] public byte MageSkill0; // dummy value
    [ProtoMember(34)] public byte SkillFire;
    [ProtoMember(35)] public byte SkillWater;
    [ProtoMember(36)] public byte SkillAir;
    [ProtoMember(37)] public byte SkillEarth;
    [ProtoMember(38)] public byte SkillAstral;
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
    [ProtoMember(50)] public short DamageBonus;
    // monster stats only
    [ProtoMember(51)] public byte ProtectionBlade;
    [ProtoMember(52)] public byte ProtectionAxe;
    [ProtoMember(53)] public byte ProtectionBludgeon;
    [ProtoMember(54)] public byte ProtectionPike;
    [ProtoMember(55)] public byte ProtectionShooting;

    public static UnitStats operator +(UnitStats a, UnitStats b)
    {
        UnitStats outObj = new UnitStats();
        FieldInfo[] fields = typeof(UnitStats).GetFields();
        foreach (FieldInfo field in fields)
        {
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

        return outObj;
    }

    public static UnitStats operator -(UnitStats a, UnitStats b)
    {
        UnitStats outObj = new UnitStats();
        FieldInfo[] fields = typeof(UnitStats).GetFields();
        foreach (FieldInfo field in fields)
        {
            // we process byte, short, int, float and long.
            // handle overflows.
            if (field.FieldType == typeof(byte))
            {
                int fa = (byte)field.GetValue(a);
                int fb = (byte)field.GetValue(b);
                field.SetValue(outObj, (byte)Math.Min(fa - fb, 255));
            }
            else if (field.FieldType == typeof(short))
            {
                int fa = (short)field.GetValue(a);
                int fb = (short)field.GetValue(b);
                field.SetValue(outObj, (short)Math.Max(Math.Min(fa - fb, 32767), -32768));
            }
            else if (field.FieldType == typeof(int))
            {
                long fa = (int)field.GetValue(a);
                long fb = (int)field.GetValue(b);
                field.SetValue(outObj, (int)Math.Max(Math.Min(fa - fb, 2147483647), -2147483648));
            }
            else if (field.FieldType == typeof(long))
            {
                long fa = (long)field.GetValue(a);
                long fb = (long)field.GetValue(b);
                field.SetValue(outObj, fa - fb);
            }
            else if (field.FieldType == typeof(float))
            {
                float fa = (float)field.GetValue(a);
                float fb = (float)field.GetValue(b);
                field.SetValue(outObj, fa - fb);
            }
        }

        return outObj;
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
}