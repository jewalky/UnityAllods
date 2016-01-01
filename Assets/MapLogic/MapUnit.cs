using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public interface IUnitAction
{
    bool Process();
}

public enum UnitVisualState
{
    Idle,
    Rotating,
    Moving
}

public class MapUnit : MapObject, IPlayerPawn, IVulnerable, IDisposable
{
    public override MapObjectType GetObjectType() { return MapObjectType.Monster; }
    protected override Type GetGameObjectType() { return typeof(MapViewUnit); }

    public UnitClass Class = null;
    public Templates.TplMonster Template = null; // 
    public UnitStats Stats;
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
    public int Tag = 0;
    private int _Angle = 0;
    public int Angle
    {
        get
        {
            return _Angle;
        }

        set
        {
            _Angle = value;
            while (_Angle < 0)
                _Angle += 360;
            while (_Angle >= 360)
                _Angle -= 360;
        }
    }

    public List<IUnitAction> Actions = new List<IUnitAction>();
    public UnitVisualState VState = UnitVisualState.Idle;
    public bool AllowIdle = false;
    public int IdleFrame = 0;
    public int IdleTime = 0;
    public int MoveFrame = 0;
    public int MoveTime = 0;
    // for visual state stuff
    public float FracX = 0;
    public float FracY = 0;

    // for AI
    public int WalkX = -1;
    public int WalkY = -1;

    //
    public readonly bool[,] Vision = new bool[41, 41];
    private ScanrangeCalc VisionCalc = new ScanrangeCalc();

    public MapUnit(int serverId)
    {
        Template = TemplateLoader.GetMonsterById(serverId);
        if (Template == null)
            Debug.LogFormat("Invalid unit created (serverId={0})", serverId);
        else InitUnit();
    }

    public MapUnit(string name)
    {
        Template = TemplateLoader.GetMonsterByName(name);
        if (Template == null)
            Debug.LogFormat("Invalid unit created (name={0})", name);
        else InitUnit();
    }

    private void InitUnit()
    {
        Class = UnitClassLoader.GetUnitClassById(Template.TypeID);
        if (Class == null)
        {
            Debug.LogFormat("Invalid unit created (class not found, serverId={0}, typeId={1})", Template.ServerID, Template.TypeID);
            Template = null;
            return;
        }

        Stats = new UnitStats();
        Width = Template.TokenSize;
        Height = Template.TokenSize;

        Stats.Health = Stats.HealthMax = Math.Max(Template.HealthMax, 0);
        Stats.Mana = Stats.ManaMax = Math.Max(Template.ManaMax, 0); // they sometimes put -1 as mana counter for fighters

        // BRMS
        Stats.Body = (short)Template.Body;
        Stats.Reaction = (short)Template.Reaction;
        Stats.Mind = (short)Template.Mind;
        Stats.Spirit = (short)Template.Spirit;

        // physical damage and resists
        Stats.DamageMin = (short)Template.PhysicalMin;
        Stats.DamageMax = (short)Template.PhysicalMax;
        Stats.ToHit = (short)Template.ToHit;
        Stats.Absorbtion = (short)Template.Absorbtion;
        Stats.Defence = (short)Template.Defense;

        // magical resists
        Stats.ProtectionFire = (byte)Template.ProtectionFire;
        Stats.ProtectionWater = (byte)Template.ProtectionWater;
        Stats.ProtectionAir = (byte)Template.ProtectionAir;
        Stats.ProtectionEarth = (byte)Template.ProtectionEarth;
        Stats.ProtectionAstral = (byte)Template.ProtectionAstral;

        // physical resists (custom)
        Stats.ProtectionBlade = (byte)Template.ProtectionBlade;
        Stats.ProtectionAxe = (byte)Template.ProtectionAxe;
        Stats.ProtectionBludgeon = (byte)Template.ProtectionBludgeon;
        Stats.ProtectionPike = (byte)Template.ProtectionPike;
        Stats.ProtectionShooting = (byte)Template.ProtectionShooting;

        Stats.RotationSpeed = (byte)Template.RotationSpeed;
        if (Stats.RotationSpeed < 1)
            Stats.RotationSpeed = 1;
        Stats.Speed = (byte)Template.Speed;
        if (Stats.Speed < 1)
            Stats.Speed = 1;
        Stats.ScanRange = Template.ScanRange;

        Actions.Add(new IdleAction(this));
        DoUpdateView = true;
        VState = UnitVisualState.Idle;
        CalculateVision();
    }

