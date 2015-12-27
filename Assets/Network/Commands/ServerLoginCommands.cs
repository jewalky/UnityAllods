using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.IO;

/// <summary>
/// On login, clients send ClientAuth.
/// The server then responds either with ClientCommands.Error or with ClientCommands.SwitchMap.
/// 
/// MAP DOWNLOAD:
/// If the client doesn't have the map that server has, it replies with RequestDownloadStart.
/// Server then sends ClientCommands.DownloadStart.
/// After this, the client should send series of RequestDownloadContinue to each of which a ClientCommands.DownloadContinue will be sent.
/// Once the map is fully downloaded, the client saves it locally and re-sends ClientAuth.
/// 
/// If the client has the map, it should send RequestGamestate command.
/// Here, the server will send him main game info like the current players and some limited map object data.
/// </summary>

namespace ServerCommands
{
    [Serializable()]
    public struct ClientAuth : IServerCommand
    {
        // nothing here for now. will possibly put network version or smth later. (or hat-related stuff)
        public bool Process(ServerClient client)
        {
            // send client the current map, or tell him to go away
            if (MapLogic.Instance.IsLoaded)
            {
                if (MapLogic.Instance.GetNetPlayerCount() >= MapLogic.MaxPlayers)
                {
                    client.State = ClientState.Disconnected;
                    ClientCommands.Error errCmd;
                    errCmd.Code = ClientCommands.Error.ErrorCode.ServerFull;
                    client.SendCommand(errCmd);
                    return true;
                }

                client.State = ClientState.Connected;
                ClientCommands.SwitchMap mapCmd;
                mapCmd.FileName = MapLogic.Instance.FileName;
                mapCmd.MD5 = MapLogic.Instance.FileMD5;
                client.SendCommand(mapCmd);
                return true;
            }
            else
            {
                client.State = ClientState.Disconnected;
                ClientCommands.Error errCmd;
                errCmd.Code = ClientCommands.Error.ErrorCode.MapNotLoaded;
                client.SendCommand(errCmd);
                return true;
            }
        }
    }

    [Serializable()]
    public struct RequestDownloadStart : IServerCommand
    {
        public bool Process(ServerClient client)
        {
            if (client.State != ClientState.Connected)
                return false;

            client.State = ClientState.DownloadingMap;
            client.Dl_Name = MapLogic.Instance.FileName;
            using (MemoryStream ms = ResourceManager.OpenRead(client.Dl_Name))
            {
                client.Dl_Bytes = new byte[ms.Length];
                ms.Read(client.Dl_Bytes, 0, (int)ms.Length);
            }
            client.Dl_Done = 0;
            // send client the map
            ClientCommands.DownloadStart dlStart;
            dlStart.TotalSize = client.Dl_Bytes.Length;
            client.SendCommand(dlStart);
            return true;
        }
    }

    [Serializable()]
    public struct RequestDownloadContinue : IServerCommand
    {
        public bool Process(ServerClient client)
        {
            if (client.State != ClientState.DownloadingMap)
                return false;

            int curLeft = (int)Mathf.Min(client.Dl_Bytes.Length - client.Dl_Done, 1024);
            ClientCommands.DownloadContinue dlCmd;
            dlCmd.PartialBytes = client.Dl_Bytes.Skip(client.Dl_Done).Take(curLeft).ToArray();
            client.Dl_Done += curLeft;
            client.SendCommand(dlCmd);
            if (client.Dl_Done == client.Dl_Bytes.Length)
                client.State = ClientState.DownloadedMap;
            return true;
        }
    }

    [Serializable()]
    public struct RequestGamestate : IServerCommand
    {
        public bool Process(ServerClient client)
        {
            if (client.State != ClientState.Connected)
                return false;

            client.State = ClientState.Playing;
            // give this client a MapLogicPlayer instance.
            if (MapLogic.Instance.GetNetPlayer(client) != null) // there's already a player attached to this client. i.e. player is trying to login twice.
                return false;
            MapLogicPlayer player = new MapLogicPlayer(client);
            MapLogic.Instance.AddNetPlayer(player, false); // I don't really care here, silent affects only clients
            // basic setup
            player.Money = 1000;
            // notify the client of all players in the game.
            foreach (MapLogicPlayer notifyplayer in MapLogic.Instance.Players)
            {
                if ((notifyplayer.Flags & MapLogicPlayerFlags.NetClient) == 0) // only send netclients! all other players are loaded from alm on client side
                    continue;

                ClientCommands.AddPlayer plCmd;
                plCmd.ID = notifyplayer.ID;
                plCmd.Color = notifyplayer.Color;
                plCmd.Name = notifyplayer.Name;
                plCmd.Money = notifyplayer.Money;
                plCmd.Diplomacy = notifyplayer.Diplomacy;
                plCmd.Silent = true;
                plCmd.ConsolePlayer = (notifyplayer == player);
                client.SendCommand(plCmd);
            }
            return true;
        }
    }
}