using System;
using System.Threading.Tasks;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Core;
using Unity.Services.Relay.Models;
using Unity.Services.Relay;
using UnityEngine;
using Unity.Services.Authentication;

namespace CarRacingGame3d
{
    public abstract class ConnectionMethodBase
    {
        protected ConnectionManager ConnectionManager;
        readonly ProfileManager ProfileManager;
        protected readonly string PlayerName;
        protected const string DtlsConnType = "dtls";

        /// <summary>
        /// Setup the host connection prior to starting the NetworkManager
        /// </summary>
        /// <returns></returns>
        public abstract Task SetupHostConnectionAsync();


        /// <summary>
        /// Setup the client connection prior to starting the NetworkManager
        /// </summary>
        /// <returns></returns>
        public abstract Task SetupClientConnectionAsync();

        /// <summary>
        /// Setup the client for reconnection prior to reconnecting
        /// </summary>
        /// <returns>
        /// success = true if succeeded in setting up reconnection, false if failed.
        /// shouldTryAgain = true if we should try again after failing, false if not.
        /// </returns>
        public abstract Task<(bool success, bool shouldTryAgain)> SetupClientReconnectionAsync();

        public ConnectionMethodBase(ConnectionManager connectionManager, ProfileManager profileManager, string playerName)
        {
            ConnectionManager = connectionManager;
            ProfileManager = profileManager;
            PlayerName = playerName;
        }

        protected void SetConnectionPayload(string playerId, string playerName)
        {
            var payload = JsonUtility.ToJson(new ConnectionPayload()
            {
                playerId = playerId,
                playerName = playerName,
                isDebug = Debug.isDebugBuild
            });

            var payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);

            ConnectionManager.NetworkManager.NetworkConfig.ConnectionData = payloadBytes;
        }

