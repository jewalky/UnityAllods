using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

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
    public static ClientState State { get; private set; }

    public static void ConnectedToServer()
    {
        GameConsole.Instance.WriteLine("Connected to [{0}]:{1}!", ClientManager.ServerIPAddress, ClientManager.ServerIPPort);
        State = ClientState.Connected;

        // unload local map and tell the server that we're going to play.
        if (MapLogic.Instance.IsLoaded)
            MapLogic.Instance.Unload();
        Server.ClientAuthCommand authCmd;
        ClientManager.SendCommand(authCmd);
    }

    public static void DisconnectedFromServer()
    {
        GameConsole.Instance.WriteLine("Disconnected from [{0}]:{1}.", ClientManager.ServerIPAddress, ClientManager.ServerIPPort);
    }

    [Serializable()]
    public struct SwitchMapCommand : IClientCommand
    {
        public string FileName;
        public string MD5;

        public bool Process()
        {
            // similar logic to "map" console command, except no .alm autocompletion because server should send it
            string baseFilename = Path.GetFileName(FileName);
            string actualFilename = null;
            if (ResourceManager.FileExists(FileName))
                actualFilename = FileName;
            else if (ResourceManager.FileExists(baseFilename))
                actualFilename = baseFilename;
            else if (ResourceManager.FileExists("maps/" + baseFilename))
                actualFilename = "maps/" + baseFilename;
            string actualMD5 = (actualFilename != null) ? ResourceManager.CalcMD5(actualFilename) : null;
            if (actualMD5 != null)
                GameConsole.Instance.WriteLine("Server is using map \"{0}\" (hash {1}).\nLocal file found: {2} (hash {3}).", FileName, MD5, actualFilename, actualMD5);
            else GameConsole.Instance.WriteLine("Server is using map \"{0}\" (hash {1}).\nLocal file NOT found.", FileName, MD5, actualFilename, actualMD5);
            if (actualMD5 != MD5) // including null case I guess
            {
                // enter map download state locally.
                //
                State = ClientState.DownloadingMap;

            }
            else
            {
                // possibly execute additional init sequences here
                State = ClientState.Playing;
                Server.RequestGamestateCommand reqCmd;
                ClientManager.SendCommand(reqCmd);
                MapView.Instance.InitFromFile(actualFilename);
            }
            return true;
        }
    }

    [Serializable()]
    public struct ErrorCommand : IClientCommand
    {
        public enum ErrorCode
        {
            MapNotLoaded
        }
        public ErrorCode Code;

        public bool Process()
        {
            GameConsole.Instance.WriteLine("Error: {0}", Code.ToString());
            return false;
        }
    }
}