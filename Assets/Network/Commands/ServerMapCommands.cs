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

        private Item GetItem(MapUnit unit)
        {
            Item item = null;
            switch (Source)
            {
                case ItemMoveLocation.UnitBody:
                    item = unit.GetItemFromBody((MapUnit.BodySlot)SourceIndex);
                    break;
                case ItemMoveLocation.UnitPack:
                    item = unit.ItemsPack[SourceIndex];
                    break;
            }

            return item;
        }

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
                    item = GetItem(unit);
                    if (!unit.IsItemUsable(item)) return true; // can't use
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

                case ItemMoveLocation.Ground:
                    if (unit == null) return true;
                    item = TakeItem(unit);
                    if (item == null) return true;
                    if (Math.Abs(CellX - unit.X) > 2 ||
                        Math.Abs(CellY - unit.Y) > 2)
                    {
                        CellX = unit.X;
                        CellY = unit.Y;
                    }
                    ItemPack pack = new ItemPack();
                    pack.PutItem(0, new Item(item, item.Count));
                    MapLogic.Instance.PutSackAt(CellX, CellY, pack, false);
                    break;
            }

            Server.NotifyUnitPack(unit);

            return true;
        }
    }

    [ProtoContract]
    [NetworkPacketId(ServerIdentifiers.UnitPickup)]
    public struct UnitPickup : IServerCommand
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
            // we can't do anything with units that don't belong to our player.
            if (unit.Player != player)
                return true;

            unit.SetState(new PickupState(unit, X, Y));
            return true;
        }
    }

    [ProtoContract]
    [NetworkPacketId(ServerIdentifiers.CastToUnit)]
    public struct CastToUnit : IServerCommand
    {
        [ProtoMember(1)]
        public int TagFrom;
        [ProtoMember(2)]
        public int TagTo;
        [ProtoMember(3)]
        public int SpellID;
        [ProtoMember(4)]
        public int ItemID;

        public bool Process(ServerClient client)
        {
            if (client.State != ClientState.Playing)
                return false;

            Player player = MapLogic.Instance.GetNetPlayer(client);
            if (player == null)
                return false; // huehue, same as "order error" in a2server.exe, except we just boot them

            MapUnit unitFrom = MapLogic.Instance.GetUnitByTag(TagFrom);
            // we can't do anything with units that don't belong to our player.
            if (unitFrom.Player != player)
                return true;

            MapUnit unitTo = MapLogic.Instance.GetUnitByTag(TagTo);

            Spell cspell = unitFrom.GetSpell((Spell.Spells)SpellID, (ushort)ItemID);
            if (cspell == null)
                return true; // spell not found

            unitFrom.SetState(new CastState(unitFrom, cspell, unitTo));
            return true;
        }
    }

    [ProtoContract]
    [NetworkPacketId(ServerIdentifiers.CastToArea)]
    public struct CastToArea : IServerCommand
    {
        [ProtoMember(1)]
        public int TagFrom;
        [ProtoMember(2)]
        public int SpellID;
        [ProtoMember(3)]
        public int TargetX;
        [ProtoMember(4)]
        public int TargetY;
        [ProtoMember(5)]
        public int ItemID;

        public bool Process(ServerClient client)
        {
            if (client.State != ClientState.Playing)
                return false;

            Player player = MapLogic.Instance.GetNetPlayer(client);
            if (player == null)
                return false; // huehue, same as "order error" in a2server.exe, except we just boot them

            MapUnit unitFrom = MapLogic.Instance.GetUnitByTag(TagFrom);
            // we can't do anything with units that don't belong to our player.
            if (unitFrom == null || unitFrom.Player != player)
                return true;

            Spell cspell = unitFrom.GetSpell((Spell.Spells)SpellID, (ushort)ItemID);
            if (cspell == null)
                return true; // spell not found

            unitFrom.SetState(new CastState(unitFrom, cspell, TargetX, TargetY));
            return true;
        }
    }

    [ProtoContract]
    [NetworkPacketId(ServerIdentifiers.UseStructure)]
    public struct UseStructure : IServerCommand
    {
        [ProtoMember(1)]
        public int UnitTag;
        [ProtoMember(2)]
        public int StructureTag;

        public bool Process(ServerClient client)
        {
            if (client.State != ClientState.Playing)
                return false;

            Player player = MapLogic.Instance.GetNetPlayer(client);
            if (player == null)
                return false;

            MapUnit unit = MapLogic.Instance.GetUnitByTag(UnitTag);
            if (unit == null || unit.Player != player)
                return true;

            MapStructure structure = MapLogic.Instance.GetStructureByTag(StructureTag);
            if (structure == null || structure.Class == null || !structure.Class.Usable)
                return false;

            unit.SetState(new UseStructureState(unit, structure));
            return true;
        }
    }

    [ProtoContract]
    [NetworkPacketId(ServerIdentifiers.LeaveStructure)]
    public struct LeaveStructure : IServerCommand
    {
        public bool Process(ServerClient client)
        {
            if (client.State != ClientState.Playing)
                return false;

            Player player = MapLogic.Instance.GetNetPlayer(client);
            if (player == null)
                return false;

            // find all units of the player and leave all structures
            foreach (MapObject mobj in player.Objects)
                if (mobj is MapUnit unit && unit.CurrentStructure != null)
                    unit.CurrentStructure.HandleUnitLeave(unit);

            Server.NotifyLeaveStructure(player);
            return true;
        }
    }
}