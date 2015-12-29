using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// this enum duplicates MapLogicStats.
// it does NOT need to be synced when you edit MapLogicStats,
// but it should be synced with locale/stats.txt or item stats won't be displayed correctly.
public class ItemEffect
{
    public enum Effects
    {
        NoneStat = 0, // dummy value
        Price,
        Body,
        Mind,
        Reaction,
        Spirit,
        Health,
        HealthMax,
        HealthRegeneration,
        Mana,
        ManaMax,
        ManaRegeneration,
        ToHit,
        DamageMin,
        DamageMax,
        Defence, // should use this name because it's used in data.bin
        Absorbtion,
        Speed,
        RotationSpeed,
        ScanRange,
        Protection0, // dummy value
        ProtectionFire,
        ProtectionWater,
        ProtectionAir,
        ProtectionEarth,
        ProtectionAstral,
        FighterSkill0, // dummy value
        SkillBlade,
        SkillAxe,
        SkillBludgeon,
        SkillPike,
        SkillShooting,
        MageSkill0, // dummy value
        SkillFire,
        SkillWater,
        SkillAir,
        SkillEarth,
        SkillAstral,
        ItemLore, // apparently distance?
        MagicLore, // dummy value
        CreatureLore, // dummy value
        CastSpell,
        TeachSpell,
        Damage, // dummy value
        DamageFire,
        DamageWater,
        DamageAir,
        DamageEarth,
        DamageAstral,
        DamageBonus,
        // monster stats only
        ProtectionBlade,
        ProtectionAxe,
        ProtectionBludgeon,
        ProtectionPike,
        ProtectionShooting
    }


    Effects Type1 = Effects.NoneStat;
    int Value1 = 0;

    // only in castSpell
    Effects Type2 = Effects.NoneStat;
    int Value2 = 0;

    public ItemEffect()
    {
        // wat
    }

    public ItemEffect(string effect)
    {
        string[] efs = effect.Split('='); // yes you can write castSpell=Stone_Curse=30 lol.
        if (efs.Length != 2)
            return;

        try
        {
            Type1 = (Effects)Enum.Parse(typeof(Effects), efs[0], true);
        }
        catch (Exception)
        {
            return;
        }

        if (Type1 == Effects.CastSpell || Type1 == Effects.TeachSpell)
        {
            string[] efs2 = efs[1].Split(':');
            if (efs2.Length != 2)
            {
                Type1 = Effects.NoneStat;
                return;
            }

            try
            {
                int spell = (int)Enum.Parse(typeof(Spell.Spells), efs2[0], true);
                Value1 = spell;
            }
            catch (Exception)
            {
                Type1 = Effects.NoneStat;
                return;
            }

            if (Type1 == Effects.CastSpell)
            {
                Type2 = Effects.MageSkill0;
                try
                {
                    Value2 = int.Parse(efs2[1]);
                }
                catch (Exception)
                {
                    Type1 = Effects.NoneStat;
                    Value1 = 0;
                    Type2 = Effects.NoneStat;
                    Value2 = 0;
                }
            }
        }
        else
        {
            try
            {
                Value1 = int.Parse(efs[1]);
            }
            catch (Exception)
            {
                Type1 = Effects.NoneStat;
                Value1 = 0;
            }
        }
    }

    /*MapLogicItemEffect eff = new MapLogicItemEffect("castSpell=Stone_Curse:255");
    Debug.Log(eff.ToString());*/ // says "CastSpell=Stone_Curse:255"
    public override string ToString()
    {
        string effect = Type1.ToString();
        string value;
        if (Type1 == Effects.TeachSpell || Type1 == Effects.CastSpell)
        {
            value = ((Spell.Spells)Value1).ToString();
            if (Type1 == Effects.CastSpell)
                value += string.Format(":{0}", Value2);
        }
        else value = Value1.ToString();
        return string.Format("{0}={1}", effect, value);
    }
}

public class Item
{
    // stats are already in Templates.
    // power for castSpell is stored in paired mageSkill0.
    // so basically castSpell=Stone_Curse:10 translates into castSpell=21;mageSkill0=10
    // just in case, I have an local enum that translates to stat number.

}