    public override void Dispose()
    {
        base.Dispose();
        if (_Player != null)
            _Player.Objects.Remove(this);
    }

    public override void Update()
    {
        if (Class == null)
            return;

        UpdateNetVisibility();

        while (!Actions.Last().Process())
            Actions.RemoveAt(Actions.Count - 1);
    }

    public override MapNodeFlags GetNodeLinkFlags(int x, int y)
    {
        if (Template == null)
            return 0;
        return (Template.IsFlying) ? MapNodeFlags.DynamicAir : MapNodeFlags.DynamicGround;
    }

    /// <summary>
    /// EVERYTHING BELOW IS ASTAR
    /// </summary>

    class AstarHelper : IShortestPath<Vector2i, Vector2i>
    {
        private MapUnit unit;
        public bool StaticLookup = false;

        public AstarHelper(MapUnit unit)
        {
            this.unit = unit;
        }

        /**
         * Should return a estimate of shortest distance. The estimate must me admissible (never overestimate)
         */
        public float Heuristic(Vector2i fromLocation, Vector2i toLocation)
        {
            return (fromLocation - toLocation).magnitude; // return straight line distance
        }

        private bool CheckWalkable(Vector2i p)
        {
            if (p.x < 8 || p.y < 8 ||
                p.x >= MapLogic.Instance.Width - 8 || p.y >= MapLogic.Instance.Height - 8) return false;
            return unit.CheckWalkableForUnit(p.x, p.y, StaticLookup);
        }

