using UnityEngine;
using System.Collections;
using System.Net;
using System.Net.Sockets;

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
}
