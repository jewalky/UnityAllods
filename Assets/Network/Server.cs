using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;

public interface IServerCommand
{
    bool Process(ServerClient client);
}

public class Server
{
    public static void ClientConnected(ServerClient cl)
    {
        GameConsole.Instance.WriteLine("Client {0}:{1} has connected.", cl.IPAddress, cl.IPPort);
    }

    public static void ClientDisconnected(ServerClient cl)
    {
        GameConsole.Instance.WriteLine("Client {0}:{1} has disconnected.", cl.IPAddress, cl.IPPort);
        // right now, we don't have any special handling for offline (yet present) players. that will only be implemented when we'll have a master server (hat).
        // so when a player disconnects, we immediately kick him out without giving a chance to reconnect flawlessly.
        if (MapLogic.Instance.IsLoaded)
        {
            Player player = MapLogic.Instance.GetNetPlayer(cl);
            if (player != null)
                MapLogic.Instance.DelNetPlayer(player, false, false);
        }
    }

    public static void DisconnectPlayer(Player player)
    {
        if (player.NetClient == null)
            return;
        player.NetClient.Disconnect();
    }

    public static void KickPlayer(Player player)
    {
        if (player.NetClient == null)
            return;
        DisconnectPlayer(player);
        MapLogic.Instance.DelNetPlayer(player, false, true);
    }

    public static void NotifyPlayerJoined(Player player)
    {
        foreach (ServerClient client in ServerManager.Clients)
        {
            if (client.State != ClientState.Playing)
                continue;
            if (client == player.NetClient) // don't send own info, this is done separately.
                continue;

            ClientCommands.AddPlayer plCmd;
            plCmd.ID = player.ID;
            plCmd.Color = player.Color;
            plCmd.Name = player.Name;
            plCmd.Money = player.Money;
            plCmd.Diplomacy = player.Diplomacy;
            plCmd.Silent = false;
            plCmd.ConsolePlayer = false;
            client.SendCommand(plCmd);
        }
    }

    public static void NotifyPlayerLeft(Player player, bool kicked)
    {
        foreach (ServerClient client in ServerManager.Clients)
        {
            if (client.State != ClientState.Playing)
                continue;
            if (client == player.NetClient) // don't send own info, this is done separately.
                continue;

            ClientCommands.DelPlayer plCmd;
            plCmd.ID = player.ID;
            plCmd.Kick = kicked;
            plCmd.Silent = false;
            client.SendCommand(plCmd);
        }
    }

    public static void NotifyChatMessage(Player player, string message)
    {
        foreach (ServerClient client in ServerManager.Clients)
        {
            if (client.State != ClientState.Playing)
                continue;

            ClientCommands.ChatMessage chatCmd;
            chatCmd.PlayerID = (player != null) ? player.ID : -1;
            chatCmd.Text = message;
            client.SendCommand(chatCmd);
        }
    }

    // clients will see anything sent with this function as a "<server>: blablabla" in their chat.
    public static void SendChatMessage(string text)
    {
        // local chat presentation.
        int color = Player.AllColorsSystem;
        string actualText = "<server>: " + text;
        MapViewChat.Instance.AddChatMessage(color, actualText);

        NotifyChatMessage(null, text);
    }

    public static void NotifySpeedChanged(int newSpeed)
    {
        foreach (ServerClient client in ServerManager.Clients)
        {
            if (client.State != ClientState.Playing)
                continue;

            ClientCommands.SpeedChanged speedCmd;
            speedCmd.NewSpeed = newSpeed;
            client.SendCommand(speedCmd);
        }
    }

    public static void ObjectBecameVisible(Player player, MapObject mobj)
    {
        //Debug.LogFormat("visible = {0}->{1}", player.Name, mobj.GetObjectType().ToString());
        if (mobj.GetObjectType() == MapObjectType.Monster ||
            mobj.GetObjectType() == MapObjectType.Human)
        {
            MapUnit unit = (MapUnit)mobj;
            ClientCommands.AddUnit unitCmd;
            unitCmd.Tag = unit.Tag;
            unitCmd.X = unit.X;
            unitCmd.Y = unit.Y;
            unitCmd.Angle = unit.Angle;
            unitCmd.Player = unit.Player.ID;
            unitCmd.ServerID = unit.Template.ServerID;
            unitCmd.CurrentStats = unit.Stats;
            unitCmd.IsAvatar = (unit == unit.Player.Avatar);
            player.NetClient.SendCommand(unitCmd);
            Debug.LogFormat("sending player {0} unit {1}", player.Name, unitCmd.Tag);
        }
    }

    public static void ObjectBecameInvisible(Player player, MapObject mobj)
    {
        //Debug.LogFormat("invisible = {0}->{1}", player.Name, mobj.GetObjectType().ToString());
    }

    public static void NotifyDelObject(MapObject mobj)
    {
        foreach (ServerClient client in ServerManager.Clients)
        {
            if (client.State != ClientState.Playing)
                continue;

            Player p = MapLogic.Instance.GetNetPlayer(client);
            if (mobj.IsVisibleForNetPlayer(p))
            {
                if (mobj.GetObjectType() == MapObjectType.Monster ||
                    mobj.GetObjectType() == MapObjectType.Human)
                {
                    MapUnit unit = (MapUnit)mobj;
                    ClientCommands.DelUnit dunitCmd;
                    dunitCmd.Tag = unit.Tag;
                    client.SendCommand(dunitCmd);
                }
            }
        }
    }

    public static void NotifyMoveUnit(MapUnit unit, int x1, int y1, int x2, int y2, int angle1, int angle2)
    {
        foreach (ServerClient client in ServerManager.Clients)
        {
            if (client.State != ClientState.Playing)
                continue;

            Player p = MapLogic.Instance.GetNetPlayer(client);
            if (unit.IsVisibleForNetPlayer(p))
            {
                ClientCommands.WalkUnit walkCmd;
                walkCmd.Tag = unit.Tag;
                walkCmd.X1 = x1;
                walkCmd.X2 = x2;
                walkCmd.Y1 = y1;
                walkCmd.Y2 = y2;
                walkCmd.StartAngle = angle1;
                walkCmd.EndAngle = angle2;
                client.SendCommand(walkCmd);
            }
        }
    }

    public static void NotifyIdleUnit(MapUnit unit, int x, int y, int angle)
    {
        foreach (ServerClient client in ServerManager.Clients)
        {
            if (client.State != ClientState.Playing)
                continue;

            Player p = MapLogic.Instance.GetNetPlayer(client);
            if (unit.IsVisibleForNetPlayer(p))
            {
                ClientCommands.IdleUnit idleCmd;
                idleCmd.Tag = unit.Tag;
                idleCmd.X = x;
                idleCmd.Y = y;
                idleCmd.Angle = angle;
                client.SendCommand(idleCmd);
            }
        }
    }
}