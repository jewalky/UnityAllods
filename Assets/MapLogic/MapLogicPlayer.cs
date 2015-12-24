using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

[Flags]
public enum DiplomacyFlags
{
    Enemy   = 0x0001,
    Ally    = 0x0002,
    Vision  = 0x0010,
    Ignore  = 0x0020
}

[Flags]
public enum MapLogicPlayerFlags
{
    AI          = 0x0001,
    QuestKill   = 0x0002,
    Dormant     = 0x0004
}

public class MapLogicPlayer
{
    public int ID { get; private set; }
    public int Color { get; private set; }
    public MapLogicPlayerFlags Flags { get; private set; }
    public long Money { get; private set; }
    public string Name { get; private set; }
    public Dictionary<int, DiplomacyFlags> Diplomacy { get; private set; }

    public MapLogicPlayer(AllodsMap.AlmPlayer almp)
    {
        Diplomacy = new Dictionary<int, DiplomacyFlags>();

        // these are only AI players.
        // AI players will have money set at 0 no matter what.
        Color = almp.Color;
        Flags |= MapLogicPlayerFlags.AI;
        if ((almp.Flags & 0x01) == 0) Flags |= MapLogicPlayerFlags.Dormant;
        if ((almp.Flags & 0x02) != 0) Flags |= MapLogicPlayerFlags.QuestKill;
        Money = 0;
        Name = almp.Name;
        ID = MapLogic.Instance.GetFreePlayerID(true);
        // set diplomacy with other AI players
        for (int i = 0; i < 16; i++)
        {
            DiplomacyFlags df = 0;
            if ((almp.Diplomacy[i] & 0x01) != 0) df |= DiplomacyFlags.Enemy;
            if ((almp.Diplomacy[i] & 0x02) != 0) df |= DiplomacyFlags.Ally;
            if ((almp.Diplomacy[i] & 0x10) != 0) df |= DiplomacyFlags.Vision;
            Diplomacy[i] = df;
        }
    }
}