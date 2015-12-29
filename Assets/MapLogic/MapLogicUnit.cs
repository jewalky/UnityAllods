using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class MapLogicUnit : MapLogicObject, IMapLogicPlayerPawn, IDisposable
{
    public override MapLogicObjectType GetObjectType() { return MapLogicObjectType.Monster; }
    protected override Type GetGameObjectType() { return typeof(MapViewUnit); }

    public UnitClass Class = null;
    public Templates.TplMonster Template = null; // 
    public MapLogicStats Stats { get; private set; }
    private MapLogicPlayer _Player;

    public MapLogicPlayer Player
    {
        get
        {
            return _Player;
        }

        set
        {
            if (_Player != null)
                _Player.Objects.Remove(this);
            _Player = value;
            _Player.Objects.Add(this);
        }
    }

    public MapLogicPlayer GetPlayer() { return _Player; }
    public int Tag = 0;

    public MapLogicUnit(int serverId)
    {
        Template = TemplateLoader.GetMonsterById(serverId);
        if (Template == null)
            Debug.Log(string.Format("Invalid unit created (serverId={0})", serverId));
        else InitUnit();
    }

    public MapLogicUnit(string name)
    {
        Template = TemplateLoader.GetMonsterByName(name);
        if (Template == null)
            Debug.Log(string.Format("Invalid unit created (name={0})", name));
        else InitUnit();
    }

    private void InitUnit()
    {
        Class = UnitClassLoader.GetUnitClassById(Template.TypeID);
        if (Class == null)
        {
            Debug.Log(string.Format("Invalid unit created (class not found, serverId={0}, typeId={1})", Template.ServerID, Template.TypeID));
            Template = null;
            return;
        }

        Width = Template.TokenSize;
        Height = Template.TokenSize;

        Stats = new MapLogicStats();
        DoUpdateView = true;
    }

    public override void Dispose()
    {
        base.Dispose();
        if (_Player != null)
            _Player.Objects.Remove(this);
    }

    public override void Update()
    {
        
    }

    public override MapNodeFlags GetNodeLinkFlags(int x, int y)
    {
        if (Template == null)
            return 0;
        if (Template.MovementType == 1 || Template.MovementType == 2)
            return MapNodeFlags.BlockedGround;
        if (Template.MovementType == 3)
            return MapNodeFlags.BlockedAir;
        return 0;
    }
}