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
        // also kill MapDownloader
        if (MapDownloader.Instance != null)
            MapDownloader.Instance.Kill();
        // also unload map, because if we were connected, we had server map.
        MapLogic.Instance.Unload();
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
                MapDownloader dlHandler = Utils.CreateObjectWithScript<MapDownloader>();
                dlHandler.Setup(FileName);
                dlHandler.transform.parent = UiManager.Instance.transform;
                Server.RequestDownloadCommand reqDlCmd;
                ClientManager.SendCommand(reqDlCmd);
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

    [Serializable()]
    public struct DownloadStartCommand : IClientCommand
    {
        public int TotalSize;

        public bool Process()
        {
            GameConsole.Instance.WriteLine("Downloading map from server ({0} bytes to download)...", TotalSize);
            MapDownloader.Instance.Dl_FullSize = TotalSize;
            MapDownloader.Instance.Dl_Content = new byte[TotalSize];
            Server.RequestDownloadContinueCommand dlCntCmd;
            ClientManager.SendCommand(dlCntCmd);
            return true;
        }
    }

    [Serializable()]
    public struct DownloadCommand : IClientCommand
    {
        public byte[] PartialBytes;

        public bool Process()
        {
            PartialBytes.CopyTo(MapDownloader.Instance.Dl_Content, MapDownloader.Instance.Dl_DoneSize);
            MapDownloader.Instance.Dl_DoneSize += PartialBytes.Length;
            Server.RequestDownloadContinueCommand dlCntCmd;
            if (MapDownloader.Instance.Dl_DoneSize == MapDownloader.Instance.Dl_FullSize)
            {
                // save map file, and retry authentication
                if (!Directory.Exists("maps"))
                {
                    try
                    {
                        DirectoryInfo info = Directory.CreateDirectory("maps");
                    }
                    catch (IOException)
                    {
                        GameConsole.Instance.WriteLine("Error: unable to write map file into \"maps\".");
                        NetworkManager.Instance.Disconnect();
                    }
                }

                try
                {
                    using (FileStream fs = File.OpenWrite("maps/" + MapDownloader.Instance.FileName))
                        fs.Write(MapDownloader.Instance.Dl_Content, 0, MapDownloader.Instance.Dl_FullSize);
                    GameConsole.Instance.WriteLine("Wrote the new map into \"maps/{0}\".", MapDownloader.Instance.FileName);
                    Server.ClientAuthCommand authCmd;
                    ClientManager.SendCommand(authCmd);
                }
                catch(IOException)
                {
                    GameConsole.Instance.WriteLine("Error: unable to write map file into \"maps\".");
                    NetworkManager.Instance.Disconnect();
                }

                GameObject.Destroy(MapDownloader.Instance.gameObject);
            }
            else ClientManager.SendCommand(dlCntCmd); // next part
            return true;
        }
    }
}