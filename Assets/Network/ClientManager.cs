using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Net.Sockets;
using System.Threading;
using ProtoBuf;

public class ClientManager
{
    // our connection to the server
    public static string ServerIPAddress { get; private set; }
    public static ushort ServerIPPort { get; private set; }
    public static TcpClient Connection { get; private set; }

    private static bool DidConnect = false;
    private static bool DidFailConnect = false;
    private static bool DoDisconnectMe = false;
    private static Thread ClientThread;
    private static Thread ClientThreadSend;
    private static List<byte[]> ConnectionPackets = new List<byte[]>();
    private static List<byte[]> ConnectionPacketsToSend = new List<byte[]>();
    private static void ClientThreadProc(TcpClient connection, string host, int port)
    {
        try
        {
            connection.Connect(host, port);
        }
        catch(Exception)
        {
            // failed. report somehow.
        }

        connection.Client.Blocking = true;
        connection.Client.ReceiveBufferSize = 1048576; // 1mb

        ClientThreadSend = new Thread(new ThreadStart(() => { ClientThreadSendProc(Connection); }));
        ClientThreadSend.Start(); // start packet sending thread once we're connected

        while (true)
        {
            Thread.Sleep(1);
            byte[] packet = NetworkManager.DoReadPacketFromStream(connection.Client);
            if (packet == null)
            {
                DoDisconnectMe = true;
                break;
            }

            lock(ConnectionPackets)
                ConnectionPackets.Add(packet);
        }
    }

    private static void ClientThreadSendProc(TcpClient connection)
    {
        Connection.Client.Blocking = true;
        connection.Client.SendBufferSize = 1048576; // 1mb

        while (true)
        {
            lock (ConnectionPacketsToSend)
            {
                foreach (byte[] packet in ConnectionPacketsToSend)
                {
                    if (!NetworkManager.DoWritePacketToStream(connection.Client, packet))
                    {
                        DoDisconnectMe = true;
                        break;
                    }
                }

                ConnectionPacketsToSend.Clear();
            }

            if (DoDisconnectMe)
                break;

            Thread.Sleep(1);
        }
    }

    public static bool Init(string host, ushort port)
    {
        Shutdown(false);
        DidConnect = false;
        DidFailConnect = false;
        DoDisconnectMe = false;
        ConnectionPackets.Clear();
        ConnectionPacketsToSend.Clear();
        GameConsole.Instance.WriteLine("Connecting to {0}:{1}...", host, port);
        ServerIPAddress = host;
        ServerIPPort = port;
        Connection = new TcpClient();
        // how init packet stuff
        ClientThread = new Thread(new ThreadStart(() => { ClientThreadProc(Connection, host, port); }));
        ClientThread.Start();
        return true;
    }

    public static void Shutdown(bool force)
    {
        if (force)
        {
            if (ClientThread != null)
                ClientThread.Abort();
            ClientThread = null;
            if (ClientThreadSend != null)
                ClientThreadSend.Abort();
            ClientThreadSend = null;
        }
        if (Connection != null)
            Connection.Close();
        Connection = null;
    }

    public static void Update()
    {
        if (NetworkManager.IsClient)
        {
            lock (ConnectionPackets)
            {
                foreach (byte[] packet in ConnectionPackets)
                    OnPacketReceived(packet);
                ConnectionPackets.Clear();
            }

            if (!DidConnect && (Connection != null && Connection.Connected))
            {
                OnConnected();
                DidConnect = true;
            }
        }

        if (DoDisconnectMe)
        {
            OnDisconnected();
            Shutdown(false);
            DoDisconnectMe = false;
        }
    }

    public static void OnConnected()
    {
        Client.ConnectedToServer();
    }

    public static void OnDisconnected()
    {
        Client.DisconnectedFromServer();
    }

    public static void OnPacketReceived(byte[] packet)
    {
        // unserialize command
        MemoryStream ms = new MemoryStream(packet);
        try
        {
            // get class identifier.
            BinaryReader br = new BinaryReader(ms);
            byte pid = br.ReadByte();
            Type objType = NetworkManager.FindTypeFromPacketId("ClientCommands", pid);
            if (objType == null)
            {
                GameConsole.Instance.WriteLine("Unknown command ID={0:X2}.", pid);
                NetworkManager.Instance.Disconnect();
                return;
            }

            object o = Serializer.Deserialize(objType, ms);
            if (!(o is IClientCommand))
            {
                GameConsole.Instance.WriteLine("Client commands should implement IClientCommand.");
                NetworkManager.Instance.Disconnect();
                return;
            }

            //Debug.LogFormat("received packet = {0}\n{1}", o.GetType().Name, o.ToString());

            if (!((IClientCommand)o).Process())
                NetworkManager.Instance.Disconnect();
        }
        catch(Exception e)
        {
            GameConsole.Instance.WriteLine("Error encountered during command processing.\n{0}", e.ToString());
            Debug.LogFormat("Error encountered during command processing.\n{0}", e.ToString());
            NetworkManager.Instance.Disconnect();
        }
        finally
        {
            ms.Close();
        }
    }

    private static bool SendPacket(byte[] packet)
    {
        lock (ConnectionPacketsToSend)
            ConnectionPacketsToSend.Add(packet);
        return true;
    }

    public static bool SendCommand<T>(T o) where T : IServerCommand
    {
        MemoryStream ms = new MemoryStream();
        try
        {
            BinaryWriter writer = new BinaryWriter(ms);
            // write packet ID. currently a byte.
            NetworkPacketId[] npi = (NetworkPacketId[])o.GetType().GetCustomAttributes(typeof(NetworkPacketId), false);
            if (npi.Length != 0)
            {
                writer.Write(npi[0].PacketID);
                Serializer.Serialize(ms, o);
                return SendPacket(ms.ToArray());
            }

            Debug.LogFormat("ERROR: Can't send commands without ID! (type = {0})", o.GetType().Name);
            return false;
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