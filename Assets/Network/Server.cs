using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
}