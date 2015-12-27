using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

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
    Dormant     = 0x0004,
    NetClient   = 0x0008
}

public class MapLogicPlayer
{
    public static Color32[] AllColors =
        { new Color32(0x52, 0x79, 0xE7, 0xFF),
          new Color32(0x52, 0x79, 0xE7, 0xFF),
          new Color32(0x84, 0xE7, 0x52, 0xFF),
          new Color32(0xE7, 0x55, 0x7B, 0xFF),
          new Color32(0xE7, 0x71, 0x52, 0xFF),
          new Color32(0xAD, 0x55, 0xE7, 0xFF),
          new Color32(0xE7, 0x9A, 0x52, 0xFF),
          new Color32(0x52, 0xE7, 0xBD, 0xFF),
          new Color32(0x9C, 0x9E, 0x9C, 0xFF),
          new Color32(0x21, 0x45, 0xBD, 0xFF),
          new Color32(0x52, 0xBE, 0x21, 0xFF),
          new Color32(0xBD, 0x20, 0x4A, 0xFF),
          new Color32(0xBD, 0x3C, 0x21, 0xFF),
          new Color32(0x84, 0x20, 0xBD, 0xFF),
          new Color32(0xBD, 0x69, 0x21, 0xFF),
          new Color32(0x21, 0xBE, 0x94, 0xFF),
          new Color32(0x6B, 0x6D, 0x6B, 0xFF),
          new Color32(0x00, 0x00, 0x10, 0xFF) };

    public int ID { get; set; }
    public int Color { get; set; }
    public MapLogicPlayerFlags Flags { get; set; }
    public long Money { get; set; }
    public string Name { get; set; }
    public Dictionary<int, DiplomacyFlags> Diplomacy { get; private set; }

    // only for human players
    public ServerClient NetClient { get; private set; }

    public MapLogicPlayer(AllodsMap.AlmPlayer almp)
    {
        Diplomacy = new Dictionary<int, DiplomacyFlags>();

        // these are only AI players.
        // AI players will have money set at 0 no matter what.
        ID = MapLogic.Instance.GetFreePlayerID(true);
        Color = almp.Color;
        Flags |= MapLogicPlayerFlags.AI;
        if ((almp.Flags & 0x01) == 0) Flags |= MapLogicPlayerFlags.Dormant;
        if ((almp.Flags & 0x02) != 0) Flags |= MapLogicPlayerFlags.QuestKill;
        Money = 0;
        Name = almp.Name;
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

    public MapLogicPlayer(ServerClient client)
    {
        Diplomacy = new Dictionary<int, DiplomacyFlags>();

        // this player is always a Human player, i.e. we never set any additional flags on it.
        ID = MapLogic.Instance.GetFreePlayerID(false);
        Color = ID % 16; // we can have only 16 colors for humans
        // this will later be used for disconnected player timeout.
        // also on the client, network players have this flag, but null NetClient field.
        Flags = MapLogicPlayerFlags.NetClient;
        Money = 0;
        Name = string.Format("Player {0}", ID);
        NetClient = client;
        // set default diplomacy with AI players based on AI players diplomacy to Self
        MapLogicPlayer Self = MapLogic.Instance.GetPlayerByName("Self");
        for (int i = 0; i < 16; i++)
        {
            MapLogicPlayer p = MapLogic.Instance.GetPlayerByID(i);
            // skip null or non-AI players (although everything below 16 should be AI, just make sure here)
            if (p == null || ((p.Flags & MapLogicPlayerFlags.AI) == 0)) continue;
            DiplomacyFlags dAItoSelf = p.Diplomacy[Self.ID];
            DiplomacyFlags dSelftoAI = Self.Diplomacy[p.ID];
            p.Diplomacy[ID] = dAItoSelf;
            Diplomacy[p.ID] = dSelftoAI;
        }
    }
}