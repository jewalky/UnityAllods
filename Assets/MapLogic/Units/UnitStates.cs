using UnityEngine;
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
        return true;
    }
}

class UnitStateWalker
{
    private const int SCAN_RATE = MapLogic.TICRATE;

    private int LastGoodPath;
    private int LastBadPath;
    private int RandomOffset;
    private static System.Random Random = null;

    public UnitStateWalker()
    {
        LastBadPath = 0;
        LastGoodPath = MapLogic.Instance.LevelTime;
        if (Random == null)
            Random = new System.Random();
        RandomOffset = SCAN_RATE + (Random.Next(0, SCAN_RATE) - (SCAN_RATE / 2));
    }

    public bool TryWalkTo(MapUnit unit, int walkX, int walkY, int walkWidth, int walkHeight, float distance = 0)
    {
        // check if we should not search yet
        if (LastBadPath > LastGoodPath && LastBadPath + RandomOffset > MapLogic.Instance.LevelTime)
            return false; // don't check for some time after bad path

        // check if target is walkable for us (statically)
        if (distance < 0)
            distance = 0;

        // note: 0x0 = specific cell
        // more than 0x0 = unit
        // for now just call pathfinding here

        LastBadPath = MapLogic.Instance.LevelTime;

        // try to pathfind
        Vector2i point = unit.DecideNextMove(walkX, walkY, walkWidth, walkHeight, distance);
        if (point == null)
            return false;

        // if NEXT node is not walkable, we drop into idle state.
        if (unit.Interaction.CheckWalkableForUnit(point.x, point.y, false))
        {
            LastGoodPath = MapLogic.Instance.LevelTime;
            // next path node found
            // notify clients
            unit.AddActions(new MoveAction(unit, point.x, point.y), new RotateAction(unit, unit.FaceCell(point.x, point.y)));
            return true;
        }

        return false;
    }
}

public class MoveState : IUnitState
{
    private MapUnit Unit;
    private int WalkX;
    private int WalkY;
    private int LastGoodPath;
    private int LastBadPath;
    private UnitStateWalker Walker;

    public MoveState(MapUnit unit, int x, int y)
    {
        Unit = unit;
        WalkX = x;
        WalkY = y;
        LastBadPath = 0;
        LastGoodPath = MapLogic.Instance.LevelTime;
        Walker = new UnitStateWalker();
    }

    public bool Process()
    {
        if (Unit.Stats.Health <= 0)
            return false;

        if (Unit.X == WalkX && Unit.Y == WalkY)
            return false;

        if (MapLogic.Instance.LevelTime - LastGoodPath > 5 * MapLogic.TICRATE && LastBadPath > LastGoodPath)
            return false; // if last good path was found more time ago, and last path was bad... break out, but wait for 5 seconds

        if (!Walker.TryWalkTo(Unit, WalkX, WalkY, 0, 0))
        {
            LastBadPath = MapLogic.Instance.LevelTime;
            return true;
        }
        else LastGoodPath = MapLogic.Instance.LevelTime;

        return true;
    }
}

public class AttackState : IUnitState
{
    private MapUnit Unit;
    private MapUnit TargetUnit;
    private UnitStateWalker Walker;

    public AttackState(MapUnit unit, MapUnit targetUnit)
    {
        Unit = unit;
        TargetUnit = targetUnit;
        Walker = new UnitStateWalker();
    }

    public bool Process()
    {
        if (Unit.Stats.Health <= 0)
            return false;

        if (TargetUnit == Unit || !TargetUnit.IsAlive || !TargetUnit.IsLinked)
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
            //Debug.LogFormat("ID {0} ATTACKING", Unit.ID);
            int damage = Random.Range(Unit.Stats.DamageMin, Unit.Stats.DamageMax);
            DamageFlags df = Unit.GetDamageType();
            // we need to compare Option to set damage flags properly here
            Unit.AddActions(new AttackAction(Unit, TargetUnit, df, damage));
        }
        else
        {
            //Debug.LogFormat("ID {0} TRY WALK TO", Unit.ID);
            // make one step to the target.
            Walker.TryWalkTo(Unit, TargetUnit.X, TargetUnit.Y, TargetUnit.Width, TargetUnit.Height, Unit.Interaction.GetAttackRange());
        }

        return true;
    }
}

public class PickupState : IUnitState
{
    private MapUnit Unit;
    private int TargetX;
    private int TargetY;
    private UnitStateWalker Walker;

