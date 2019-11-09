using System;
using System.Collections.Generic;

[Flags]
public enum GroupFlags
{
    RandomPositions,
    QuestKill,
    QuestIntercept
}

public class Group
{
    private const int SCAN_RATE = MapLogic.TICRATE / 2;

    public int ID { get; set; }
    public int RepopDelay { get; set; }
    public GroupFlags Flags { get; set; }
    public MapUnit SharedTarget { get; private set; }
    public readonly List<MapUnit> Units = new List<MapUnit>();
    private int LastUnitAlive = 0;
    private int SightOffset = 0;
    private static Random Random = null;

    public Group()
    {
        if (Random == null)
            Random = new Random();
        SightOffset = Random.Next(0, SCAN_RATE);
    }

    private bool CheckSharedTarget(MapUnit from, MapUnit target)
    {
        if (from == null || target == null)
            return false;
        // order checks by performance
        if (!from.Player.Diplomacy[target.Player.ID].HasFlag(DiplomacyFlags.Enemy))
            return false;
        if (from.CanSeeUnit(target) < 2)
            return false;
        return true;
    }

    public void UpdateSight(bool onlyUnits)
    {
        // so, UpdateSight is called when we don't have a suitable target, and cannot guess it from group units aggro lists.
        // so we have to look at every unit's sight for enemies
        // uses closest enemy found
        float minDist = 65536f;
        MapUnit minTarget = null;
        for (int i = 0; i < Units.Count; i++)
        {
            MapUnit unit = Units[i];
            float unitMinDist = 65536f;
            MapUnit unitMinTarget = null;
            if (!unit.IsAlive || !unit.IsLinked)
                continue;
            bool hasAggro = unit.Aggro.Count > 0 && unit.Aggro[0].GetAggro() > 0;
            if (hasAggro && onlyUnits) continue;
            // sight table is 41x41. -20 to +20 from x/y
            for (int y = -20; y <= 20; y++)
            {
                if (y+unit.Y < 8 || y+unit.Y >= MapLogic.Instance.Height-8)
                    continue;
                for (int x = -20; x <= 20; x++)
                {
                    if (x+unit.X < 8 || x+unit.X >= MapLogic.Instance.Width-8)
                        continue;
                    if (!unit.Vision[x + 20, y + 20])
                        continue;
                    MapNode node = MapLogic.Instance.Nodes[x+unit.X, y+unit.Y];
                    for (int j = 0; j < node.Objects.Count; j++)
                    {
                        MapObject mobj = node.Objects[j];
                        if (!(mobj is MapUnit))
                            continue;
                        MapUnit checkUnit = (MapUnit)mobj;
                        // check if it's a good target anyway
                        if (!CheckSharedTarget(unit, checkUnit))
                            continue;
                        // check distance
                        float dist = (new Vector2i(checkUnit.X, checkUnit.Y) - new Vector2i(unit.X, unit.Y)).magnitude;
                        if (dist < minDist || minTarget == null)
                            minTarget = checkUnit;
                        if (dist < unitMinDist || unitMinTarget == null)
                            unitMinTarget = checkUnit;
                    }
                }
            }
            
            // set unit target, if it's aggro is 0 (no damage seen)
            if (unitMinTarget != null && (unit.Aggro.Count <= 0 || unit.Aggro[0].GetAggro() <= 0))
            {
                unit.Aggro.Clear();
                unit.Aggro.Add(new MapUnitAggro(unitMinTarget));
            }
        }

        if (onlyUnits)
            return;

        if (minTarget != null)
            SharedTarget = minTarget;

        if (SharedTarget != null)
        {
            //Debug.LogFormat("picked new target for group {0} (={1})", ID, SharedTarget.Class.DescText);
        }
    }

    public void Update()
    {
        // check if shared target is not valid
        if (SharedTarget != null && (!SharedTarget.IsAlive || !SharedTarget.IsLinked))
            SharedTarget = null;

        bool anyAlive = false;
        MapUnit possibleTarget = null;
        bool currentTargetIsOk = false;
        for (int i = 0; i < Units.Count; i++)
        {
            MapUnit unit = Units[i];
            if (!unit.IsAlive)
                continue;

            anyAlive = true;
            if (!unit.IsLinked)
                continue;

            if (!currentTargetIsOk && SharedTarget != null)
                currentTargetIsOk = CheckSharedTarget(unit, SharedTarget);

            // 
            if (unit.Target != null || unit.Aggro.Count > 0)
            {
                // pick next possible SharedTarget
                if (possibleTarget == null)
                {
                    if (CheckSharedTarget(unit, unit.Target))
                        possibleTarget = unit.Target;
                    if (possibleTarget == null)
                    {
                        for (int j = 0; j < unit.Aggro.Count; j++)
                        {
                            MapUnitAggro ag = unit.Aggro[j];
                            if (CheckSharedTarget(unit, ag.Target))
                            {
                                possibleTarget = ag.Target;
                                break;
                            }
                        }
                    }
                }
            }
        }

        if (!currentTargetIsOk && SharedTarget != null)
        {
            //Debug.LogFormat("lost shared target for group {0}", ID);
            SharedTarget = null;
        }

        // repop logic not implemented yet
        if (anyAlive)
        {
            LastUnitAlive = MapLogic.Instance.LevelTime;
        }
        else
        {
            // check repop timer, and repop units if it's done
            if (MapLogic.Instance.LevelTime-LastUnitAlive > RepopDelay)
            {
                for (int i = 0; i < Units.Count; i++)
                {
                    MapUnit unit = Units[i];
                    Vector2i randomPosition = (Flags.HasFlag(GroupFlags.RandomPositions) ? unit.FindRandomPosition(unit.SpawnX, unit.SpawnY, 5) : null);
                    if (randomPosition == null)
                        randomPosition = new Vector2i(unit.X, unit.Y);
                    unit.Respawn(randomPosition.x, randomPosition.y);
                }
                //Debug.LogFormat("group {0} is repopped");
            }
            //else Debug.LogFormat("group {0} will repop in {1} seconds", ID, (float)(RepopDelay - (MapLogic.Instance.LevelTime - LastUnitAlive)) / MapLogic.TICRATE);
        }

        if (anyAlive && SharedTarget == null)
        {
            // pick new shared target if there is one
            if (possibleTarget != null)
            {
                SharedTarget = possibleTarget;
            }
        }

        if (anyAlive)
        {
            if (((MapLogic.Instance.LevelTime + SightOffset) % SCAN_RATE) == 0)
            {
                UpdateSight(SharedTarget != null);
            }
        }
    }
}