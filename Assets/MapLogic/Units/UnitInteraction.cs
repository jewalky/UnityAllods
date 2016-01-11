using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class UnitInteraction
{
    private MapUnit Unit = null;

    public UnitInteraction(MapUnit unit)
    {
        Unit = unit;
    }

    // returns true if cell is walkable for this unit
    public bool CheckWalkableForUnit(int x, int y, bool staticOnly)
    {
        for (int ly = y; ly < y + Unit.Height; ly++)
        {
            for (int lx = x; lx < x + Unit.Width; lx++)
            {
                // skip cells currently taken
                if (MapLogic.Instance.Nodes[lx, ly].Objects.Contains(Unit))
                    continue; // if we are already on this cell, skip it as passible
                uint tile = MapLogic.Instance.Nodes[lx, ly].Tile;
                MapNodeFlags flags = MapLogic.Instance.Nodes[lx, ly].Flags;
                if (Unit.IsWalking && (flags & MapNodeFlags.Unblocked) == 0 && (tile >= 0x1C0 && tile <= 0x2FF))
                    return false;
                MapNodeFlags bAir = staticOnly ? MapNodeFlags.BlockedAir : MapNodeFlags.BlockedAir | MapNodeFlags.DynamicAir;
                MapNodeFlags bGround = staticOnly ? MapNodeFlags.BlockedGround : MapNodeFlags.BlockedGround | MapNodeFlags.DynamicGround;
                if (Unit.IsFlying && (flags & bAir) != 0) return false;
                else if (!Unit.IsFlying && (flags & bGround) != 0)
                    return false;
            }
        }

        return true;
    }

    public float GetAttackRange()
    {
        Item weapon = Unit.GetItemFromBody(MapUnit.BodySlot.Weapon);
        if (weapon != null)
            return Math.Max(1, weapon.Class.Option.Range);
        return 1;
    }

    public bool CheckCanAttack(MapUnit other)
    {
        if (other.IsFlying && !Unit.IsFlying && GetAttackRange() <= 1)
            return false;
        return true;
    }

    public Vector2i GetClosestPointTo(int x, int y)
    {
        Vector2i cPt = new Vector2i(x, y);
        int cX = 256;
        int cY = 256;
        for (int ly = Unit.Y; ly < Unit.Y + Unit.Height; ly++)
        {
            for (int lx = Unit.X; lx < Unit.X + Unit.Width; lx++)
            {
                int xDist = Math.Abs(x - lx);
                int yDist = Math.Abs(y - ly);
                if (xDist < cX || yDist < cY)
                {
                    cX = xDist;
                    cY = yDist;
                    cPt = new Vector2i(lx, ly);
                }
            }
        }

        return cPt;

    }

    public Vector2i GetClosestPointTo(MapUnit other)
    {
        return GetClosestPointTo(other.X, other.Y);
    }

    public float GetClosestDistanceTo(MapUnit other)
    {
        if (other == Unit)
            return 0;

        Vector2i cPt1 = new Vector2i(Unit.X, Unit.Y);
        Vector2i cPt2 = new Vector2i(other.X, other.Y);
        int cX = 256;
        int cY = 256;
        for (int ly = Unit.Y; ly < Unit.Y + Unit.Height; ly++)
        {
            for (int lx = Unit.X; lx < Unit.X + Unit.Width; lx++)
            {
                for (int lly = other.Y; lly < other.Y + other.Height; lly++)
                {
                    for (int llx = other.X; llx < other.X + other.Width; llx++)
                    {
                        int xDist = Math.Abs(llx - lx);
                        int yDist = Math.Abs(lly - ly);

                        if (xDist < cX || yDist < cY)
                        {
                            cPt1 = new Vector2i(lx, ly);
                            cPt2 = new Vector2i(llx, lly);
                            cX = xDist;
                            cY = yDist;
                        }
                    }
                }
            }
        }

        return (cPt1 - cPt2).magnitude;
    }

}