    public PickupState(MapUnit unit, int x, int y)
    {
        Unit = unit;
        TargetX = x;
        TargetY = y;
        Walker = new UnitStateWalker();
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
            Walker.TryWalkTo(Unit, TargetX, TargetY, 0, 0);
            return true;
        }
    }
}

public class CastState : IUnitState
{
    private MapUnit Unit;
    private Spell Spell;
    private MapUnit TargetUnit;
    private int TargetX;
    private int TargetY;
    private bool Executed;
    private bool IsAttack;
    private UnitStateWalker Walker;

    public CastState(MapUnit unit, Spell spell, MapUnit targetUnit)
    {
        Unit = unit;
        Spell = spell;
        TargetUnit = targetUnit;
        TargetX = TargetY = -1;
        Executed = false;
        IsAttack = Spell.IsAttackSpell(spell.SpellID);
        Walker = new UnitStateWalker();
    }

    public CastState(MapUnit unit, Spell spell, int targetX, int targetY)
    {
        Unit = unit;
        Spell = spell;
        TargetUnit = null;
        TargetX = targetX;
        TargetY = targetY;
        Executed = false;
        IsAttack = global::Spell.IsAttackSpell(spell.SpellID);
        Walker = new UnitStateWalker();
    }

    public bool Process()
    {
        // check target. if target is outside map range, terminate. server doesn't really handle this well
        if (TargetX < 8 || TargetY < 8 || TargetX >= MapLogic.Instance.Width - 8 || TargetY >= MapLogic.Instance.Height - 8)
        {
            if (TargetUnit != null)
            {
                TargetX = -1;
                TargetY = -1;
            }
            else return false;
        }

        if (Executed && Unit.Actions[Unit.Actions.Count - 1].GetType() != typeof(AttackAction))
            return false;

        if (Unit.Stats.Health <= 0)
            return false;

        if ((IsAttack && TargetUnit == Unit) || (TargetUnit != null && (!TargetUnit.IsAlive || !TargetUnit.IsLinked)))
            return false;

        if (TargetUnit != null && !Unit.Interaction.CheckCanAttack(TargetUnit))
            return false;

        // assume melee attack right now
        // check if in direct proximity
        if ((TargetUnit != null && Unit.Interaction.GetClosestDistanceTo(TargetUnit) <= Spell.GetDistance() + 0.5f) ||
            (TargetUnit == null && (Unit.Interaction.GetClosestPointTo(TargetX, TargetY)-new Vector2i(TargetX, TargetY)).magnitude <= Spell.GetDistance() + 0.5f))
        {
            // in direct proximity!
            // 
            if (TargetUnit != Unit)
            {
                Vector2i enemyCell = (TargetUnit != null ? TargetUnit.Interaction.GetClosestPointTo(Unit) : new Vector2i(TargetX, TargetY));
                int angleNeeded = Unit.FaceCellPrecise(enemyCell.x, enemyCell.y);
                if (Unit.Angle != angleNeeded)
                {
                    Unit.AddActions(new RotateAction(Unit, angleNeeded));
                    return true;
                }
            }

            //
            //Debug.LogFormat("ATTACKING");
            if ((Spell.Item != null || Unit.Stats.Mana >= Spell.Template.ManaCost) &&
                (!Spell.ItemDisposable || Unit.ItemsPack.Contains(Spell.Item)))
            {
                Unit.AddActions(new AttackAction(Unit, TargetUnit, Spell, TargetX, TargetY));
                if (Spell.Item == null && Unit.Stats.TrySetMana(Unit.Stats.Mana - Spell.Template.ManaCost) && NetworkManager.IsServer)
                    Server.NotifyUnitStatsShort(Unit);
                else if (Spell.Item != null && Spell.ItemDisposable && Unit.ItemsPack.TakeItem(Spell.Item, 1) != null)
                    Server.NotifyUnitPack(Unit);
                Unit.DoUpdateView = true;
                Unit.DoUpdateInfo = true;
            }
            else return false; // :( no mana
            Executed = true;
        }
        else
        {
            // make one step to the target.
            if (TargetUnit != null)
                Walker.TryWalkTo(Unit, TargetUnit.X, TargetUnit.Y, TargetUnit.Width, TargetUnit.Height, Spell.GetDistance());
            else Walker.TryWalkTo(Unit, TargetX, TargetY, 0, 0, Spell.GetDistance());
        }

        return true;
    }
}