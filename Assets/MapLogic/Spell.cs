using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using Spells;

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
        Ice_Missile,
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
    public Item Item = null;
    public Templates.TplSpell Template;
    public Spells SpellID = 0;

    private int OwnSkill = 0;
    public int Skill
    {
        get
        {
            if (User != null && Item == null)
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

    private static List<Spell.Spells> AttackSpells = new List<Spell.Spells>(new Spell.Spells[]
    {
        Spells.Fire_Arrow,
        Spells.Fire_Ball,
        Spells.Wall_of_Fire,
        Spells.Lightning,
        Spells.Prismatic_Spray,
        Spells.Diamond_Dust,
        Spells.Stone_Curse,
        Spells.Ice_Missile,
        Spells.Poison_Cloud,
        Spells.Blizzard,
        Spells.Acid_Spray,
        Spells.Curse,
        Spells.Darkness,
        Spells.Slow,
        Spells.Drain_Life
    });

    public static bool IsAttackSpell(Spells id)
    {
        return AttackSpells.Contains(id);
    }

    public Spell(int id, MapUnit unit = null)
    {
        SpellID = (Spells)id;
        Template = TemplateLoader.GetSpellById(id-1);
        if (Template == null)
            Debug.LogFormat("Invalid spell created (id={0})", id);
        User = unit;
    }

    public SpellProc Cast(int tgX, int tgY, MapUnit tgUnit = null)
    {
        // find spellproc instance for this spell
        // 
        Type spt = SpellProc.FindProcTypeFromSpell(this);
        if (spt == null)
            return null;

        try
        {
            ConstructorInfo ci = spt.GetConstructor(new Type[] { typeof(Spell), typeof(int), typeof(int), typeof(MapUnit) });
            SpellProc sp = (SpellProc)ci.Invoke(new object[] { this, tgX, tgY, tgUnit });
            return sp;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public float GetDistance()
    {
        // maxRange is base range
        // on top of that, skill / 30 is added for regular spells, and skill / 3 for teleport
        if (SpellID == Spells.Teleport)
            return 7 + Skill / 3;
        return 7 + Skill / 30;
    }
}
