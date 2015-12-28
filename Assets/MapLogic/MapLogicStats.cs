using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// this class is used by items, monsters and humans.
// this class should be optimized because it gets sent over network.
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
}