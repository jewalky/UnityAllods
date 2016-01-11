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

    public enum HGender
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

    public HGender Gender { get; private set; }


    public MapHuman(int serverId)
    {
        Template = TemplateLoader.GetHumanById(serverId);
        if (Template == null)
            Debug.LogFormat("Invalid human created (serverId={0})", serverId);
        else InitHuman();
    }

    public MapHuman(string name)
    {
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

        // initial items
        for (int i = 0; i < Template.EquipItems.Length; i++)
        {
            if (Template.EquipItems[i].Length <= 0)
                continue;

            Item item = new Item(Template.EquipItems[i]);
            if (!item.IsValid)
                continue;

            PutItemToBody((BodySlot)item.Class.Option.Slot, item);
        }

        // human specific
        if (Template.Gender == 1)
            Gender = HGender.Female;
        else Gender = HGender.Male;
        // guess class (mage/fighter) from type
        if (Class.ID == 24 || Class.ID == 23) // unarmed mage, mage with staff
            Gender |= HGender.Mage;
        else Gender |= HGender.Fighter; // otherwise its a fighter.

        CalculateVision();
    }

    public override void Update()
    {
        // update human specific stuff

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