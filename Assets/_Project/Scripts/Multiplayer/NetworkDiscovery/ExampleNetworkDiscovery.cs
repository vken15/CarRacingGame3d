using CarRacingGame3d;
using System;
using System.Net;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class ExampleNetworkDiscovery : NetworkDiscovery<DiscoveryBroadcastData, DiscoveryResponseData>
{
    //[Serializable]
    //public class ServerFoundEvent : UnityEvent<IPEndPoint, DiscoveryResponseData>
    //{
    //};

    NetworkManager m_NetworkManager;
    
    [SerializeField]
    [Tooltip("If true NetworkDiscovery will make the server visible and answer to client broadcasts as soon as netcode starts running as server.")]
    bool m_StartWithServer = true;

    //public ServerFoundEvent OnServerFound;
    public event Action<IPEndPoint, DiscoveryResponseData> OnServerFound;


    private bool m_HasStartedWithServer = false;

    public void Awake()
    {
        m_NetworkManager = GetComponent<NetworkManager>();
    }

    public void Update()
    {
        if (m_StartWithServer && m_HasStartedWithServer == false && IsRunning == false)
        {
            if (m_NetworkManager.IsServer)
            {
                StartServer();
                m_HasStartedWithServer = true;
            }
        }
    }

    protected override bool ProcessBroadcast(IPEndPoint sender, DiscoveryBroadcastData broadCast, out DiscoveryResponseData response)
    {
        var sessionData = SessionManager<SessionPlayerData>.Instance.GetPlayerData(NetworkManager.Singleton.LocalClientId);
        response = new DiscoveryResponseData()
        {
            ServerName = sessionData.Value.PlayerName,
            Port = ((UnityTransport)m_NetworkManager.NetworkConfig.NetworkTransport).ConnectionData.Port,
            CurrentPlayer = (ushort)NetworkManager.Singleton.ConnectedClientsIds.Count,
            MaxPlayer = (ushort)ConnectionManager.instance.MaxConnectedPlayers,
            MapId = GameManager.instance.map.MapID,
            CurRound = GameManager.instance.currentRound,
            MaxRound = GameManager.instance.maxRound,
        };
        return true;
    }

    protected override void ResponseReceived(IPEndPoint sender, DiscoveryResponseData response)
    {
        OnServerFound?.Invoke(sender, response);
    }
}