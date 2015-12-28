using UnityEngine;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System;
using System.Linq;
using System.Threading;
using System.IO;

public enum NetworkState
{
    Disconnected,
    Client,
    Server
}

public class NetworkManager : MonoBehaviour {

    private static NetworkManager _Instance = null;
    public static NetworkManager Instance
    {
        get
        {
            if (_Instance == null) _Instance = FindObjectOfType<NetworkManager>();
            return _Instance;
        }
    }

    public static bool IsServer
    {
        get
        {
            return (Instance.State == NetworkState.Server);
        }
    }

    public static bool IsClient
    {
        get
        {
            return (Instance.State == NetworkState.Client);
        }
    }

    public NetworkState State { get; private set; }

    private void InitGeneric(ushort port)
    {

    }

    public void Start()
    {

    }

    public bool InitServer(ushort port)
    {
        if (State != NetworkState.Disconnected)
            Disconnect();

        InitGeneric(port);

        if (ServerManager.Init(port))
        {
            State = NetworkState.Server;
            return true;
        }

        return false;
    }

    public bool InitClient(string addr, ushort port)
    {
        if (State != NetworkState.Disconnected)
            Disconnect();

        InitGeneric(0); // init with default port

        if (ClientManager.Init(addr, port))
        {
            State = NetworkState.Client;
            return true;
        }

        return false;
    }

    public void Disconnect()
    {
        if (State == NetworkState.Server)
            ServerManager.Shutdown(false);
        if (State == NetworkState.Client)
            ClientManager.Shutdown(false);
        State = NetworkState.Disconnected;
    }

    public void OnDestroy()
    {
        ServerManager.Shutdown(true);
        ClientManager.Shutdown(true);
    }

    public void Update()
    {
        ServerManager.Update();
        ClientManager.Update();
    }

    private static byte[] DoReadDataFromStream(Socket sock, int size)
    {
        try
        {
            byte[] ovtmp = new byte[size];
            byte[] ov = new byte[size];
            int done = 0;
            while (true)
            {
                if (!sock.Poll(0, SelectMode.SelectRead))
                {
                    Thread.Sleep(1);
                    continue;
                }
                if (sock.Available == 0)
                    return null; // disconnected
                int doneNow = sock.Receive(ovtmp);
                ovtmp.Take(doneNow).ToArray().CopyTo(ov, done);
                done += doneNow;
                if (done == size)
                    return ov;
            }
        }
        catch(Exception e)
        {
            Debug.Log(e.ToString());
            return null;
        }
    }

    public static byte[] DoReadPacketFromStream(Socket sock)
    {
        // first off, try reading 4 bytes
        byte[] packet_size_buf = DoReadDataFromStream(sock, 4);
        if (packet_size_buf == null)
            return null;
        uint packet_size = BitConverter.ToUInt32(packet_size_buf, 0);
        byte[] packet_buf = DoReadDataFromStream(sock, (int)packet_size);
        return packet_buf;
    }

    private static bool DoWriteDataToStream(Socket sock, byte[] data)
    {
        try
        {
            int done = 0;
            while (true)
            {
                if (!sock.Poll(0, SelectMode.SelectWrite))
                {
                    Thread.Sleep(1);
                    continue;
                }
                byte[] ov = data.Skip(done).Take(1024).ToArray();
                int doneNow = sock.Send(ov);
                done += doneNow;
                if (done == data.Length)
                    return true;
            }
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
            return false;
        }
    }

    public static bool DoWritePacketToStream(Socket sock, byte[] packet)
    {
        if (!DoWriteDataToStream(sock, BitConverter.GetBytes((uint)packet.Length)))
            return false;
        if (!DoWriteDataToStream(sock, packet))
            return false;
        return true;
    }
}
