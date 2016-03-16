using System.Collections.Generic;

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

        if (!doFullAI) return true;
        // rotate randomly
        if ((UnityEngine.Random.Range(0, 256) < 1) &&
            Unit.Actions.Count == 1) // unit is idle and 1/256 chance returns true
        {
            int angle = UnityEngine.Random.Range(0, 36) * 10;
            Unit.AddActions(new RotateAction(Unit, angle));
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
    public static bool TryWalkTo(MapUnit unit, int walkX, int walkY)
    {
        // check if target is walkable for us (statically)
        if (!unit.Interaction.CheckWalkableForUnit(walkX, walkY, false))
        {
            List<Vector2i> switchNodes = new List<Vector2i>();
            for (int ly = walkY - unit.Height; ly < walkY + unit.Height; ly++)
                for (int lx = walkX - unit.Width; lx < walkX + unit.Width; lx++)
                    if (unit.Interaction.CheckWalkableForUnit(lx, ly, false))
                        switchNodes.Add(new Vector2i(lx, ly));

            switchNodes.Sort((a, b) => 
            {
                Vector2i own1 = unit.Interaction.GetClosestPointTo(a.x, a.y);
                Vector2i own2 = unit.Interaction.GetClosestPointTo(b.x, b.y);
                float d1 = (a - own1).magnitude;
                float d2 = (b - own2).magnitude;
                if (d1 > d2)
                    return 1;
                else if (d1 < d2)
                    return -1;
                return 0;
            });

            if (switchNodes.Count <= 0)
                return false;

            walkX = switchNodes[0].x;
            walkY = switchNodes[0].y;
        }

        if (walkX == unit.X && walkY == unit.Y)
            return true;

        // try to pathfind
        List<Vector2i> path = unit.DecideNextMove(walkX, walkY, true);
        if (path == null)
            return false;

        int sbd = 32;
        if (sbd > path.Count) sbd = path.Count;
        for (int i = 0; i < sbd; i++)
        {
            if (!unit.Interaction.CheckWalkableForUnit(path[i].x, path[i].y, false))
            {
                // one of nodes in statically found path (up to 32 nodes ahead) is non-walkable.
                // here we try to build another path around it instead.
                // if it's not found, we continue to walk along the old path.
                List<Vector2i> path2 = null;
                int pnum = path.Count - 1;
                while (path2 == null && pnum >= 0)
                {
                    path2 = unit.DecideNextMove(path[pnum].x, path[pnum].y, false);
                    pnum--;
                }

                if (path2 != null)
                    path = path2;

                break;
            }
        }

        // if NEXT node is not walkable, we drop into idle state.
        if (unit.Interaction.CheckWalkableForUnit(path[0].x, path[0].y, false))
        {
            // next path node found
            // notify clients
            unit.AddActions(new MoveAction(unit, path[0].x, path[0].y), new RotateAction(unit, unit.FaceCell(path[0].x, path[0].y)));
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

        if (!Unit.Interaction.CheckCanAttack(TargetUnit))
            return false;

        // assume melee attack right now
        // check if in direct proximity
        if (Unit.Interaction.GetClosestDistanceTo(TargetUnit) <= Unit.Interaction.GetAttackRange() + 0.5f)
        {
            // in direct proximity!
            // 
            Vector2i enemyCell = TargetUnit.Interaction.GetClosestPointTo(Unit);
            int angleNeeded = Unit.FaceCellPrecise(enemyCell.x, enemyCell.y);
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

public class PickupState : IUnitState
{
    private MapUnit Unit;
    private int TargetX;
    private int TargetY;

    public PickupState(MapUnit unit, int x, int y)
    {
        Unit = unit;
        TargetX = x;
        TargetY = y;
    }

    public bool Process()
    {
        if (Unit.Stats.Health <= 0)
            return false;

        // check if sack still exists
        MapSack targetsack = MapLogic.Instance.GetSackAt(TargetX, TargetY);
        if (targetsack == null)
            return false;

        // if unit is on target cell, just pick up
        if (Unit.X <= TargetX && Unit.Y <= TargetY &&
            Unit.X+Unit.Width > TargetX && Unit.Y+Unit.Height > TargetY)
        {
            // pick the target sack up.
            // add money
            for (int i = 0; i < targetsack.Pack.Count; i++)
            {
                Item newItem = Unit.ItemsPack.PutItem(Unit.ItemsPack.Count, new Item(targetsack.Pack[i], targetsack.Pack[i].Count));
                // display "you have picked up ..."
                Server.NotifyItemPickup(Unit, targetsack.Pack[i].Class.ItemID, newItem.Count);
            }

            if (targetsack.Pack.Money > 0)
            {
                Unit.ItemsPack.Money += targetsack.Pack.Money;
                Server.NotifyItemPickup(Unit, -1, targetsack.Pack.Money);
            }

            MapLogic.Instance.RemoveSackAt(TargetX, TargetY);

            if (NetworkManager.IsServer)
                Server.NotifyUnitPack(Unit);

            return false; // done
        }
        else
        {
            MoveState.TryWalkTo(Unit, TargetX, TargetY);
            return true;
        }
    }
}