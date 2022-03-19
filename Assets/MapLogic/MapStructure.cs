using System;
using System.Collections.Generic;
using UnityEngine;

public class StructureLogic
{
    protected MapStructure Structure;
    public List<MapUnit> Units { get; private set; }

    protected StructureLogic(MapStructure s)
    {
        Units = new List<MapUnit>();
        Structure = s;
    }

    public virtual bool OnEnter(MapUnit unit)
    {
        if (unit.CurrentStructure != null)
            return false;
        Units.Add(unit);
        unit.CurrentStructure = Structure;
        unit.PhaseOut();
        return true;
    }

    public virtual void OnLeave(MapUnit unit)
    {
        if (unit.CurrentStructure != Structure)
            Debug.LogFormat("Warning: tried to leave structure while current structure is not attached to the unit");
        else unit.CurrentStructure = null;
        Units.Remove(unit);
        unit.PhaseIn();
    }

    public virtual void Update()
    {
        // override to implement inn/shop update logic
    }
}

public class MapStructure : MapObject, IDynlight, IPlayerPawn, IVulnerable, IDisposable
{
    public override MapObjectType GetObjectType() { return MapObjectType.Structure; }
    protected override Type GetGameObjectType() { return typeof(MapViewStructure); }

    public StructureClass Class = null;
    public Templates.TplStructure Template = null;
    public int CurrentFrame = 0;
    public int CurrentTime = 0;
    public int HealthMax = 0;
    private int _Health = 0;
    public int Health
    {
        get
        {
            return _Health;
        }

        set
        {
            if (value > HealthMax)
                value = HealthMax;
            if (value < 0)
                value = 0;
            _Health = value;
        }
    }
    private Player _Player;

    public Player Player
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

    public Player GetPlayer() { return _Player; }
    public bool IsBridge = false;
    public int Tag = 0;
    public float ScanRange = 0;

    // this is used for dynlight
    private int LightTime = 0;
    private int LightFrame = 0;
    private int LightValue = 0; // basically, this gets set if structure is a dynlight
    public int GetLightValue() { return LightValue; }

    // entering structures
    public StructureLogic Logic = null;

    public MapStructure(int typeId)
    {
        Class = StructureClassLoader.GetStructureClassById(typeId);
        if (Class == null)
            Debug.LogFormat("Invalid structure created (typeId={0})", typeId);
        else InitStructure();
    }

    public MapStructure(string name)
    {
        Class = StructureClassLoader.GetStructureClassByName(name);
        if (Class == null)
            Debug.LogFormat("Invalid structure created (name={0})", name);
        else InitStructure();
    }

    public override void Dispose()
    {
        if (_Player != null)
            _Player.Objects.Remove(this);
        base.Dispose();
    }

    private void InitStructure()
    {
        Template = TemplateLoader.GetStructureById(Class.ID);
        if (Template == null)
        {
            Debug.LogFormat("Invalid structure created (template not found, typeId={0})", Class.ID);
            Class = null;
            return;
        }

        HealthMax = Health = Template.HealthMax;
        Width = Template.Width;
        Height = Template.Height;
        ScanRange = Template.ScanRange; // only default scanrange
        RenderViewVersion++;
    }

    public override void CheckAllocateObject()
    {
        if (GetVisibility() >= 1)
            AllocateObject();
    }

    public override void Update()
    {
        if (Class == null)
            return;

        UpdateNetVisibility();

        if (Logic != null)
            Logic.Update();

        // perform animation
        // do not animate if visibility != 2, also do not render at all if visibility == 0
        if (Class.Frames.Length > 1 && GetVisibility() == 2)
        {
            CurrentTime++;
            if (CurrentTime > Class.Frames[CurrentFrame].Time)
            {
                CurrentFrame = ++CurrentFrame % Class.Frames.Length;
                CurrentTime = 0;
                RenderViewVersion++;
            }

            if (Class.LightRadius > 0)
            {
                LightTime++;
                if (LightTime > Class.LightPulse)
                {
                    LightFrame++;
                    LightValue = (int)(Mathf.Sin((float)LightFrame / 3) * 64 + 128);
                    MapLogic.Instance.MarkDynLightingForUpdate();
                }
            }
        }
    }

    public override MapNodeFlags GetNodeLinkFlags(int x, int y)
    {
        if (IsBridge) return MapNodeFlags.Unblocked;

        bool canNotPass = ((1 << (y * Width + x)) & Template.CanNotPass) != 0;
        bool canPass = ((1 << (y * Width + x)) & Template.CanPass) != 0;
        if (!canPass) return MapNodeFlags.Unblocked;
        if (canNotPass) return MapNodeFlags.BlockedGround;
        return 0;
    }

    public int TakeDamage(DamageFlags flags, MapUnit source, int count)
    {
        if ((flags & DamageFlags.TerrainDamage) == 0)
            return 0;

        Health -= count;
        RenderInfoVersion++;
        RenderViewVersion++;

        return count;
    }

    public bool HandleUnitEnter(MapUnit unit)
    {
        if (Logic != null)
            return Logic.OnEnter(unit);
        return false;
    }

    public void HandleUnitLeave(MapUnit unit)
    {
        if (Logic != null)
            Logic.OnLeave(unit);
    }
}