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

        Stats.Health = Stats.HealthMax = Math.Max(Template.HealthMax, 0);
        Stats.Mana = Stats.ManaMax = Math.Max(Template.ManaMax, 0); // they sometimes put -1 as mana counter for fighters

        // BRMS
        Stats.Body = (short)Template.Body;
        Stats.Reaction = (short)Template.Reaction;
        Stats.Mind = (short)Template.Mind;
        Stats.Spirit = (short)Template.Spirit;

        // speed and scanrange
        Stats.RotationSpeed = (byte)Template.RotationSpeed;
        if (Stats.RotationSpeed < 1)
            Stats.RotationSpeed = 1;
        Stats.Speed = (byte)Template.Speed;
        if (Stats.Speed < 1)
            Stats.Speed = 1;
        Stats.ScanRange = Template.ScanRange;

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
        ItemsPack.PutItem(ItemsPack.Count, new Item("Very Rare Crystal Ring {body=4}"));
        ItemsPack.PutItem(ItemsPack.Count, new Item("Very Rare Crystal Amulet {body=4}"));
        ItemsPack.PutItem(ItemsPack.Count, new Item("Potion Body"));
        ItemsPack.PutItem(ItemsPack.Count, new Item("Very Rare Meteoric Plate Cuirass {body=4}"));
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