using System;

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
        if (x < 8 || x > MapLogic.Instance.Width - 8 ||
            y < 8 || y > MapLogic.Instance.Height - 8) return false;
        for (int ly = y; ly < y + Unit.Height; ly++)
        {
            for (int lx = x; lx < x + Unit.Width; lx++)
            {
                MapNode node = MapLogic.Instance.Nodes[lx, ly];
                // skip cells currently taken
                if (node.Objects.Contains(Unit))
                    continue; // if we are already on this cell, skip it as passible
                uint tile = node.Tile;
                MapNodeFlags flags = node.Flags;
                if (Unit.IsWalking && !flags.HasFlag(MapNodeFlags.Unblocked) && flags.HasFlag(MapNodeFlags.BlockedTerrain))
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
        if (!Unit.CanDetectUnit(other))
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
        return GetClosestPointTo(other.TargetX, other.TargetY);
    }

    public float GetClosestDistanceTo(MapUnit other)
    {
        if (other == Unit)
            return 0;

        Vector2i cPt1 = new Vector2i(Unit.X, Unit.Y);
        Vector2i cPt2 = new Vector2i(other.TargetX, other.TargetY);
        int cX = 256;
        int cY = 256;
        for (int ly = Unit.Y; ly < Unit.Y + Unit.Height; ly++)
        {
            for (int lx = Unit.X; lx < Unit.X + Unit.Width; lx++)
            {
                // check current coords
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
                // check target coords
                for (int lly = other.TargetY; lly < other.TargetY + other.Height; lly++)
                {
                    for (int llx = other.TargetX; llx < other.TargetX + other.Width; llx++)
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

    // check if next node is dangerous. used in step-by-step pathfinding to detect that we need to recheck with dynamic mode
    public bool CheckDangerous(int x, int y)
    {
        for (int ly = y; ly < y + Unit.Height; ly++)
        {
            for (int lx = x; lx < x + Unit.Width; lx++)
            {
                MapNode node = MapLogic.Instance.Nodes[lx, ly];

                foreach (MapObject mobj in node.Objects)
                {
                    if (!(mobj is MapProjectile))
                        continue;
                    MapProjectile proj = (MapProjectile)mobj;
                    // now some magic ;)
                    if (proj.Class == null)
                        continue;

                    switch ((AllodsProjectile)proj.Class.ID)
                    {
                        case AllodsProjectile.FireWall:
                            if (Unit.Stats.ProtectionFire != 100)
                                return true;
                            break;
                        case AllodsProjectile.PoisonCloud:
                        case AllodsProjectile.Blizzard:
                            if (Unit.Stats.ProtectionWater != 100)
                                return true;
                            break;
                    }
                }
            }
        }

        return false;
    }

    // returns cost to walk to a node.
    // 1.5 = 150% cost, 0.5 = 50% cost. does not always relate to speed (may be related to spell effects being present)
    public float GetNodeCostFactor(int x, int y, bool staticOnly)
    {
        float nodeFactor = 0;
        int nodeCount = 0;

        for (int ly = y; ly < y + Unit.Height; ly++)
        {
            for (int lx = x; lx < x + Unit.Width; lx++)
            {
                MapNode node = MapLogic.Instance.Nodes[lx, ly];
                // for walking units, take base node speed as factor
                float baseNodeFactor = 1f;
                if (Unit.IsWalking)
                    baseNodeFactor = Math.Max((byte)1, node.BaseWalkCost) / 8f; // reverse formula for cost compared to speed

                // it would be strange if long-term path included spells and other temporary effects
                if (!staticOnly)
                {
                    foreach (MapObject mobj in node.Objects)
                    {
                        if (!(mobj is MapProjectile))
                            continue;
                        MapProjectile proj = (MapProjectile)mobj;
                        // now some magic ;)
                        if (proj.Class == null)
                            continue;
                        // logic: the more elemental protection unit has, the more likely it is that it will try to walk right through a spell
                        // we are multiplying it *2 because just "firefac" and "waterfac" don't seem to be powerful enough do make the unit go around with 50 protection
                        switch ((AllodsProjectile)proj.Class.ID)
                        {
                            case AllodsProjectile.FireWall:
                                float firefac = (100 - Unit.Stats.ProtectionFire) / 100f;
                                baseNodeFactor *= 1f + firefac*2;
                                break;
                            case AllodsProjectile.PoisonCloud:
                            case AllodsProjectile.Blizzard:
                                float waterfac = (100 - Unit.Stats.ProtectionWater) / 100f;
                                baseNodeFactor *= 1f + waterfac*2;
                                break;
                            // could check for SpecLight, SpecDarkness here. however ROM2 does not seem to do this
                            default:
                                break;
                        }
                    }
                }

                nodeFactor += baseNodeFactor;
                nodeCount++;
            }
        }

        return nodeFactor / nodeCount;
    }

    // returns speed of the unit
    // 1.5 = 150% speed, 0.5 = 50% speed
    // staticOnly is for now unused
    public float GetNodeSpeedFactor(int x, int y, bool staticOnly)
    {
        // flying and hovering units don't care about node speed factor
        if (!Unit.IsWalking)
            return 1f;

        float nodeFactor = 0;
        int nodeCount = 0;

        for (int ly = y; ly < y + Unit.Height; ly++)
        {
            for (int lx = x; lx < x + Unit.Width; lx++)
            {
                MapNode node = MapLogic.Instance.Nodes[lx, ly];
                nodeFactor += 8f / Math.Max((byte)1, node.BaseWalkCost);
                nodeCount++;
            }
        }

        return nodeFactor / nodeCount;
    }

    // returns time of move the unit from current point to target (meaning it is adjustment to current)
    // 400??? = 1 sec
    // staticOnly is for now unused
    public float GetMoveTime(int x, int y, bool staticOnly)
    {
        float Time = 1f / Unit.Stats.Speed;

        // flying and hovering units don't care about node speed factors
        if (Unit.IsWalking) {

	        int SurfaceFactor = 0;
	        int HeightChange = 0;
	        int Count = 0;
	        MapNode node;
	
	        for (int ly = Unit.Height; --ly >= 0; )
	        {
	            for (int lx = Unit.Width; --lx >= 0; )
	            {
	                node = MapLogic.Instance.Nodes[     x+lx,      y+ly];
	                SurfaceFactor += Math.Max((byte)8, node.BaseWalkCost);
	                HeightChange += node.Height;
	
	                node = MapLogic.Instance.Nodes[Unit.X+lx, Unit.Y+ly];
	                SurfaceFactor += Math.Max((byte)8, node.BaseWalkCost);
	                HeightChange -= node.Height;
	
	                Count+=2;
	            }
	        }
	
            Time = Time * SurfaceFactor / Count / 8f; // normal base tile cost

            if (x != Unit.X && y != Unit.Y)
                Time = Time * 1.4142f;
	   	
	   	    if (HeightChange > 0)
	   	        Time += HeightChange/128f /8f;
	   	    else
	   	        Time += HeightChange/128f /8f /4f;

	   }
	
       return Time;
    }

}