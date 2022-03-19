using System;
using UnityEngine;

public enum ClientState
{
    Disconnected,
    ConnectWait,
    Connected,
    DownloadingMap,
    DownloadedMap,
    Playing
}

public interface IClientCommand
{
    bool Process();
}

public class Client
{
    public static ClientState State = ClientState.Disconnected;

    public static void ConnectedToServer()
    {
        GameConsole.Instance.WriteLine("Connected to {0}:{1}!", ClientManager.ServerIPAddress, ClientManager.ServerIPPort);
        State = ClientState.Connected;

        // unload local map and tell the server that we're going to play.
        if (MapLogic.Instance.IsLoaded)
            MapLogic.Instance.Unload();
        ServerCommands.ClientAuth authCmd;
        ClientManager.SendCommand(authCmd);
    }

    public static void DisconnectedFromServer()
    {
        GameConsole.Instance.WriteLine("Disconnected from {0}:{1}.", ClientManager.ServerIPAddress, ClientManager.ServerIPPort);
        // also kill MapDownloader
        if (MapDownloader.Instance != null)
            MapDownloader.Instance.Kill();
        // also unload map, because if we were connected, we had server map.
        MapLogic.Instance.Unload();
    }

    public static void SendChatMessage(string text)
    {
        if (NetworkManager.IsClient)
        {
            if (State != ClientState.Playing)
                return;

            ServerCommands.ChatMessage chatCmd;
            chatCmd.Text = text;
            ClientManager.SendCommand(chatCmd);
        }
        else if (MapLogic.Instance.ConsolePlayer != null) // local game
        {
            MapViewChat.Instance.AddChatMessage(MapLogic.Instance.ConsolePlayer.Color, MapLogic.Instance.ConsolePlayer.Name + ": " + text);
        }
    }

    public static void SendMoveUnit(MapUnit unit, int x, int y)
    {
        if (NetworkManager.IsClient)
        {
            ServerCommands.MoveUnit walkCmd;
            walkCmd.Tag = unit.Tag;
            walkCmd.X = x;
            walkCmd.Y = y;
            ClientManager.SendCommand(walkCmd);
        }
        else
        {
            if (MapLogic.Instance.ConsolePlayer == unit.Player)
            {
                unit.SetState(new MoveState(unit, x, y));
            }
        }
    }

    public static void SendUseStructure(MapUnit unit, MapStructure s)
    {
        if (NetworkManager.IsClient)
        {
            ServerCommands.UseStructure useStructureCmd;
            useStructureCmd.UnitTag = unit.Tag;
            useStructureCmd.StructureTag = s.Tag;
            ClientManager.SendCommand(useStructureCmd);
        }
        else
        {
            if (MapLogic.Instance.ConsolePlayer == unit.Player)
            {
                unit.SetState(new UseStructureState(unit, s));
            }
        }
    }

    public static void SendLeaveStructure()
    {
        if (NetworkManager.IsClient)
        {
            ServerCommands.LeaveStructure leaveStructureCmd;
            ClientManager.SendCommand(leaveStructureCmd);
        }
        else
        {
            Player player = MapLogic.Instance.ConsolePlayer;
            // find all units of the player and leave all structures
            foreach (MapObject mobj in player.Objects)
            {
                if (mobj is MapUnit unit && unit.CurrentStructure != null)
                {
                    unit.CurrentStructure.HandleUnitLeave(unit);
                    Server.NotifyLeaveStructure(unit);
                }
            }
        }
    }

    public static void SendAttackUnit(MapUnit unit, MapUnit targetUnit)
    {
        if (NetworkManager.IsClient)
        {
            ServerCommands.AttackUnit atkCmd;
            atkCmd.Tag = unit.Tag;
            atkCmd.TargetTag = targetUnit.Tag;
            ClientManager.SendCommand(atkCmd);
        }
        else
        {
            if (MapLogic.Instance.ConsolePlayer == unit.Player)
            {
                unit.SetState(new AttackState(unit, targetUnit));
            }
        }
    }

    public static void SendPickupUnit(MapUnit unit, int x, int y)
    {
        if (NetworkManager.IsClient)
        {
            ServerCommands.UnitPickup pkpCmd;
            pkpCmd.Tag = unit.Tag;
            pkpCmd.X = x;
            pkpCmd.Y = y;
            ClientManager.SendCommand(pkpCmd);
        }
        else
        {
            if (MapLogic.Instance.ConsolePlayer == unit.Player)
            {
                unit.SetState(new PickupState(unit, x, y));
            }
        }
    }

