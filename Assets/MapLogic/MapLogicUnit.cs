using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public interface IMapLogicUnitState
{
    bool Process();
}

public class MapLogicUnit : MapLogicObject, IMapLogicPlayerPawn, IDisposable
{
    public class IdleState : IMapLogicUnitState
    {
        private MapLogicUnit Unit;

        public IdleState(MapLogicUnit unit)
        {
            Unit = unit;
        }

        public virtual bool Process()
        {
            if (Unit.WalkX > 0 && Unit.WalkY > 0)
            {
                Debug.Log(string.Format("idle state: walk to {0},{1}", Unit.WalkX, Unit.WalkY));
                if (Unit.WalkX == Unit.X && Unit.WalkY == Unit.Y)
                {
                    Unit.WalkX = -1;
                    Unit.WalkY = -1;
                    return true;
                }

                // try to pathfind
                Vector2i path = Unit.DecideNextMove(Unit.WalkX, Unit.WalkY);
                if (path == null)
                {
                    Debug.Log(string.Format("idle state: path to {0},{1} not found", Unit.WalkX, Unit.WalkY));
                    Unit.WalkX = -1;
                    Unit.WalkY = -1;
                    return true;
                }

                // next path node found
                Unit.States.Add(new WalkState(Unit, path.x, path.y));
            }
            return true; // idle state is always present
        }
    }

    public class RotateState : IMapLogicUnitState
    {
        private MapLogicUnit Unit;
        private int TargetAngle;

        public RotateState(MapLogicUnit unit, int targetAngle)
        {
            Unit = unit;
            TargetAngle = targetAngle;
        }

        public virtual bool Process()
        {
            // 
            Unit.Angle = TargetAngle;
            return false;
        }
    }

    public class WalkState : IMapLogicUnitState
    {
        private MapLogicUnit Unit;
        private int TargetX;
        private int TargetY;

        public WalkState(MapLogicUnit unit, int x, int y)
        {
            Unit = unit;
            TargetX = x;
            TargetY = y;
        }

        public virtual bool Process()
        {
            Debug.Log(string.Format("walk state: moved to {0},{1}", TargetX, TargetY));
            Unit.UnlinkFromWorld();
            Unit.X = TargetX;
            Unit.Y = TargetY;
            Unit.LinkToWorld();
            Unit.DoUpdateView = true;
            return false;
        }
    }

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
    public int Angle = 0;
    private List<IMapLogicUnitState> States = new List<IMapLogicUnitState>();

    public int WalkX = -1;
    public int WalkY = -1;

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
        States.Add(new IdleState(this));
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
        private MapLogicUnit unit;

        public AstarHelper(MapLogicUnit unit)
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

    // returns true if cell is walkable for this unit
    public bool CheckWalkableForUnit(int x, int y)
    {
        for (int ly = y; ly < y + Height; ly++)
        {
            for (int lx = x; lx < x + Width; lx++)
            {
                // skip cells currently taken
                if (lx >= X && lx < X + Width &&
                    ly >= Y && ly < Y + Height) continue;
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
}