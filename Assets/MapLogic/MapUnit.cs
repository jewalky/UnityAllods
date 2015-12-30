using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public interface IUnitState
{
    bool Process();
}

public enum UnitVisualState
{
    Idle,
    Rotating,
    Moving
}

public class MapUnit : MapObject, IPlayerPawn, IDisposable
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

    public List<IUnitState> States = new List<IUnitState>();
    public UnitVisualState VState = UnitVisualState.Idle;
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
        Stats.RotationSpeed = (byte)Template.RotationSpeed;
        if (Stats.RotationSpeed < 1)
            Stats.RotationSpeed = 1;
        Stats.Speed = (byte)Template.Speed;
        if (Stats.Speed < 1)
            Stats.Speed = 1;
        Stats.ScanRange = Template.ScanRange;

        States.Add(new IdleState(this));
        DoUpdateView = true;
        VState = UnitVisualState.Idle;
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

        while (!States.Last().Process())
            States.RemoveAt(States.Count - 1);
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

    /// <summary>
    /// EVERYTHING BELOW IS ASTAR
    /// </summary>

    class AstarHelper : IShortestPath<Vector2i, Vector2i>
    {
        private MapUnit unit;

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
            return unit.CheckWalkableForUnit(p.x, p.y);
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
    public Vector2i DecideNextMove(int targetX, int targetY)
    {
        // if targetX,targetY is blocked, refuse to pathfind.
        if (!CheckWalkableForUnit(targetX, targetY))
            return null;

        if (AstarSearcher == null)
            AstarSearcher = new ShortestPathGraphSearch<Vector2i, Vector2i>(new AstarHelper(this));
        try
        {
            List<Vector2i> nodes = AstarSearcher.GetShortestPath(new Vector2i(X, Y), new Vector2i(targetX, targetY));
            if (nodes == null)
                return null;
            if (nodes.Count == 0)
                return new Vector2i(targetX, targetY);
            return new Vector2i(nodes[0].x, nodes[0].y);
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
    public bool CheckWalkableForUnit(int x, int y)
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
                if (Template.MovementType == 1 && (flags & MapNodeFlags.Unblocked) == 0 && (tile >= 0x1C0 && tile <= 0x2FF))
                    return false;
                if (Template.MovementType == 3 &&
                    (flags & MapNodeFlags.BlockedAir) != 0) return false;
                else if (Template.MovementType != 3 && (flags & MapNodeFlags.BlockedGround) != 0)
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
    }

    public void AddState(IUnitState state)
    {
        States.Add(state);
        if (NetworkManager.IsServer)
            Server.NotifyAddUnitState(this, state);
    }
}