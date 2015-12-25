using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Reflection;

public class ServerClient
{
    internal int ConnectionID;
    public string IPAddress { get; private set; }
    public ushort IPPort { get; private set; }
    public ServerClient(int connectionId)
    {
        ConnectionID = connectionId;

        byte error;
        int port;
        ulong _network;
        ushort _dstNode;
        string ip = NetworkTransport.GetConnectionInfo(NetworkManager.Instance.HostID, connectionId, out port, out _network, out _dstNode, out error);
        if (error != (byte)NetworkError.Ok)
        {
            IPAddress = "<error>";
            IPPort = 0;
            return;
        }

        IPAddress = ip;
        IPPort = (ushort)port;
    }

    public void OnConnected()
    {
        GameConsole.Instance.WriteLine("Client [{0}]:{1} has connected.", IPAddress, IPPort);
        ClientCommands.TestCommand tc = new ClientCommands.TestCommand();
        tc.TestString = "hi this is a string! :D";
        SendCommand(tc);
    }

    public void OnDisconnected()
    {
        GameConsole.Instance.WriteLine("Client [{0}]:{1} has disconnected.", IPAddress, IPPort);
    }

    public void OnPacketReceived(byte[] packet)
    {
        // unserialize command
        MemoryStream ms = new MemoryStream(packet);
        try
        {
            BinaryFormatter bf = new BinaryFormatter();
            object o = bf.Deserialize(ms);
            // search for handler of this command, in ClientCommands class
            string requiredName = o.GetType().Name;
            MethodInfo mi = typeof(ServerCommands).GetMethod("On" + requiredName, new Type[] { o.GetType() });
            if (!mi.IsStatic)
            {
                GameConsole.Instance.WriteLine("Error: command handlers should be static.");
                return;
            }
            mi.Invoke(null, new object[] { o });
        }
        catch (Exception)
        {
            GameConsole.Instance.WriteLine("Error encountered during command processing.");
            NetworkManager.Instance.Disconnect();
        }
        finally
        {
            ms.Close();
        }
    }

    public bool SendPacket(byte[] packet)
    {
        byte error;
        NetworkTransport.Send(NetworkManager.Instance.HostID, ConnectionID, NetworkManager.Instance.ReliableChannel, packet, packet.Length, out error);
        if (error != (byte)NetworkError.Ok)
            return false;
        return true;
    }

    public bool SendCommand(object o)
    {
        MemoryStream ms = new MemoryStream();
        try
        {
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(ms, o);
            return SendPacket(ms.GetBuffer());
        }
        catch(Exception)
        {
            return false;
        }
        finally
        {
            ms.Close();
        }
    }

    //////// todo implement packet handling below
    ////////


}

public class Server
{
    private static List<ServerClient> Clients = new List<ServerClient>();

    public static bool Init(ushort port)
    {
        return true;
    }

    public static void Shutdown()
    {

    }

    private static byte[] RecBuffer = new byte[4096];
    public static void Update()
    {
        int RecHostID;
        int RecConnectionID;
        int RecChannelID;
        int DataSize;
        byte Error;
        NetworkEventType recData = NetworkTransport.Receive(out RecHostID, out RecConnectionID, out RecChannelID, RecBuffer, RecBuffer.Length, out DataSize, out Error);
        switch (recData)
        {
            case NetworkEventType.ConnectEvent:
                //Debug.Log(string.Format("{0} = {1}, {2}, {3}", recData, RecHostID, RecConnectionID, RecChannelID));
                if (RecConnectionID != NetworkManager.Instance.ConnectionID)
                {
                    ServerClient client = new ServerClient(RecConnectionID);
                    client.OnConnected();
                    Clients.Add(client);
                }
                break;
            case NetworkEventType.DisconnectEvent:
                //Debug.Log(string.Format("{0} = {1}, {2}, {3}", recData, RecHostID, RecConnectionID, RecChannelID));
                foreach (ServerClient client in Clients)
                {
                    if (client.ConnectionID == RecConnectionID)
                    {
                        client.OnDisconnected();
                        break;
                    }
                }
                break;
            case NetworkEventType.DataEvent:
                //Debug.Log(string.Format("{0} = {1}, {2}, {3}", recData, RecHostID, RecConnectionID, RecChannelID));
                foreach (ServerClient client in Clients)
                {
                    if (client.ConnectionID == RecConnectionID)
                    {
                        client.OnPacketReceived(RecBuffer.Take(DataSize).ToArray());
                        break;
                    }
                }
                break;
        }
    }
}