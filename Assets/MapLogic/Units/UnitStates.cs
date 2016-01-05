using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class IdleState : IUnitState
{
    private MapUnit Unit;

    public IdleState(MapUnit unit)
    {
        Unit = unit;
    }

    public bool Process()
    {
        if (Unit.Stats.Health <= 0)
            return true;

        bool doFullAI = (Unit.Player.Flags & PlayerFlags.AI) != 0 &&
                        (Unit.Player.Flags & PlayerFlags.Dormant) == 0;

        if (doFullAI)
        {
            // rotate randomly
            if ((UnityEngine.Random.Range(0, 256) < 1) &&
                Unit.Actions.Count == 1) // unit is idle and 1/256 chance returns true
            {
                int angle = UnityEngine.Random.Range(0, 36) * 10;
                Unit.AddActions(new RotateAction(Unit, angle));
            }
        }

        return true;
    }
}

public class MoveState : IUnitState
{
    private MapUnit Unit;
    private int WalkX;
    private int WalkY;

    public MoveState(MapUnit unit, int x, int y)
    {
        Unit = unit;
        WalkX = x;
        WalkY = y;
    }

    // made static because it's also used by other actions
    public static bool TryWalkTo(MapUnit Unit, int WalkX, int WalkY)
    {
        if (WalkX == Unit.X && WalkY == Unit.Y)
            return true;

        // try to pathfind
        List<Vector2i> path = Unit.DecideNextMove(WalkX, WalkY, true);
        if (path == null)
            return false;

        int sbd = 32;
        if (sbd > path.Count) sbd = path.Count;
        for (int i = 0; i < sbd; i++)
        {
            if (!Unit.CheckWalkableForUnit(path[i].x, path[i].y, false))
            {
                // one of nodes in statically found path (up to 32 nodes ahead) is non-walkable.
                // here we try to build another path around it instead.
                // if it's not found, we continue to walk along the old path.
                List<Vector2i> path2 = null;
                int pnum = path.Count - 1;
                while (path2 == null && pnum >= 0)
                {
                    path2 = Unit.DecideNextMove(path[pnum].x, path[pnum].y, false);
                    pnum--;
                }

                if (path2 != null)
                    path = path2;

                break;
            }
        }

        // if NEXT node is not walkable, we drop into idle state.
        if (Unit.CheckWalkableForUnit(path[0].x, path[0].y, false))
        {
            // next path node found
            // notify clients
            Unit.AddActions(new MoveAction(Unit, path[0].x, path[0].y), new RotateAction(Unit, Unit.FaceCell(path[0].x, path[0].y)));
            return true;
        }

        return false;
    }

    public bool Process()
    {
        if (Unit.Stats.Health <= 0)
            return false;

        if (Unit.X == WalkX && Unit.Y == WalkY)
            return false;

        TryWalkTo(Unit, WalkX, WalkY);
        return true;
    }
}

public class AttackState : IUnitState
{
    private MapUnit Unit;
    private MapUnit TargetUnit;

    public AttackState(MapUnit unit, MapUnit targetUnit)
    {
        Unit = unit;
        TargetUnit = targetUnit;
    }

    public bool Process()
    {
        if (Unit.Stats.Health <= 0)
            return false;

        if (TargetUnit == Unit || !TargetUnit.IsAlive || !MapLogic.Instance.Objects.Contains(TargetUnit))
            return false;

        // assume melee attack right now
        // check if in direct proximity
        if (Unit.GetClosestDistanceTo(TargetUnit) <= 1.5)
        {
            // in direct proximity!
            // 
            Vector2i enemyCell = Unit.GetClosestPointTo(TargetUnit);
            int angleNeeded = Unit.FaceCell(enemyCell.x, enemyCell.y);
            if (Unit.Angle != angleNeeded)
            {
                Unit.AddActions(new RotateAction(Unit, angleNeeded));
                return true;
            }

            //
            //Debug.LogFormat("ATTACKING");
            int damage = UnityEngine.Random.Range(Unit.Stats.DamageMin, Unit.Stats.DamageMax);
            Unit.AddActions(new AttackAction(Unit, TargetUnit, DamageFlags.Raw, damage));
        }
        else
        {
            // make one step to the target.
            MoveState.TryWalkTo(Unit, TargetUnit.X, TargetUnit.Y);
        }

        return true;
    }
}