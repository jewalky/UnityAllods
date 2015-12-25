using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public interface IClientCommand
{
    bool Process();
}

public class Client
{
    public static void ConnectedToServer()
    {
        GameConsole.Instance.WriteLine("Connected to [{0}]:{1}!", ClientManager.ServerIPAddress, ClientManager.ServerIPPort);
    }

    public static void DisconnectedFromServer()
    {
        GameConsole.Instance.WriteLine("Disconnected from [{0}]:{1}.", ClientManager.ServerIPAddress, ClientManager.ServerIPPort);

    }
}