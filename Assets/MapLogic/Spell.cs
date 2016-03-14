using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class Spell
{
    // order should be same as data.bin spells or locale spell.txt
    public enum Spells
    {
        NoneSpell = 0,
        Fire_Arrow,
        Fire_Ball,
        Wall_of_Fire,
        Protection_from_Fire,
        Ice_Arrow,
        Poison_Cloud,
        Blizzard,
        Protection_from_Water,
        Acid_Spray,
        Lightning,
        Prismatic_Spray,
        Invisibility,
        Protection_from_Air,
        Darkness,
        Light,
        Diamond_Dust,
        Wall_of_Earth,
        Stone_Curse,
        Protection_from_Earth,
        Bless,
        Haste,
        Control_Spirit,
        Teleport,
        Heal,
        Summon,
        Drain_Life,
        Shield,
        Curse,
        Slow
    }

    public MapUnit User = null;
    public Templates.TplSpell Template;

    private int OwnSkill = 0;
    public int Skill
    {
        get
        {
            if (User != null)
            {
                switch (Template.Sphere)
                {
                    case 1:
                        return User.Stats.SkillFire;
                    case 2:
                        return User.Stats.SkillWater;
                    case 3:
                        return User.Stats.SkillAir;
                    case 4:
                        return User.Stats.SkillEarth;
                    case 5:
                        return User.Stats.SkillAstral;
                    default:
                        return OwnSkill;
                }
            }
            else return OwnSkill;
        }

        set
        {
            if (User != null && Template.Sphere >= 1 && Template.Sphere <= 5)
                return;
            OwnSkill = value;
        }
    }

    public Spell(int id)
    {
        Template = TemplateLoader.GetSpellById(id);
        if (Template == null)
            Debug.LogFormat("Invalid spell created (id={0})", id);
    }
}
