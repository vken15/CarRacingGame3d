using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace CarRacingGame3d
{
    public struct ReconnectMessage
    {
        public int CurrentAttempt;
        public int MaxAttempt;

        public ReconnectMessage(int currentAttempt, int maxAttempt)
        {
            CurrentAttempt = currentAttempt;
            MaxAttempt = maxAttempt;
        }
    }

    public struct ConnectionEventMessage : INetworkSerializeByMemcpy
    {
        public ConnectStatus ConnectStatus;
        public FixedPlayerName PlayerName;
    }

    [Serializable]
    public class ConnectionPayload
    {
        public string playerId;
        public string playerName;
        public bool isDebug;
    }

    /// <summary>
    /// This state machine handles connection through the NetworkManager. It is responsible for listening to
    /// NetworkManger callbacks and other outside calls and redirecting them to the current ConnectionState object.
    /// </summary>
    public class ConnectionManager : MonoBehaviour
    {
        public static ConnectionManager instance = null;

        ConnectionState currentState;

        NetworkManager networkManager;

        ExampleNetworkDiscovery discovery;

        public NetworkManager NetworkManager => networkManager;

        public ExampleNetworkDiscovery Discovery => discovery;

        [SerializeField]
        int m_NbReconnectAttempts = 2;

        public int NbReconnectAttempts => m_NbReconnectAttempts;

        public int MaxConnectedPlayers = 8;

        public bool GameStarted = false;

        internal readonly OfflineState m_Offline = new();
        internal readonly ClientConnectingState m_ClientConnecting = new();
        internal readonly ClientConnectedState m_ClientConnected = new();
        internal readonly ClientReconnectingState m_ClientReconnecting = new();
        internal readonly StartingHostState m_StartingHost = new();
        internal readonly HostingState m_Hosting = new();

        void Awake()
        {
            if (instance == null)
                instance = this;
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }

            networkManager = FindAnyObjectByType<NetworkManager>();
            discovery = FindAnyObjectByType<ExampleNetworkDiscovery>();
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            currentState = m_Offline;

            NetworkManager.OnClientConnectedCallback += OnClientConnectedCallback;
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnectCallback;
            NetworkManager.OnServerStarted += OnServerStarted;
            NetworkManager.ConnectionApprovalCallback += ApprovalCheck;
            NetworkManager.OnTransportFailure += OnTransportFailure;
            NetworkManager.OnServerStopped += OnServerStopped;
        }

        void OnDestroy()
        {
            NetworkManager.OnClientConnectedCallback -= OnClientConnectedCallback;
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnectCallback;
            NetworkManager.OnServerStarted -= OnServerStarted;
            NetworkManager.ConnectionApprovalCallback -= ApprovalCheck;
            NetworkManager.OnTransportFailure -= OnTransportFailure;
            NetworkManager.OnServerStopped -= OnServerStopped;
        }

        internal void ChangeState(ConnectionState nextState)
        {
            Debug.Log($"{name}: Changed connection state from {currentState.GetType().Name} to {nextState.GetType().Name}.");

            currentState?.Exit();
            currentState = nextState;
            currentState.Enter();
        }

        void OnClientDisconnectCallback(ulong clientId)
        {
            currentState.OnClientDisconnect(clientId);
        }

        void OnClientConnectedCallback(ulong clientId)
        {
            currentState.OnClientConnected(clientId);
        }

        void OnServerStarted()
        {
            currentState.OnServerStarted();
        }

        void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            currentState.ApprovalCheck(request, response);
        }

        void OnTransportFailure()
        {
            currentState.OnTransportFailure();
        }

        void OnServerStopped(bool _) // we don't need this parameter as the ConnectionState already carries the relevant information
        {
            currentState.OnServerStopped();
        }

        public void StartClientLobby(string playerName)
        {
            currentState.StartClientLobby(playerName);
        }

        public void StartClientIp(string playerName, string ipaddress, int port)
        {
            currentState.StartClientIP(playerName, ipaddress, port);
        }

        public void StartHostLobby(string playerName)
        {
            currentState.StartHostLobby(playerName);
        }

        public void StartHostIp(string playerName, string ipaddress, int port)
        {
            currentState.StartHostIP(playerName, ipaddress, port);
        }

        public void RequestShutdown()
        {
            currentState.OnUserRequestedShutdown();
        }
    }
}
