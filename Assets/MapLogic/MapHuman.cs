using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class MapHuman : MapUnit
{
    public override MapObjectType GetObjectType() { return MapObjectType.Human; }
    protected override Type GetGameObjectType() { return typeof(MapViewHuman); }

    // this unit type has its own template
    private Templates.TplHuman Template;

    [Flags]
    public enum GenderFlags
    {
        Fighter = 0x0001,
        Mage = 0x0002,
        Male = 0x0010,
        Female = 0x0020,

        MaleFighter = Male | Fighter,
        FemaleFighter = Female | Fighter,
        MaleMage = Male | Mage,
        FemaleMage = Female | Mage
    }

    public GenderFlags Gender { get; private set; }
    public bool IsHero { get; private set; }

    public enum ExperienceSkill
    {
        Fire = 0,
        Blade = 0,
        Water = 1,
        Axe = 1,
        Air = 2,
        Bludgeon = 2,
        Earth = 3,
        Pike = 3,
        Astral = 4,
        Shooting = 4
    }

    public ExperienceSkill MainSkill = ExperienceSkill.Fire;
    private int[] Experience = new int[5] { 0, 0, 0, 0, 0 };

    public MapHuman(int serverId, bool hero = false)
    {
        IsHero = hero;
        Template = TemplateLoader.GetHumanById(serverId);
        if (Template == null)
            Debug.LogFormat("Invalid human created (serverId={0})", serverId);
        else InitHuman();
    }

    public MapHuman(string name, bool hero = false)
    {
        IsHero = hero;
        Template = TemplateLoader.GetHumanByName(name);
        if (Template == null)
            Debug.LogFormat("Invalid human created (name={0})", name);
        else InitHuman();
    }

    private void InitHuman()
    {
        InitBaseUnit();

        Class = UnitClassLoader.GetUnitClassById(Template.TypeID);
        if (Class == null)
        {
            Debug.LogFormat("Invalid unit created (class not found, serverId={0}, typeId={1})", Template.ServerID, Template.TypeID);
            Template = null;
            return;
        }

        Width = Math.Max(1, Template.TokenSize);
        Height = Width;

        CoreStats.Health = CoreStats.HealthMax = Math.Max(Template.HealthMax, 0);
        CoreStats.Mana = CoreStats.ManaMax = Math.Max(Template.ManaMax, 0); // they sometimes put -1 as mana counter for fighters

        // BRMS
        CoreStats.Body = (short)Template.Body;
        CoreStats.Reaction = (short)Template.Reaction;
        CoreStats.Mind = (short)Template.Mind;
        CoreStats.Spirit = (short)Template.Spirit;

        // speed and scanrange
        CoreStats.RotationSpeed = (byte)Template.RotationSpeed;
        if (CoreStats.RotationSpeed < 1)
            CoreStats.RotationSpeed = 1;
        CoreStats.Speed = (byte)Template.Speed;
        if (CoreStats.Speed < 1)
            CoreStats.Speed = 1;
        CoreStats.ScanRange = Template.ScanRange;

        // human specific
        if (Template.Gender == 1)
            Gender = GenderFlags.Female;
        else Gender = GenderFlags.Male;
        // guess class (mage/fighter) from type
        if (Class.ID == 24 || Class.ID == 23) // unarmed mage, mage with staff
            Gender |= GenderFlags.Mage;
        else Gender |= GenderFlags.Fighter; // otherwise its a fighter.

        // initial items
        for (int i = 0; i < Template.EquipItems.Length; i++)
        {
            if (Template.EquipItems[i].Length <= 0)
                continue;

            Item item = new Item(Template.EquipItems[i]);
            if (!item.IsValid || item.Class.IsSpecial)
                continue;

            PutItemToBody((BodySlot)item.Class.Option.Slot, item);
        }

        // spellbook
        for (int i = 0; i < 32; i++)
        {
            uint sp = 1u << i;
            if (Template.ManaMax > 0/* && (Template.KnownSpells & sp) != 0*/)  // [ZZ] uncomment for production!!! currently enables all spells on unit
            {
                Spell cspell = new Spell(i, this);
                SpellBook.Add(cspell);
            }
        }

        // set skills by experience
        // [ZZ] I know that magic and fighting skills are exactly same in the array.
        //      this is written this way in case it changes. will be optimized later
        if ((Gender & GenderFlags.Fighter) != 0)
        {
            SetSkill(ExperienceSkill.Blade, Template.SkillBladeFire);
            SetSkill(ExperienceSkill.Axe, Template.SkillAxeWater);
            SetSkill(ExperienceSkill.Bludgeon, Template.SkillBludgeonAir);
            SetSkill(ExperienceSkill.Pike, Template.SkillPikeEarth);
            SetSkill(ExperienceSkill.Shooting, Template.SkillShootingAstral);
        }
        else if ((Gender & GenderFlags.Mage) != 0)
        {
            SetSkill(ExperienceSkill.Fire, Template.SkillBladeFire);
            SetSkill(ExperienceSkill.Water, Template.SkillAxeWater);
            SetSkill(ExperienceSkill.Air, Template.SkillBludgeonAir);
            SetSkill(ExperienceSkill.Earth, Template.SkillPikeEarth);
            SetSkill(ExperienceSkill.Astral, Template.SkillShootingAstral);
        }

        CalculateVision();
        UpdateItems();

        // fix health and mana
        Stats.TrySetHealth(Stats.HealthMax);
        Stats.TrySetMana(Stats.ManaMax);

        // add item for testing
        if (!NetworkManager.IsClient)
        {
            ItemsPack.PutItem(ItemsPack.Count, new Item("Very Rare Meteoric Amulet {skillfire=100,skillwater=100,skillair=100,skillearth=100,skillastral=100,manamax=16000}")); // for testing mage
            ItemsPack.PutItem(ItemsPack.Count, new Item("Very Rare Crystal Ring {body=3,scanrange=1,spirit=1}"));
            ItemsPack.PutItem(ItemsPack.Count, new Item("Very Rare Crystal Amulet {body=3,scanrange=1,spirit=1}"));
            ItemsPack.PutItem(ItemsPack.Count, new Item("Very Rare Dragon Leather Large Shield {body=3,protectionearth=20,damagebonus=20}"));
            ItemsPack.PutItem(ItemsPack.Count, new Item("Very Rare Crystal Plate Helm {body=3,scanrange=2}"));
            ItemsPack.PutItem(ItemsPack.Count, new Item("Very Rare Crystal Plate Cuirass {body=3}"));
            ItemsPack.PutItem(ItemsPack.Count, new Item("Very Rare Crystal Plate Bracers {body=3}"));
            ItemsPack.PutItem(ItemsPack.Count, new Item("Very Rare Crystal Scale Gauntlets {body=3}"));
            ItemsPack.PutItem(ItemsPack.Count, new Item("Very Rare Crystal Plate Boots {body=3}"));
            ItemsPack.PutItem(ItemsPack.Count, new Item("Very Rare Crystal Pike {tohit=500,damagemin=10,damagemax=20}"));
            ItemsPack.PutItem(ItemsPack.Count, new Item("Very Rare Meteoric Crossbow"));
        }
    }

    public override bool IsItemUsable(Item item)
    {
        if (item == null)
            return false;
        if (!item.IsValid)
            return false;
        if (((Gender & GenderFlags.Mage) != 0) && !item.Class.UsableMage)
            return false;
        if (((Gender & GenderFlags.Fighter) != 0) && !item.Class.UsableFighter)
            return false;
        return true;
    }

    // these functions are used in ROM2
    private static float Pow11(float v)
    {
        return Mathf.Pow(1.1f, v);
    }

    private static float Log11(float v)
    {
        return Mathf.Log(v) / Mathf.Log(1.1f);
    }

    public override void UpdateItems()
    {
        if (IsHero)
        {
            Item cuirass = GetItemFromBody(BodySlot.CuirassCloak);
            Item weapon = GetItemFromBody(BodySlot.Weapon);
            Item shield = GetItemFromBody(BodySlot.Shield);
            int isLightArmored = 0;
            if (cuirass != null)
                isLightArmored = UnitClassLoader.HeroMaterials[cuirass.Class.MaterialID];
            // now if we are a mage, then we either have armed or unarmed sprite.
            // if we're a fighter, we should pick appropriate version of the sprite instead.
            UnitClass newClass = null;
            if ((Gender & GenderFlags.Mage) != 0)
            {
                newClass = (weapon != null) ? UnitClassLoader.HeroMageSt[1] : UnitClassLoader.HeroMage[1]; // always heroes. heroes_l mages don't exist!
            }
            else if (weapon == null)
            {
                newClass = UnitClassLoader.HeroUnarmed[isLightArmored];
            }
            else
            {
                Templates.TplArmor weaponKind = weapon.Class.Option;
                switch (weaponKind.AttackType)
                {
                    case 1: // sword
                        if (weaponKind.TwoHanded == 2)
                            newClass = UnitClassLoader.HeroSwordsman2h[isLightArmored];
                        else if (shield != null)
                            newClass = UnitClassLoader.HeroSwordsman_[isLightArmored];
                        else newClass = UnitClassLoader.HeroSwordsman[isLightArmored];
                        break;

                    case 2: // axe
                        if (weaponKind.TwoHanded == 2)
                            newClass = UnitClassLoader.HeroAxeman2h[isLightArmored];
                        else if (shield != null)
                            newClass = UnitClassLoader.HeroAxeman_[isLightArmored];
                        else newClass = UnitClassLoader.HeroAxeman[isLightArmored];
                        break;

                    case 3: // club
                        newClass = (shield != null) ? UnitClassLoader.HeroClubman_[isLightArmored] : UnitClassLoader.HeroClubman[isLightArmored];
                        break;

                    case 4: // pike
                        newClass = (shield != null) ? UnitClassLoader.HeroPikeman_[isLightArmored] : UnitClassLoader.HeroPikeman[isLightArmored];
                        break;

                    case 5: // shooting (bow or crossbow)
                        if (weaponKind.Name.ToLower().Contains("crossbow"))
                            newClass = UnitClassLoader.HeroCrossbowman[isLightArmored];
                        else newClass = UnitClassLoader.HeroArcher[isLightArmored];
                        break;
                }
            }

            if (newClass != Class)
            {
                Class = newClass;
                DoUpdateView = true;
            }
        }

        // if not client, recalc stats
        // actually, let client do this too, we send it the required info anyway
        // max brms
        short maxBody = 100, maxReaction = 100, maxMind = 100, maxSpirit = 100;
        if ((Gender & GenderFlags.MaleFighter) == GenderFlags.MaleFighter)
        {
            maxBody = 52;
            maxReaction = 50;
            maxMind = 48;
            maxSpirit = 46;
        }
        else if ((Gender & GenderFlags.FemaleFighter) == GenderFlags.FemaleFighter)
        {
            maxBody = 50;
            maxReaction = 52;
            maxMind = 46;
            maxSpirit = 48;
        }
        else if ((Gender & GenderFlags.MaleMage) == GenderFlags.MaleMage)
        {
            maxBody = 48;
            maxReaction = 46;
            maxMind = 52;
            maxSpirit = 50;
        }
        else if ((Gender & GenderFlags.FemaleMage) == GenderFlags.FemaleMage)
        {
            maxBody = 46;
            maxReaction = 48;
            maxMind = 50;
            maxSpirit = 52;
        }

        CoreStats.Body = Math.Min(CoreStats.Body, maxBody);
        CoreStats.Reaction = Math.Min(CoreStats.Reaction, maxReaction);
        CoreStats.Mind = Math.Min(CoreStats.Mind, maxMind);
        CoreStats.Spirit = Math.Min(CoreStats.Spirit, maxSpirit);

        float origHealth = (float)Stats.Health / Stats.HealthMax;
        float origMana = (float)Stats.Mana / Stats.ManaMax;
        CoreStats.Health = Stats.Health;
        CoreStats.Mana = Stats.Mana;

        // 
        // add stats from items
        ItemStats = new UnitStats();
        foreach (Item bodyitem in ItemsBody)
            ItemStats.MergeEffects(bodyitem.Effects);

        UnitStats baseStats = new UnitStats(CoreStats);
        if ((Gender & GenderFlags.Fighter) != 0)
        {
            baseStats.SkillBlade = (byte)GetSkill(ExperienceSkill.Blade);
            baseStats.SkillAxe = (byte)GetSkill(ExperienceSkill.Axe);
            baseStats.SkillBludgeon = (byte)GetSkill(ExperienceSkill.Bludgeon);
            baseStats.SkillPike = (byte)GetSkill(ExperienceSkill.Pike);
            baseStats.SkillShooting = (byte)GetSkill(ExperienceSkill.Shooting);
        }
        else if ((Gender & GenderFlags.Mage) != 0)
        {
            baseStats.SkillFire = (byte)GetSkill(ExperienceSkill.Fire);
            baseStats.SkillWater = (byte)GetSkill(ExperienceSkill.Water);
            baseStats.SkillAir = (byte)GetSkill(ExperienceSkill.Air);
            baseStats.SkillEarth = (byte)GetSkill(ExperienceSkill.Earth);
            baseStats.SkillAstral = (byte)GetSkill(ExperienceSkill.Astral);
        }

        Stats = new UnitStats(CoreStats);
        Stats.MergeStats(ItemStats);
        Stats.DamageMax += Stats.DamageMin; // allods use damagemax this way

        // calculate core speed - based on stats
        if (Stats.Reaction < 12)
            baseStats.Speed = (byte)Stats.Reaction;
        else baseStats.Speed = (byte)Math.Min(Stats.Reaction / 5 + 12, 255);
        if (Class.ID == 19 || Class.ID == 21) // horseman
            baseStats.Speed += 10;
        baseStats.RotationSpeed = Stats.Speed;

        // CoreStats = only BRMS used
        float experience_total = 7320;
        float fighter_mult = (Gender & GenderFlags.Fighter) != 0 ? 2 : 1;
        float mage_mult = (Gender & GenderFlags.Mage) != 0 ? 2 : 1;

        baseStats.HealthMax = (int)(Stats.Body * fighter_mult);
        baseStats.HealthMax += (int)(Log11(experience_total / 5000f + fighter_mult));
        baseStats.HealthMax = (int)((Pow11(Stats.Body) / 100f + 1f) * baseStats.HealthMax);

        if ((Gender & GenderFlags.Mage) != 0)
        {
            baseStats.ManaMax = (int)(Stats.Spirit * mage_mult);
            baseStats.ManaMax += (int)(Log11(experience_total / 5000f + mage_mult));
            baseStats.ManaMax = (int)((Pow11(Stats.Spirit) / 100f + 1f) * baseStats.ManaMax);
        }
        else baseStats.ManaMax = -1;

        // merge again
        Stats = new UnitStats(baseStats);
        Stats.MergeStats(ItemStats);
        Stats.DamageMax += Stats.DamageMin; // allods use damagemax this way

        if (CoreStats.HealthMax > 0)
            Stats.Health = (int)(origHealth * Stats.HealthMax);
        if (CoreStats.ManaMax > 0)
            Stats.Mana = (int)(origMana * Stats.ManaMax);

        for (int i = 0; i < SpellEffects.Count; i++)
            SpellEffects[i].ProcessStats(Stats);

        //Debug.LogFormat("ItemStats = {0}", ItemStats.ToString());

        DoUpdateInfo = true;
        DoUpdateView = true;
    }

    public override void Update()
    {
        // update unit
        base.Update();
    }

    // template stuff.
    public override int Charge
    {
        get
        {
            Item weapon = GetItemFromBody(BodySlot.Weapon);
            if (weapon != null)
                return weapon.Class.Option.Charge;
            return Class.AttackDelay;
        }
    }

    public override int Relax
    {
        get
        {
            Item weapon = GetItemFromBody(BodySlot.Weapon);
            if (weapon != null)
                return weapon.Class.Option.Relax;
            return 0;
        }
    }

    public override bool IsIgnoringArmor { get { return false; } }

    public override bool IsFlying { get { return false; } }
    public override bool IsHovering { get { return false; } }
    public override bool IsWalking { get { return true; } }

    public override int ServerID { get { return Template.ServerID; } }
    public override int TypeID { get { return Class.ID; } }
    public override int Face { get { return Template.Face; } }

    public override string TemplateName { get { return Template.Name; } }

    // experience stuff
    public int GetExperience()
    {
        if ((Gender & GenderFlags.Mage | GenderFlags.Fighter) == 0)
            return 0;

        int exp = 0;
        for (int i = 0; i < 5; i++)
            exp += Experience[i];
        return exp;
    }

    public int ScaleExperience(float scalar)
    {
        for (int i = 0; i < 5; i++)
            Experience[i] = (int)(Experience[i] * scalar);
        return 0;
    }

    public int SetSkillExperience(ExperienceSkill sk, int value)
    {
        if ((Gender & GenderFlags.Mage | GenderFlags.Fighter) == 0)
            return 0;
        return (Experience[(int)sk] = value);
    }

    public int GetSkillExperience(ExperienceSkill sk)
    {
        if ((Gender & GenderFlags.Mage | GenderFlags.Fighter) == 0)
            return 0;
        return Experience[(int)sk];
    }

    public void SetSkill(ExperienceSkill sk, int value)
    {
        int exp = value > 0 ? (int)((Mathf.Pow(1.1f, value) - 1f) * 1000f) : 0;
        SetSkillExperience(sk, exp);
    }

    private static int[] ReverseExpTable;
    public int GetSkill(ExperienceSkill sk)
    {
        int exp = GetSkillExperience(sk);

        if (ReverseExpTable == null)
        {
            ReverseExpTable = new int[256];
            for (int i = 0; i < 256; i++)
                ReverseExpTable[i] = i > 0 ? (int)((Mathf.Pow(1.1f, i) - 1f) * 1000f) : 0;
        }

        for (int i = 0; i < ReverseExpTable.Length; i++)
        {
            if (ReverseExpTable[i] > exp)
                return i - 1;
        }

        return 0;
    }
}
 