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
}