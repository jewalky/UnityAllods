using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

public enum ClientState
{
    Disconnected,
    ConnectWait,
    Connected,
    DownloadingMap,
    DownloadedMap,
    Playing
}

public class Client
{
    // our connection to the server
    public static string ServerIPAddress { get; private set; }
    public static ushort ServerIPPort { get; private set; }
    public static int ConnectionID { get; private set; }

    public static bool Init(string host, ushort port)
    {
        GameConsole.Instance.WriteLine("Connecting to {0}:{1}...", host, port);
        byte error;
        ConnectionID = NetworkTransport.Connect(NetworkManager.Instance.HostID, host, port, 0, out error);
        if (error != (byte)NetworkError.Ok)
        {
            GameConsole.Instance.WriteLine("Failed (Error: {0})", ((NetworkError)error).ToString());
            return false;
        }
        return true;
    }

    public static void Shutdown()
    {
        byte error;
        NetworkTransport.Disconnect(NetworkManager.Instance.HostID, ConnectionID, out error); // ignore unload errors
        OnDisconnected();
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
                if (RecConnectionID == ConnectionID)
                    OnConnected();
                break;
            case NetworkEventType.DisconnectEvent:
                //Debug.Log(string.Format("{0} = {1}, {2}, {3}", recData, RecHostID, RecConnectionID, RecChannelID));
                if (RecConnectionID == ConnectionID)
                    OnDisconnected();
                break;
            case NetworkEventType.DataEvent:
                //Debug.Log(string.Format("{0} = {1}, {2}, {3}", recData, RecHostID, RecConnectionID, RecChannelID));
                if (RecConnectionID == ConnectionID)
                    OnPacketReceived(RecBuffer.Take(DataSize).ToArray());
                break;
        }
    }

    public static void OnConnected()
    {
        byte error;
        int port;
        ulong _network;
        ushort _dstNode;
        string ip = NetworkTransport.GetConnectionInfo(NetworkManager.Instance.HostID, ConnectionID, out port, out _network, out _dstNode, out error);
        if (error != (byte)NetworkError.Ok)
        {
            ServerIPAddress = "<error>";
            ServerIPPort = 0;
            return;
        }

        ServerIPAddress = ip;
        ServerIPPort = (ushort)port;

        GameConsole.Instance.WriteLine("Connected to [{0}]:{1}!", ServerIPAddress, ServerIPPort);
    }

    public static void OnDisconnected()
    {
        GameConsole.Instance.WriteLine("Disconnected from [{0}]:{1}.", ServerIPAddress, ServerIPPort);
    }

    public static void OnPacketReceived(byte[] packet)
    {
        // unserialize command
        MemoryStream ms = new MemoryStream(packet);
        try
        {
            BinaryFormatter bf = new BinaryFormatter();
            object o = bf.Deserialize(ms);
            if (!(o is IClientCommand))
            {
                GameConsole.Instance.WriteLine("Client commands should implement IClientCommand.");
                NetworkManager.Instance.Disconnect();
                return;
            }

            if (!((IClientCommand)o).Process())
                NetworkManager.Instance.Disconnect();
        }
        catch(Exception)
        {
            GameConsole.Instance.WriteLine("Error encountered during command processing.");
            NetworkManager.Instance.Disconnect();
        }
        finally
        {
            ms.Close();
        }
    }

    public static bool SendPacket(byte[] packet)
    {
        byte error;
        NetworkTransport.Send(NetworkManager.Instance.HostID, ConnectionID, NetworkManager.Instance.ReliableChannel, packet, packet.Length, out error);
        if (error != (byte)NetworkError.Ok)
            return false;
        return true;
    }

    public static bool SendCommand(object o)
    {
        MemoryStream ms = new MemoryStream();
        try
        {
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(ms, o);
            return SendPacket(ms.GetBuffer());
        }
        catch (Exception)
        {
            return false;
        }
        finally
        {
            ms.Close();
        }
    }
}