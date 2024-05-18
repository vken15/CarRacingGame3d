using System;
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
        public ulong PlayerId;
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
        int nbReconnectAttempts = 2;

        public int NbReconnectAttempts => nbReconnectAttempts;

        public int MaxConnectedPlayers = 8;

        public int CurrentConnectedPlayers = 1;

        public bool GameStarted = false;

        internal readonly OfflineState offline = new();
        internal readonly ClientConnectingState clientConnecting = new();
        internal readonly ClientConnectedState clientConnected = new();
        internal readonly ClientReconnectingState clientReconnecting = new();
        internal readonly StartingHostState startingHost = new();
        internal readonly HostingState hosting = new();

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
            currentState = offline;

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
