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
        GameConsole.Instance.WriteLine("Client [{0}]:{1} has connected.", cl.IPAddress, cl.IPPort);
    }

    public static void ClientDisconnected(ServerClient cl)
    {
        GameConsole.Instance.WriteLine("Client [{0}]:{1} has disconnected.", cl.IPAddress, cl.IPPort);
        // right now, we don't have any special handling for offline (yet present) players. that will only be implemented when we'll have a master server (hat).
        // so when a player disconnects, we immediately kick him out without giving a chance to reconnect flawlessly.
        if (MapLogic.Instance.IsLoaded)
        {
            MapLogicPlayer player = MapLogic.Instance.GetNetPlayer(cl);
            if (player != null)
                MapLogic.Instance.DelNetPlayer(player, false, false);
        }
    }

    public static void DisconnectPlayer(MapLogicPlayer player)
    {
        if (player.NetClient == null)
            return;
        player.NetClient.Disconnect();
    }

    public static void KickPlayer(MapLogicPlayer player)
    {
        if (player.NetClient == null)
            return;
        DisconnectPlayer(player);
        MapLogic.Instance.DelNetPlayer(player, false, true);
    }

    public static void NotifyPlayerJoined(MapLogicPlayer player)
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

    public static void NotifyPlayerLeft(MapLogicPlayer player, bool kicked)
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

    public static void NotifyChatMessage(MapLogicPlayer player, string message)
    {
        foreach (ServerClient client in ServerManager.Clients)
        {
            if (client.State != ClientState.Playing)
                continue;

            ClientCommands.ChatMessage chatCmd;
            chatCmd.PlayerID = player.ID;
            chatCmd.Text = message;
            client.SendCommand(chatCmd);
        }
    }

    public static void ObjectBecameVisible(MapLogicPlayer player, MapLogicObject mobj)
    {

    }

    public static void ObjectBecameInvisible(MapLogicPlayer player, MapLogicObject mobj)
    {

    }
}