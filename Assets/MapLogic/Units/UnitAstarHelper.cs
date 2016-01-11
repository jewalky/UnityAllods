using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

class UnitAstarHelper : IShortestPath<Vector2i, Vector2i>
{
    private MapUnit unit;
    public bool StaticLookup = false;

    public UnitAstarHelper(MapUnit unit)
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
        return unit.Interaction.CheckWalkableForUnit(p.x, p.y, StaticLookup);
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