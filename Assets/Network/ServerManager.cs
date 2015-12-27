using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Reflection;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class ServerClient
{
    // data for map download
    public string Dl_Name;
    public int Dl_Done;
    public byte[] Dl_Bytes;

    public ClientState State = ClientState.Connected;
    internal TcpClient Connection;
    private Thread ConnectionThread;
    private Thread ConnectionThreadSend;
    private List<byte[]> ConnectionPackets = new List<byte[]>();
    private List<byte[]> ConnectionPacketsToSend = new List<byte[]>();
    public bool DoDisconnectMe { get; private set; }
    public string IPAddress { get; private set; }
    public ushort IPPort { get; private set; }
    public ServerClient(TcpClient conn)
    {
        Connection = conn;
        IPAddress = ((IPEndPoint)conn.Client.RemoteEndPoint).Address.ToString();
        IPPort = (ushort)((IPEndPoint)conn.Client.RemoteEndPoint).Port;
        ConnectionThread = new Thread(new ThreadStart(() => { ServerClientThreadProc(); }));
        ConnectionThread.Start();
        ConnectionThreadSend = new Thread(new ThreadStart(() => { ServerClientThreadSendProc(); }));
        ConnectionThreadSend.Start();
    }

    public void OnConnected()
    {
        Server.ClientConnected(this);
    }

    public void OnDisconnected()
    {
        Server.ClientDisconnected(this);
    }

    public void Disconnect()
    {
        ServerManager.DisconnectClient(this);
    }

    public void Update()
    {
        lock (ConnectionPackets)
        {
            foreach (byte[] packet in ConnectionPackets)
                OnPacketReceived(packet);
            ConnectionPackets.Clear();
        }
    }

    public void Kill()
    {
        if (ConnectionThread != null)
            ConnectionThread.Abort();
        ConnectionThread = null;
        if (ConnectionThreadSend != null)
            ConnectionThreadSend.Abort();
        ConnectionThreadSend = null;
        try
        {
            Connection.Close();
        }
        catch(Exception)
        {
            // ignore
        }
    }

    public void OnPacketReceived(byte[] packet)
    {
        // unserialize command
        MemoryStream ms = new MemoryStream(packet);
        try
        {
            BinaryFormatter bf = new BinaryFormatter();
            object o = bf.Deserialize(ms);
            if (!(o is IServerCommand))
            {
                GameConsole.Instance.WriteLine("Server commands should implement IServerCommand.");
                ServerManager.DisconnectClient(this);
                return;
            }

            if (!((IServerCommand)o).Process(this))
                ServerManager.DisconnectClient(this);
        }
        catch (Exception)
        {
            GameConsole.Instance.WriteLine("Error encountered during command processing.");
            ServerManager.DisconnectClient(this);
        }
        finally
        {
            ms.Close();
        }
    }

    private bool SendPacket(byte[] packet)
    {
        lock (ConnectionPacketsToSend)
            ConnectionPacketsToSend.Add(packet);
        return true;
    }

    public bool SendCommand(IClientCommand o)
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

    private void ServerClientThreadProc()
    {
        NetworkStream stream = Connection.GetStream();
        while (true)
        {
            Thread.Sleep(1);
            try
            {
                if (!Connection.Client.Poll(0, SelectMode.SelectRead))
                    continue;
                if (Connection.Client.Available == 0)
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
            catch(Exception e)
            {
                Debug.Log(string.Format("receiver exception = {0}", e));
                DoDisconnectMe = true;
                break;
            }
        }
    }

    private void ServerClientThreadSendProc()
    {
        NetworkStream stream = Connection.GetStream();
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
            catch(Exception)
            {
                DoDisconnectMe = true;
                break;
            }

            Thread.Sleep(1);
        }
    }
}

public class ServerManager
{
    public static List<ServerClient> Clients = new List<ServerClient>();
    public static Thread ServerThread { get; private set; }

    private static List<ServerClient> NewClients = new List<ServerClient>();
    private static TcpListener Listener;
    
    private static void ServerThreadProc(TcpListener listener)
    {
        listener.Start();
        while (true)
        {
            try
            {
                TcpClient tcpclient = listener.AcceptTcpClient();
                // new client has connected.
                ServerClient cl = new ServerClient(tcpclient);
                lock (NewClients)
                    NewClients.Add(cl);
                Thread.Sleep(1);
            }
            catch(Exception e)
            {
                Debug.Log(string.Format("ServerThreadProc: socket disconnected because {0}", e.ToString()));
                break;
            }
        }
    }

    public static bool Init(ushort port)
    {
        //
        // create listener thread
        Shutdown(false);

        try
        {
            Listener = new TcpListener(new IPAddress(0), port);
        }
        catch(Exception)
        {
            GameConsole.Instance.WriteLine("Unable to listen on port {0}", port);
            Listener = null;
            return false;
        }

        ServerThread = new Thread(new ThreadStart(() => { ServerThreadProc(Listener); }));
        ServerThread.Start();
        return true;
    }

    public static void Shutdown(bool forced)
    {
        if (forced)
        {
            if (ServerThread != null)
                ServerThread.Abort();
            ServerThread = null;

            foreach (ServerClient client in Clients)
                client.Kill();
            Clients.Clear();
        }
        else
        {
            for (int i = 0; i < Clients.Count; i++)
            {
                DisconnectClient(Clients[i]);
                i--;
            }
        }
        if (Listener != null)
            Listener.Stop();
        Listener = null;
    }

    private static byte[] RecBuffer = new byte[4096];
    public static void Update()
    {
        if (!NetworkManager.IsServer)
            return;

        lock (NewClients)
        {
            foreach (ServerClient client in NewClients)
            {
                Clients.Add(client);
                client.OnConnected();
            }

            NewClients.Clear();

            for (int i = 0; i < Clients.Count; i++)
            {
                if (Clients[i].DoDisconnectMe)
                {
                    DisconnectClient(Clients[i]);
                    i--;
                }
            }
        }

        foreach (ServerClient client in Clients)
            client.Update();
    }

    public static void DisconnectClient(ServerClient client)
    {
        try
        {
            client.Connection.Close();
        }
        catch(Exception)
        {
            // ignore
        }
        client.OnDisconnected();
        Clients.Remove(client);
    }
}