        /// Using authentication, this makes sure your session is associated with your account and not your device. This means you could reconnect
        /// from a different device for example. A playerId is also a bit more permanent than player prefs. In a browser for example,
        /// player prefs can be cleared as easily as cookies.
        /// The forked flow here is for debug purposes and to make UGS optional in Boss Room. This way you can study the sample without
        /// setting up a UGS account. It's recommended to investigate your own initialization and IsSigned flows to see if you need
        /// those checks on your own and react accordingly. We offer here the option for offline access for debug purposes, but in your own game you
        /// might want to show an error popup and ask your player to connect to the internet.
        protected string GetPlayerId()
        {
            if (Unity.Services.Core.UnityServices.State != ServicesInitializationState.Initialized)
            {
                return ClientPrefs.GetGuid() + ProfileManager.Profile;
            }

            return AuthenticationService.Instance.IsSignedIn ? AuthenticationService.Instance.PlayerId : ClientPrefs.GetGuid() + ProfileManager.Profile;
        }
    }

    /// <summary>
    /// Simple IP connection setup with UTP
    /// </summary>
    class ConnectionMethodIP : ConnectionMethodBase
    {
        readonly string ipaddress;
        readonly ushort port;

        public ConnectionMethodIP(string ip, ushort port, ConnectionManager connectionManager, ProfileManager profileManager, string playerName)
            : base(connectionManager, profileManager, playerName)
        {
            ipaddress = ip;
            this.port = port;
            ConnectionManager = connectionManager;
        }

        public override async Task SetupClientConnectionAsync()
        {
            SetConnectionPayload(GetPlayerId(), PlayerName);
            var utp = (UnityTransport)ConnectionManager.NetworkManager.NetworkConfig.NetworkTransport;
            utp.SetConnectionData(ipaddress, port);
        }

        public override async Task<(bool success, bool shouldTryAgain)> SetupClientReconnectionAsync()
        {
            // Nothing to do here
            return (true, true);
        }

        public override async Task SetupHostConnectionAsync()
        {
            SetConnectionPayload(GetPlayerId(), PlayerName); // Need to set connection payload for host as well, as host is a client too
            var utp = (UnityTransport)ConnectionManager.NetworkManager.NetworkConfig.NetworkTransport;
            utp.SetConnectionData(ipaddress, port);
        }
    }

    /// <summary>
    /// UTP's Relay connection setup using the Lobby integration
    /// </summary>
    class ConnectionMethodRelay : ConnectionMethodBase
    {
        public ConnectionMethodRelay(ConnectionManager connectionManager, ProfileManager profileManager, string playerName)
            : base(connectionManager, profileManager, playerName)
        {
            ConnectionManager = connectionManager;
        }

        public override async Task SetupClientConnectionAsync()
        {
            Debug.Log("Setting up Unity Relay client");

            SetConnectionPayload(GetPlayerId(), PlayerName);

            if (LobbyServiceFacade.Instance.CurrentUnityLobby == null)
            {
                throw new Exception("Trying to start relay while Lobby isn't set");
            }

            Debug.Log($"Setting Unity Relay client with join code {LocalLobby.Instance.RelayJoinCode}");

            // Create client joining allocation from join code
            var joinedAllocation = await RelayService.Instance.JoinAllocationAsync(LocalLobby.Instance.RelayJoinCode);
            Debug.Log($"client: {joinedAllocation.ConnectionData[0]} {joinedAllocation.ConnectionData[1]}, " +
                $"host: {joinedAllocation.HostConnectionData[0]} {joinedAllocation.HostConnectionData[1]}, " +
                $"client: {joinedAllocation.AllocationId}");

            await LobbyServiceFacade.Instance.UpdatePlayerDataAsync(joinedAllocation.AllocationId.ToString(), LocalLobby.Instance.RelayJoinCode);

            // Configure UTP with allocation
            var utp = (UnityTransport)ConnectionManager.NetworkManager.NetworkConfig.NetworkTransport;
            utp.SetRelayServerData(new RelayServerData(joinedAllocation, DtlsConnType));
        }

        public override async Task<(bool success, bool shouldTryAgain)> SetupClientReconnectionAsync()
        {
            if (LobbyServiceFacade.Instance.CurrentUnityLobby == null)
            {
                Debug.Log("Lobby does not exist anymore, stopping reconnection attempts.");
                return (false, false);
            }

            // When using Lobby with Relay, if a user is disconnected from the Relay server, the server will notify the
            // Lobby service and mark the user as disconnected, but will not remove them from the lobby. They then have
            // some time to attempt to reconnect (defined by the "Disconnect removal time" parameter on the dashboard),
            // after which they will be removed from the lobby completely.
            // See https://docs.unity.com/lobby/reconnect-to-lobby.html
            //var lobby = await m_LobbyServiceFacade.ReconnectToLobbyAsync();
            //var success = lobby != null;
            //Debug.Log(success ? "Successfully reconnected to Lobby." : "Failed to reconnect to Lobby.");
            //return (success, true); // return a success if reconnecting to lobby returns a lobby
            return (true, true);
        }

        public override async Task SetupHostConnectionAsync()
        {
            Debug.Log("Setting up Unity Relay host");

            SetConnectionPayload(GetPlayerId(), PlayerName); // Need to set connection payload for host as well, as host is a client too

            // Create relay allocation
            Allocation hostAllocation = await RelayService.Instance.CreateAllocationAsync(ConnectionManager.MaxConnectedPlayers, region: null);
            var joinCode = await RelayService.Instance.GetJoinCodeAsync(hostAllocation.AllocationId);

            Debug.Log($"server: connection data: {hostAllocation.ConnectionData[0]} {hostAllocation.ConnectionData[1]}, " +
                $"allocation ID:{hostAllocation.AllocationId}, region:{hostAllocation.Region}");

            LocalLobby.Instance.RelayJoinCode = joinCode;
            LocalLobby.Instance.CurRound = GameManager.instance.currentRound;
            LocalLobby.Instance.MaxRound = GameManager.instance.maxRound;
            LocalLobby.Instance.MapId = GameManager.instance.map.MapID;

            // next line enables lobby and relay services integration
            await LobbyServiceFacade.Instance.UpdateLobbyDataAndUnlockAsync();
            await LobbyServiceFacade.Instance.UpdatePlayerDataAsync(hostAllocation.AllocationIdBytes.ToString(), joinCode);

            // Setup UTP with relay connection info
            var utp = (UnityTransport)ConnectionManager.NetworkManager.NetworkConfig.NetworkTransport;
            utp.SetRelayServerData(new RelayServerData(hostAllocation, DtlsConnType)); // This is with DTLS enabled for a secure connection

            Debug.Log($"Created relay allocation with join code {LocalLobby.Instance.RelayJoinCode}");
        }
    }

}
