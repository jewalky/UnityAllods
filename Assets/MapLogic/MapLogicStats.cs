using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

// this class is used by items, monsters and humans.
// this class should be optimized because it gets sent over network.
// current size is ~80 bytes.
public class MapLogicStats
{
    public byte NoneStat; // dummy value
    public long Price;
    public short Body;
    public short Mind;
    public short Reaction;
    public short Spirit;
    public int Health;
    public int HealthMax;
    public short HealthRegeneration;
    public int Mana;
    public int ManaMax;
    public short ManaRegeneration;
    public short ToHit;
    public short DamageMin;
    public short DamageMax;
    public short Defence; // should use this name because it's used in data.bin
    public short Absorbtion;
    public byte Speed;
    public byte RotationSpeed;
    public float ScanRange;
    public byte Protection0; // dummy value
    public byte ProtectionFire;
    public byte ProtectionWater;
    public byte ProtectionAir;
    public byte ProtectionEarth;
    public byte ProtectionAstral;
    public byte FighterSkill0; // dummy value
    public byte SkillBlade;
    public byte SkillAxe;
    public byte SkillBludgeon;
    public byte SkillPike;
    public byte SkillShooting;
    public byte MageSkill0; // dummy value
    public byte SkillFire;
    public byte SkillWater;
    public byte SkillAir;
    public byte SkillEarth;
    public byte SkillAstral;
    public byte ItemLore; // dummy value
    public byte MagicLore; // dummy value
    public byte CreatureLore; // dummy value
    public byte CastSpell;
    public byte TeachSpell;
    public short Damage; // dummy value
    public short DamageFire;
    public short DamageWater;
    public short DamageAir;
    public short DamageEarth;
    public short DamageAstral;
    public short DamageBonus;
    // monster stats only
    public byte ProtectionBlade;
    public byte ProtectionAxe;
    public byte ProtectionBludgeon;
    public byte ProtectionPike;
    public byte ProtectionShooting;

    public static MapLogicStats operator +(MapLogicStats a, MapLogicStats b)
    {
        MapLogicStats outObj = new MapLogicStats();
        FieldInfo[] fields = typeof(MapLogicStats).GetFields();
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

    public static MapLogicStats operator -(MapLogicStats a, MapLogicStats b)
    {
        MapLogicStats outObj = new MapLogicStats();
        FieldInfo[] fields = typeof(MapLogicStats).GetFields();
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
        foreach (var field in typeof(MapLogicStats).GetFields())
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