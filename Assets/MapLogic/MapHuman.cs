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

        CalculateVision();
        UpdateItems();

        // add item for testing
        if (!NetworkManager.IsClient)
        {
            ItemsPack.PutItem(ItemsPack.Count, new Item("Very Rare Crystal Ring {body=4}"));
            ItemsPack.PutItem(ItemsPack.Count, new Item("Very Rare Crystal Amulet {body=4}"));
            ItemsPack.PutItem(ItemsPack.Count, new Item("Potion Body"));
            ItemsPack.PutItem(ItemsPack.Count, new Item("Very Rare Meteoric Plate Cuirass {body=4}"));
            ItemsPack.PutItem(ItemsPack.Count, new Item("Very Rare Meteoric Plate Cuirass {body=4}"));
            /*                    MapProjectile proj = new MapProjectile(15);
                    proj.SetPosition(16, 16, 0);
                    proj.Target = ConsolePlayer.Avatar;
                    Objects.Add(proj);
*/          ItemsPack.PutItem(ItemsPack.Count, new Item("Very Rare Meteoric Crossbow"));
        }
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
        if (!NetworkManager.IsClient)
        {
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

            // 
            // add stats from items
            ItemStats = new UnitStats();
            foreach (Item bodyitem in ItemsBody)
                ItemStats.MergeEffects(bodyitem.Effects);

            //Debug.LogFormat("ItemStats = {0}", ItemStats.ToString());

            Stats = CoreStats; // add all item stats
            Stats.MergeStats(ItemStats);
            // CoreStats = only BRMS used
            //Debug.LogFormat("Body = {0}, {1}, {2}", Stats.Body, CoreStats.Body, ItemStats.Body);

            /*
                *(_WORD *)(this + 150) = (signed __int64)(sub_53066C((double)*(signed int *)(this + 304) / 5000.0 + 1.0)
                                            * (double)((v34 != 0) + 1)
                                            + (double)*(_WORD *)(this + 150));
                *(_WORD *)(v28 + 150) = (signed __int64)((sub_53064C(*(_WORD *)(v28 + 132)) / 100.0 + 1.0)
                                                        * (double)*(_WORD *)(v28 + 150));

            sub_53066C(a1):
            log(a1) / log(1.1)

            sub_53064C(a1):
            pow(1.1, a1)

            health = body * (is_fighter ? 2 : 1)
            health += sub_53066C(experience_total) / 5000.0 + 1 * (is_fighter ? 2 : 1)
            health *= sub_53064C(body) / 100.0 + 1.0
            */

            float experience_total = 7320;
            float fighter_mult = (Gender & GenderFlags.Fighter) != 0 ? 2 : 1;
            float mage_mult = (Gender & GenderFlags.Mage) != 0 ? 2 : 1;

            Stats.HealthMax = (int)(Stats.Body * fighter_mult);
            Stats.HealthMax += (int)(Log11(experience_total / 5000f + fighter_mult));
            Stats.HealthMax = (int)((Pow11(Stats.Body) / 100f + 1f) * Stats.HealthMax);

            if ((Gender & GenderFlags.Mage) != 0)
            {
                Stats.ManaMax = (int)(Stats.Spirit * mage_mult);
                Stats.ManaMax += (int)(Log11(experience_total / 5000f + mage_mult));
                Stats.ManaMax = (int)((Pow11(Stats.Spirit) / 100f + 1f) * Stats.ManaMax);
            }
            else Stats.ManaMax = -1;

            //
            /*
              if ( *(v28 + 134) >= 12 )
                *(v28 + 140) = *(v28 + 134) / 5 + 12;       // speed
              else
                *(v28 + 140) = *(v28 + 134);
              if ( *(v28 + 14) == 19 || *(v28 + 14) == 21 ) // ManHorse_LanceShield, ManHorse_SwordShield
                *(v28 + 140) += 10;
             */

            if (Stats.Reaction < 12)
                Stats.Speed = (byte)Stats.Reaction;
            else Stats.Speed = (byte)Math.Min(Stats.Reaction / 5 + 12, 255);
            if (Class.ID == 19 || Class.ID == 21) // horseman
                Stats.Speed += 10;
            Stats.RotationSpeed = Stats.Speed;
        }

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
}
 