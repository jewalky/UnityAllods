using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
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
}