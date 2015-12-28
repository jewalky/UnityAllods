using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ProtoBuf;

namespace ClientCommands
{
    [ProtoContract]
    [NetworkPacketId(ClientIdentifiers.AddPlayer)]
    public struct AddPlayer : IClientCommand
    {
        [ProtoMember(1)]
        public int ID;
        [ProtoMember(2)]
        public string Name;
        [ProtoMember(3)]
        public int Color;
        [ProtoMember(4)]
        public long Money;
        [ProtoMember(5)]
        public Dictionary<int, DiplomacyFlags> Diplomacy;
        [ProtoMember(6)]
        public bool Silent; // whether to display the "player has connected" or not.
        [ProtoMember(7)]
        public bool ConsolePlayer; // this is true when the server says us that we have control over this one.

        public bool Process()
        {
            if (!MapLogic.Instance.IsLoaded)
                return false;
            if (MapLogic.Instance.GetPlayerByID(ID) != null)
                return false; // player already exists: this should NOT happen

            MapLogicPlayer player = new MapLogicPlayer((ServerClient)null);
            player.ID = ID;
            player.Name = Name;
            player.Color = Color;
            player.Money = Money;
            foreach (var pair in Diplomacy)
                player.Diplomacy[pair.Key] = pair.Value;
            if (ConsolePlayer)
            {
                GameConsole.Instance.WriteLine("We are controlling player {0}.", player.Name);
                MapLogic.Instance.ConsolePlayer = player;
            }
            MapLogic.Instance.AddNetPlayer(player, Silent);
            return true;
        }
    }

    [ProtoContract]
    [NetworkPacketId(ClientIdentifiers.DelPlayer)]
    public struct DelPlayer : IClientCommand
    {
        [ProtoMember(1)]
        public int ID;
        [ProtoMember(2)]
        public bool Kick; // whether the "player was kicked" message will be displayed (if Silent is false)
        [ProtoMember(3)]
        public bool Silent;

        public bool Process()
        {
            if (!MapLogic.Instance.IsLoaded)
                return false;
            MapLogicPlayer player = MapLogic.Instance.GetPlayerByID(ID);
            if (player == MapLogic.Instance.ConsolePlayer)
                MapLogic.Instance.ConsolePlayer = null;
            if (player != null)
                MapLogic.Instance.DelNetPlayer(player, Silent, Kick);
            return true;
        }
    }

    [ProtoContract]
    [NetworkPacketId(ClientIdentifiers.ChatMessage)]
    public struct ChatMessage : IClientCommand
    {
        [ProtoMember(1)]
        public int PlayerID;
        [ProtoMember(2)]
        public string Text;

        public bool Process()
        {
            if (!MapLogic.Instance.IsLoaded)
                return false;
            MapLogicPlayer player = (PlayerID > 0) ? MapLogic.Instance.GetPlayerByID(PlayerID) : null;
            int color = (player != null) ? player.Color : MapLogicPlayer.AllColorsSystem;
            string text = (player != null) ? player.Name + ": " + Text : "<server>: " + Text;
            MapViewChat.Instance.AddChatMessage(color, text);
            return true;
        }
    }

    [ProtoContract]
    [NetworkPacketId(ClientIdentifiers.LogicFrame)]
    public struct LogicFrame : IClientCommand
    {
        public bool Process()
        {
            if (!MapLogic.Instance.IsLoaded)
                return false;
            MapLogic.Instance.Update();
            return true;
        }
    }
}