    public static void SendCastToUnit(MapUnit unit, Spell cspell, MapUnit target, int x, int y)
    {
        if (cspell == null)
            return;

        if (cspell.Template.IsAreaSpell)
        {
            if (x <= unit.X+unit.Width && y <= unit.Y+unit.Height &&
                x >= unit.X && y >= unit.Y)
            {
                SendCastToArea(unit, cspell, x, y);
                return;
            }

            // x,y does not match unit coordinates. take unit as priority?
            Vector2i unitCoords = target.Interaction.GetClosestPointTo(unit);
            SendCastToArea(unit, cspell, unitCoords.x, unitCoords.y);
            return;
        }

        if (NetworkManager.IsClient)
        {
            ServerCommands.CastToUnit cunitCmd;
            cunitCmd.TagFrom = unit.Tag;
            cunitCmd.SpellID = (int)cspell.SpellID;
            cunitCmd.TagTo = target.Tag;
            cunitCmd.ItemID = cspell.Item != null ? cspell.Item.Class.ItemID : 0;
            ClientManager.SendCommand(cunitCmd);
        }
        else
        {
            if (MapLogic.Instance.ConsolePlayer == unit.Player)
            {
                unit.SetState(new CastState(unit, cspell, target));
            }
        }
    }

    public static void SendCastToArea(MapUnit unit, Spell cspell, int x, int y)
    {
        if (cspell == null)
            return;

        if (NetworkManager.IsClient)
        {
            ServerCommands.CastToArea careaCmd;
            careaCmd.TagFrom = unit.Tag;
            careaCmd.SpellID = (int)cspell.SpellID;
            careaCmd.TargetX = x;
            careaCmd.TargetY = y;
            careaCmd.ItemID = cspell.Item != null ? cspell.Item.Class.ItemID : 0;
            ClientManager.SendCommand(careaCmd);
        }
        else
        {
            if (MapLogic.Instance.ConsolePlayer == unit.Player)
            {
                unit.SetState(new CastState(unit, cspell, x, y));
            }
        }
    }

    public static void SendRespawn()
    {
        if (NetworkManager.IsClient)
        {
            ServerCommands.RespawnAvatar respCmd;
            ClientManager.SendCommand(respCmd);
        }
        else
        {
            if (MapLogic.Instance.ConsolePlayer != null &&
                MapLogic.Instance.ConsolePlayer.Avatar != null &&
                !MapLogic.Instance.ConsolePlayer.Avatar.IsAlive)
            {
                MapLogic.Instance.ConsolePlayer.Avatar.Respawn(16, 16);
            }
        }
    }

    public static void SendItemMove(ServerCommands.ItemMoveLocation from, ServerCommands.ItemMoveLocation to,
                                    int fromIndex, int toIndex, int count,
                                    MapUnit currentUnit,
                                    int cellX, int cellY)
    {
        if (NetworkManager.IsClient)
        {
            /*Debug.LogFormat("from = {0}, to = {1}, fromIndex = {2}, toIndex = {3}, count = {4}",
                from, to, fromIndex, toIndex, count);*/
            Debug.LogFormat("send item move from {0}/{1} to {2}/{3}", from, fromIndex, to, toIndex);
            ServerCommands.ItemMove imvCmd;
            imvCmd.Source = from;
            imvCmd.SourceIndex = fromIndex;
            imvCmd.Destination = to;
            imvCmd.DestinationIndex = toIndex;
            imvCmd.Count = count;
            imvCmd.UnitTag = (currentUnit != null) ? (currentUnit.Tag) : -1;
            imvCmd.CellX = cellX;
            imvCmd.CellY = cellY;
            ClientManager.SendCommand(imvCmd);
        }
    }

    public static void DropItem(MapHuman human, Item item, int x, int y)
    {
        if (NetworkManager.IsClient)
        {
            // check what this item is.
            if (item.Parent == human.ItemsBody)
            {
                SendItemMove(ServerCommands.ItemMoveLocation.UnitBody,
                    ServerCommands.ItemMoveLocation.Ground,
                    item.Class.Option.Slot, -1, item.Count, human, x, y);
            }
            else if (item.Parent == human.ItemsPack)
            {
                SendItemMove(ServerCommands.ItemMoveLocation.UnitPack,
                    ServerCommands.ItemMoveLocation.Ground,
                    item.Index, -1, item.Count, human, x, y);
            }
        }
        else
        {
            // check coordinates
            if (Math.Abs(human.X - x) > 2 ||
                Math.Abs(human.Y - y) > 2)
            {
                x = human.X;
                y = human.Y;
            }

            // check if we are dropping gold
            ItemPack pack = new ItemPack();
            if (item.IsMoney)
            {
                pack.Money = item.Price;
            }
            else
            {
                pack.PutItem(0, new Item(item, item.Count));
            }
            MapLogic.Instance.PutSackAt(x, y, pack, false);
        }
    }
}