        /**
         * Return the legal moves from position
         */
        public List<Vector2i> Expand(Vector2i state)
        {
            List<Vector2i> res = new List<Vector2i>();
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0) continue;
                    Vector2i action = new Vector2i(x, y);
                    action.x += state.x;
                    action.y += state.y;
                    if (CheckWalkable(action))
                        res.Add(action);
                }
            }
            return res;
        }

        /**
         * Return the actual cost between two adjecent locations
         */
        public float ActualCost(Vector2i fromLocation, Vector2i toLocation)
        {
            return (fromLocation - toLocation).magnitude;
        }

        public Vector2i ApplyAction(Vector2i state, Vector2i action)
        {
            return action;
        }
    }

    private ShortestPathGraphSearch<Vector2i, Vector2i> AstarSearcher = null;
    private AstarHelper AstarSearcherH = null;
    public List<Vector2i> DecideNextMove(int targetX, int targetY, bool staticOnly)
    {
        // if targetX,targetY is blocked, refuse to pathfind.
        if (!CheckWalkableForUnit(targetX, targetY, staticOnly))
            return null;

        if (AstarSearcherH == null)
            AstarSearcherH = new AstarHelper(this);
        if (AstarSearcher == null)
            AstarSearcher = new ShortestPathGraphSearch<Vector2i, Vector2i>(AstarSearcherH);
        AstarSearcherH.StaticLookup = staticOnly;

        try
        {
            List<Vector2i> nodes = AstarSearcher.GetShortestPath(new Vector2i(X, Y), new Vector2i(targetX, targetY));
            if (nodes == null)
                return null;
            nodes.Add(new Vector2i(targetX, targetY));
            return nodes;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// EVERYTHING BELOW IS STATES
    /// </summary>

    // returns true if cell is walkable for this unit
    public bool CheckWalkableForUnit(int x, int y, bool staticOnly)
    {
        for (int ly = y; ly < y + Height; ly++)
        {
            for (int lx = x; lx < x + Width; lx++)
            {
                // skip cells currently taken
                if (MapLogic.Instance.Nodes[lx, ly].Objects.Contains(this))
                    continue; // if we are already on this cell, skip it as passible
                uint tile = MapLogic.Instance.Nodes[lx, ly].Tile;
                MapNodeFlags flags = MapLogic.Instance.Nodes[lx, ly].Flags;
                if (Template.IsWalking && (flags & MapNodeFlags.Unblocked) == 0 && (tile >= 0x1C0 && tile <= 0x2FF))
                    return false;
                MapNodeFlags bAir = staticOnly ? MapNodeFlags.BlockedAir : MapNodeFlags.BlockedAir | MapNodeFlags.DynamicAir;
                MapNodeFlags bGround = staticOnly ? MapNodeFlags.BlockedGround : MapNodeFlags.BlockedGround | MapNodeFlags.DynamicGround;
                if (Template.IsFlying && (flags & bAir) != 0) return false;
                else if (!Template.IsFlying && (flags & bGround) != 0)
                    return false;
            }
        }

        return true;
    }

    public int FaceCell(int x, int y)
    {
        // from current x/y
        float deltaY = y - Y;
        float deltaX = x - X;
        int sang = (int)(Math.Atan2(deltaY, deltaX) * 180 / Math.PI) - 90;
        while (sang > 360)
            sang -= 360;
        while (sang < 0)
            sang += 360;
        return sang;
    }

    public void SetPosition(int x, int y)
    {
        UnlinkFromWorld();
        X = x;
        Y = y;
        LinkToWorld();
        CalculateVision();
        DoUpdateView = true;
    }

    public void CalculateVision()
    {
        // we have vision from multiple points (based on size)
        for (int ly = 0; ly < 41; ly++)
            for (int lx = 0; lx < 41; lx++)
                Vision[lx, ly] = false;
        for (int ly = 0; ly < Height; ly++)
        {
            for (int lx = 0; lx < Width; lx++)
            {
                VisionCalc.CalculateVision(X+lx, Y+ly, Stats.ScanRange);
                for (int lly = 0; lly < 41; lly++)
                {
                    for (int llx = 0; llx < 41; llx++)
                    {
                        if (VisionCalc.pTablesVision[llx, lly] > 0 &&
                            lx + llx < 41 && ly + lly < 41)
                                Vision[lx + llx, ly + lly] = true;
                    }
                }
            }
        }
    }
    public void AddActions(params IUnitAction[] states)
    {
        for (int i = 0; i < states.Length; i++)
            Actions.Add(states[i]);
        if (NetworkManager.IsServer)
            Server.NotifyAddUnitActions(this, states);
    }

    public int TakeDamage(DamageFlags flags, MapUnit source, int damagecount)
    {
        if (damagecount < 0)
            return 0;

        bool sourceIgnoresArmor = source != null && source.Template.IsIgnoringArmor;
        if ((flags & DamageFlags.PhysicalDamage) != 0 && !sourceIgnoresArmor)
        {
            int ownChance = Stats.Defence;
            int hisChance = (source != null) ? 5+source.Stats.ToHit : ownChance;

            if (ownChance > hisChance)
            {
                if (UnityEngine.Random.Range(0, ownChance) > hisChance)
                    return 0;
            }

            damagecount -= Stats.Absorbtion;
            if (damagecount <= 0)
                return 0;
        }

        // magic resists
        if ((flags & DamageFlags.Fire) != 0)
            damagecount -= damagecount * Stats.ProtectionFire / 100;
        if ((flags & DamageFlags.Water) != 0)
            damagecount -= damagecount * Stats.ProtectionWater / 100;
        if ((flags & DamageFlags.Air) != 0)
            damagecount -= damagecount * Stats.ProtectionAir / 100;
        if ((flags & DamageFlags.Earth) != 0)
            damagecount -= damagecount * Stats.ProtectionEarth / 100;
        if ((flags & DamageFlags.Astral) != 0)
            damagecount -= damagecount * Stats.ProtectionAstral / 100;

        // physical resists in monsters
        if ((flags & DamageFlags.Blade) != 0)
            damagecount -= damagecount * Stats.ProtectionBlade / 100;
        if ((flags & DamageFlags.Axe) != 0)
            damagecount -= damagecount * Stats.ProtectionAxe / 100;
        if ((flags & DamageFlags.Bludgeon) != 0)
            damagecount -= damagecount * Stats.ProtectionBludgeon / 100;
        if ((flags & DamageFlags.Pike) != 0)
            damagecount -= damagecount * Stats.ProtectionPike / 100;
        if ((flags & DamageFlags.Shooting) != 0)
            damagecount -= damagecount * Stats.ProtectionShooting / 100;

        if (Stats.TrySetHealth(Stats.Health - damagecount))
            return damagecount;

        return 0;
    }
}