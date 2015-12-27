using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Reflection;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Threading;

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

        ClientThreadSend = new Thread(new ThreadStart(() => { ClientThreadSendProc(Connection); }));
        ClientThreadSend.Start(); // start packet sending thread once we're connected

        NetworkStream stream = connection.GetStream();
        while (true)
        {
            Thread.Sleep(1);
            try
            {
                if (!connection.Client.Poll(0, SelectMode.SelectRead))
                    continue;
                if (connection.Client.Available == 0)
                {
                    Debug.Log(string.Format("receiver implicitly disconnected"));
                    DoDisconnectMe = true;
                    break;
                }

                // try to recv packet header.
                byte[] packet_size_buf = new byte[4];
                if (stream.Read(packet_size_buf, 0, 4) != 4)
                    continue;
                uint packet_size = BitConverter.ToUInt32(packet_size_buf, 0);
                // recv packet data.
                byte[] packet_data = new byte[packet_size];
                stream.Read(packet_data, 0, (int)packet_size);
                // put into local receive queue.
                lock (ConnectionPackets)
                    ConnectionPackets.Add(packet_data);
            }
            catch (Exception e)
            {
                Debug.Log(string.Format("receiver exception = {0}", e));
                DoDisconnectMe = true;
                break;
            }
        }
    }

    private static void ClientThreadSendProc(TcpClient connection)
    {
        NetworkStream stream = connection.GetStream();
        BinaryWriter writer = new BinaryWriter(stream);
        while (true)
        {
            try
            {
                lock (ConnectionPacketsToSend)
                {
                    foreach (byte[] packet in ConnectionPacketsToSend)
                    {
                        // write packet header
                        writer.Write((uint)packet.Length);
                        writer.Write(packet);
                    }

                    ConnectionPacketsToSend.Clear();
                }
            }
            catch (Exception)
            {
                DoDisconnectMe = true;
                break;
            }

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

    private static bool SendPacket(byte[] packet)
    {
        lock (ConnectionPacketsToSend)
            ConnectionPacketsToSend.Add(packet);
        return true;
    }

    public static bool SendCommand(IServerCommand o)
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