using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ProtoBuf;

namespace ServerCommands
{
    [ProtoContract]
    [NetworkPacketId(ServerIdentifiers.ChatMessage)]
    public struct ChatMessage : IServerCommand
    {
        [ProtoMember(1)]
        public string Text;

        public bool Process(ServerClient client)
        {
            if (client.State != ClientState.Playing)
                return false;

            Player player = MapLogic.Instance.GetNetPlayer(client);
            if (player == null)
                return false; // huehue, same as "order error" in a2server.exe, except we just boot them

            // local chat presentation. on a server.
            int color = player.Color;
            string text = player.Name + ": " + Text;
            MapViewChat.Instance.AddChatMessage(color, text);

            Server.NotifyChatMessage(player, Text);
            return true;
        }
    }

    [ProtoContract]
    [NetworkPacketId(ServerIdentifiers.MoveUnit)]
    public struct MoveUnit : IServerCommand
    {
        [ProtoMember(1)]
        public int Tag;
        [ProtoMember(2)]
        public int X;
        [ProtoMember(3)]
        public int Y;

        public bool Process(ServerClient client)
        {
            if (client.State != ClientState.Playing)
                return false;

            Player player = MapLogic.Instance.GetNetPlayer(client);
            if (player == null)
                return false; // huehue, same as "order error" in a2server.exe, except we just boot them

            MapUnit unit = MapLogic.Instance.GetUnitByTag(Tag);
            if (unit == null)
                return false; // bad desync

            if (unit.Player != player)
                return true; // do nothing

            unit.SetState(new MoveState(unit, X, Y));
            return true;
        }
    }

    [ProtoContract]
    [NetworkPacketId(ServerIdentifiers.AttackUnit)]
    public struct AttackUnit : IServerCommand
    {
        [ProtoMember(1)]
        public int Tag;
        [ProtoMember(2)]
        public int TargetTag;

        public bool Process(ServerClient client)
        {
            if (client.State != ClientState.Playing)
                return false;

            Player player = MapLogic.Instance.GetNetPlayer(client);
            if (player == null)
                return false; // huehue, same as "order error" in a2server.exe, except we just boot them

            MapUnit unit = MapLogic.Instance.GetUnitByTag(Tag);
            if (unit == null)
                return false; // bad desync

            if (unit.Player != player)
                return true; // do nothing

            MapUnit targetUnit = MapLogic.Instance.GetUnitByTag(TargetTag);
            if (targetUnit == null)
                return false;

            unit.SetState(new AttackState(unit, targetUnit));
            return true;
        }
    }

    [ProtoContract]
    [NetworkPacketId(ServerIdentifiers.RespawnAvatar)]
    public struct RespawnAvatar : IServerCommand
    {
        public bool Process(ServerClient client)
        {
            if (client.State != ClientState.Playing)
                return false;

            Player player = MapLogic.Instance.GetNetPlayer(client);
            if (player == null)
                return false; // huehue, same as "order error" in a2server.exe, except we just boot them

            if (player.Avatar == null)
                return false;

            if (player.Avatar.IsAlive)
                return true;

            player.Avatar.Respawn(16, 16);
            return true;
        }
    }

    // UnitBody/UnitPack <-> Ground
    // UnitBody <-> UnitPack
    // UnitBody/UnitPack <-> ShopShelf1/ShopShelf2/ShopShelf3/ShopShelf4
    public enum ItemMoveLocation
    {
        UnitBody,
        UnitPack,
        Ground
    }

    [ProtoContract]
    [NetworkPacketId(ServerIdentifiers.ItemMove)]
    public struct ItemMove : IServerCommand
    {
        [ProtoMember(1)]
        public ItemMoveLocation Source;
        [ProtoMember(2)]
        public ItemMoveLocation Destination;
        [ProtoMember(3)]
        public int SourceIndex;
        [ProtoMember(4)]
        public int DestinationIndex;
        [ProtoMember(5)]
        public int UnitTag;
        [ProtoMember(6)]
        public int CellX;
        [ProtoMember(7)]
        public int CellY;
        [ProtoMember(8)]
        public int Count;

        private Item TakeItem(MapUnit unit)
        {
            Item item = null;
            switch (Source)
            {
                case ItemMoveLocation.UnitBody:
                    item = unit.TakeItemFromBody((MapUnit.BodySlot)SourceIndex);
                    break;
                case ItemMoveLocation.UnitPack:
                    item = unit.ItemsPack.TakeItem(SourceIndex, Count);
                    break;
            }

            return item;
        }

        public bool Process(ServerClient client)
        {
            if (client.State != ClientState.Playing)
                return false;

            Player player = MapLogic.Instance.GetNetPlayer(client);
            if (player == null)
                return false; // huehue, same as "order error" in a2server.exe, except we just boot them

            MapUnit unit = MapLogic.Instance.GetUnitByTag(UnitTag);
            // we can't do anything with units that don't belong to our player.
            if (unit.Player != player)
                return true;

            // right now only body<->pack is supported, no shop or ground items
            Item item = null;

            switch (Destination)
            {
                case ItemMoveLocation.UnitBody:
                    if (unit == null) return true;
                    item = TakeItem(unit);
                    if (item == null) return true;
                    unit.PutItemToBody((MapUnit.BodySlot)item.Class.Option.Slot, item);
                    break;

                case ItemMoveLocation.UnitPack:
                    if (unit == null) return true;
                    item = TakeItem(unit);
                    if (item == null) return true;
                    unit.ItemsPack.PutItem(DestinationIndex, item);
                    break;
            }

            Server.NotifyUnitPack(unit);

            return true;
        }
    }
}