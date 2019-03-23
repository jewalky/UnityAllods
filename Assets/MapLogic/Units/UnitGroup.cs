using UnityEngine;
using System;
using System.Collections.Generic;

public class UnitGroup
{
    [Flags]
    public enum GroupFlags
    {
        AiInstantEnabled = 0b0001,
        RandomPositions = 0b0010,
        QuestKill = 0b0100,
        QuestIntercept = 0b1000
    }

    public UnitGroup(uint id)
    {
        GroupID = id;
        Units = new List<MapUnit>();
    }

    public GroupFlags Flags;
    public uint InstanceID;
    public uint RepopTime;
    public uint GroupID;
    public List<MapUnit> Units;
    public bool Respawnable = true;
    private uint LastUnitDeathTime = 0;

    internal void NoteUnitDeath(MapUnit unit)
    {
        Units.Remove(unit);
        if (Units.Count == 0)
            LastUnitDeathTime = RepopTime;
    }

}
