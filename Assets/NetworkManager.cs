using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

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

    public NetworkState State { get; private set; }

    public HostTopology Topology { get; private set; }
    public int HostID { get; private set; }
    public int ConnectionID { get; private set; }
    public int ReliableChannel { get; private set; }
    public int UnreliableChannel { get; private set; }

    private void InitGeneric(ushort port)
    {
        GlobalConfig gConfig = new GlobalConfig();
        gConfig.MaxPacketSize = 65535;
        NetworkTransport.Init(gConfig);
        ConnectionConfig config = new ConnectionConfig();
        config.MaxSentMessageQueueSize = 65535;
        config.MaxCombinedReliableMessageCount = 32;
        config.MaxCombinedReliableMessageSize = 512;
        config.PacketSize = 65535;
        ReliableChannel = config.AddChannel(QosType.ReliableSequenced);
        //ReliableChannel = config.AddChannel(QosType.ReliableFragmented);
        UnreliableChannel = config.AddChannel(QosType.Unreliable);
        Topology = new HostTopology(config, 256);
        ConnectionID = NetworkTransport.AddHost(Topology, port);
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
            ServerManager.Shutdown();
        if (State == NetworkState.Client)
            ClientManager.Shutdown();
        byte error;
        NetworkTransport.Disconnect(HostID, ConnectionID, out error); // ignore error here
        NetworkTransport.Shutdown();
        State = NetworkState.Disconnected;
    }

    public void Update()
    {
        switch(State)
        {
            case NetworkState.Server:
                ServerManager.Update();
                break;
            case NetworkState.Client:
                ClientManager.Update();
                break;
        }
    }
}
