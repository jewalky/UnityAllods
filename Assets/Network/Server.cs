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
    }

    [Serializable()]
    public struct ClientAuthCommand : IServerCommand
    {
        // nothing here for now. will possibly put network version or smth later. (or hat-related stuff)
        public bool Process(ServerClient client)
        {
            // send client the current map, or tell him to go away if the map isn't loaded ATM
            if (MapLogic.Instance.IsLoaded)
            {
                Client.SwitchMapCommand mapCmd;
                mapCmd.FileName = MapLogic.Instance.FileName;
                mapCmd.MD5 = MapLogic.Instance.FileMD5;
                client.SendCommand(mapCmd);
                client.State = ClientState.Playing;
                return true;
            }
            else
            {
                Client.ErrorCommand errCmd;
                errCmd.Code = Client.ErrorCommand.ErrorCode.MapNotLoaded;
                client.SendCommand(errCmd);
                return true;
            }
        }
    }

    [Serializable()]
    public struct RequestGamestateCommand : IServerCommand
    {
        public bool Process(ServerClient client)
        {
            return true;
        }
    }

    [Serializable()]
    public struct RequestDownloadCommand : IServerCommand
    {
        public bool Process(ServerClient client)
        {
            try
            {
                client.State = ClientState.DownloadingMap;
                client.Dl_Name = MapLogic.Instance.FileName;
                using (MemoryStream ms = ResourceManager.OpenRead(client.Dl_Name))
                {
                    client.Dl_Bytes = new byte[ms.Length];
                    ms.Read(client.Dl_Bytes, 0, (int)ms.Length);
                }
                client.Dl_Done = 0;
                // send client the map
                Client.DownloadStartCommand dlStart;
                dlStart.TotalSize = client.Dl_Bytes.Length;
                client.SendCommand(dlStart);
            }
            catch(Exception e)
            {
                GameConsole.Instance.WriteLine(e.ToString());
            }

            return true;
        }
    }

    [Serializable()]
    public struct RequestDownloadContinueCommand : IServerCommand
    {
        public bool Process(ServerClient client)
        {
            Debug.Log(string.Format("client asked for next piece. dl_Done = {0}, dl_Total = {1}", client.Dl_Done, client.Dl_Bytes.Length));
            int curLeft = (int)Mathf.Min(client.Dl_Bytes.Length - client.Dl_Done, 512);
            Client.DownloadCommand dlCmd;
            dlCmd.PartialBytes = client.Dl_Bytes.Skip(client.Dl_Done).Take(curLeft).ToArray();
            client.Dl_Done += curLeft;
            client.SendCommand(dlCmd);
            if (client.Dl_Done == client.Dl_Bytes.Length)
                client.State = ClientState.DownloadedMap;
            return true;
        }
    }
}