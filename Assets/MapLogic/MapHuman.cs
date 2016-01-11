using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class MapHuman : MapUnit
{
    public override MapObjectType GetObjectType() { return MapObjectType.Human; }

    // this unit type has its own template
    private Templates.TplHuman Template;

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

        CalculateVision();
    }

    // template stuff.
    public override int Charge { get { return Template.Charge; } }
    public override int Relax { get { return Template.Relax; } }

    public override bool IsIgnoringArmor { get { return false; } }

    public override bool IsFlying { get { return false; } }
    public override bool IsHovering { get { return false; } }
    public override bool IsWalking { get { return true; } }

    public override int ServerID { get { return Template.ServerID; } }
    public override int TypeID { get { return Class.ID; } }
    public override int Face { get { return Template.Face; } }

    public override string TemplateName { get { return Template.Name; } }
}