using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CarRacingGame3d
{
    public interface ISessionPlayerData
    {
        bool IsConnected { get; set; }
        ulong ClientID { get; set; }

        void Reinitialize();
    }

    /// <summary>
    /// This class uses a unique player ID to bind a player to a session. Once that player connects to a host, the host
    /// associates the current ClientID to the player's unique ID. If the player disconnects and reconnects to the same
    /// host, the session is preserved.
    /// </summary>
    /// <remarks>
    /// Using a client-generated player ID and sending it directly could be problematic, as a malicious user could
    /// intercept it and reuse it to impersonate the original user. We are currently investigating this to offer a
    /// solution that handles security better.
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    public class SessionManager<T> where T : struct, ISessionPlayerData
    {
        SessionManager()
        {
            clientData = new Dictionary<string, T>();
            clientIDToPlayerId = new Dictionary<ulong, string>();
        }

        public static SessionManager<T> Instance => instance ??= new SessionManager<T>();

        static SessionManager<T> instance;

        /// <summary>
        /// Maps a given client player id to the data for a given client player.
        /// </summary>
        Dictionary<string, T> clientData;

        /// <summary>
        /// Map to allow us to cheaply map from player id to player data.
        /// </summary>
        Dictionary<ulong, string> clientIDToPlayerId;

        bool hasSessionStarted;

        /// <summary>
        /// Handles client disconnect."
        /// </summary>
        public void DisconnectClient(ulong clientId)
        {
            if (hasSessionStarted)
            {
                // Mark client as disconnected, but keep their data so they can reconnect.
                if (clientIDToPlayerId.TryGetValue(clientId, out var playerId))
                {
                    if (GetPlayerData(playerId)?.ClientID == clientId)
                    {
                        var clientData = this.clientData[playerId];
                        clientData.IsConnected = false;
                        this.clientData[playerId] = clientData;
                    }
                }
            }
            else
            {
                // Session has not started, no need to keep their data
                if (clientIDToPlayerId.TryGetValue(clientId, out var playerId))
                {
                    clientIDToPlayerId.Remove(clientId);
                    if (GetPlayerData(playerId)?.ClientID == clientId)
                    {
                        clientData.Remove(playerId);
                    }
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="playerId">This is the playerId that is unique to this client and persists across multiple logins from the same client</param>
        /// <returns>True if a player with this ID is already connected.</returns>
        public bool IsDuplicateConnection(string playerId)
        {
            return clientData.ContainsKey(playerId) && clientData[playerId].IsConnected;
        }

        /// <summary>
        /// Adds a connecting player's session data if it is a new connection, or updates their session data in case of a reconnection.
        /// </summary>
        /// <param name="clientId">This is the clientId that Netcode assigned us on login. It does not persist across multiple logins from the same client. </param>
        /// <param name="playerId">This is the playerId that is unique to this client and persists across multiple logins from the same client</param>
        /// <param name="sessionPlayerData">The player's initial data</param>
        public void SetupConnectingPlayerSessionData(ulong clientId, string playerId, T sessionPlayerData)
        {
            var isReconnecting = false;

            // Test for duplicate connection
            if (IsDuplicateConnection(playerId))
            {
                Debug.LogError($"Player ID {playerId} already exists. This is a duplicate connection. Rejecting this session data.");
                return;
            }

            // If another client exists with the same playerId
            if (clientData.ContainsKey(playerId))
            {
                if (!clientData[playerId].IsConnected)
                {
                    // If this connecting client has the same player Id as a disconnected client, this is a reconnection.
                    isReconnecting = true;
                }

            }

            // Reconnecting. Give data from old player to new player
            if (isReconnecting)
            {
                // Update player session data
                sessionPlayerData = clientData[playerId];
                sessionPlayerData.ClientID = clientId;
                sessionPlayerData.IsConnected = true;
            }

            //Populate our dictionaries with the SessionPlayerData
            clientIDToPlayerId[clientId] = playerId;
            clientData[playerId] = sessionPlayerData;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="clientId"> id of the client whose data is requested</param>
        /// <returns>The Player ID matching the given client ID</returns>
        public string GetPlayerId(ulong clientId)
        {
            if (clientIDToPlayerId.TryGetValue(clientId, out string playerId))
            {
                return playerId;
            }

            Debug.Log($"No client player ID found mapped to the given client ID: {clientId}");
            return null;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="clientId"> id of the client whose data is requested</param>
        /// <returns>Player data struct matching the given ID</returns>
        public T? GetPlayerData(ulong clientId)
        {
            //First see if we have a playerId matching the clientID given.
            var playerId = GetPlayerId(clientId);
            if (playerId != null)
            {
                return GetPlayerData(playerId);
            }

            Debug.Log($"No client player ID found mapped to the given client ID: {clientId}");
            return null;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="playerId"> Player ID of the client whose data is requested</param>
        /// <returns>Player data struct matching the given ID</returns>
        public T? GetPlayerData(string playerId)
        {
            if (clientData.TryGetValue(playerId, out T data))
            {
                return data;
            }

            Debug.Log($"No PlayerData of matching player ID found: {playerId}");
            return null;
        }

        /// <summary>
        /// Updates player data
        /// </summary>
        /// <param name="clientId"> id of the client whose data will be updated </param>
        /// <param name="sessionPlayerData"> new data to overwrite the old </param>
        public void SetPlayerData(ulong clientId, T sessionPlayerData)
        {
            if (clientIDToPlayerId.TryGetValue(clientId, out string playerId))
            {
                clientData[playerId] = sessionPlayerData;
            }
            else
            {
                Debug.LogError($"No client player ID found mapped to the given client ID: {clientId}");
            }
        }

        /// <summary>
        /// Marks the current session as started, so from now on we keep the data of disconnected players.
        /// </summary>
        public void OnSessionStarted()
        {
            hasSessionStarted = true;
        }

        /// <summary>
        /// Reinitializes session data from connected players, and clears data from disconnected players, so that if they reconnect in the next game, they will be treated as new players
        /// </summary>
        public void OnSessionEnded()
        {
            ClearDisconnectedPlayersData();
            ReinitializePlayersData();
            hasSessionStarted = false;
        }

        /// <summary>
        /// Resets all our runtime state, so it is ready to be reinitialized when starting a new server
        /// </summary>
        public void OnServerEnded()
        {
            clientData.Clear();
            clientIDToPlayerId.Clear();
            hasSessionStarted = false;
        }

        void ReinitializePlayersData()
        {
            foreach (var id in clientIDToPlayerId.Keys)
            {
                string playerId = clientIDToPlayerId[id];
                T sessionPlayerData = clientData[playerId];
                sessionPlayerData.Reinitialize();
                clientData[playerId] = sessionPlayerData;
            }
        }

        void ClearDisconnectedPlayersData()
        {
            List<ulong> idsToClear = new();
            foreach (var id in clientIDToPlayerId.Keys)
            {
                var data = GetPlayerData(id);
                if (data is { IsConnected: false })
                {
                    idsToClear.Add(id);
                }
            }

            foreach (var id in idsToClear)
            {
                string playerId = clientIDToPlayerId[id];
                if (GetPlayerData(playerId)?.ClientID == id)
                {
                    clientData.Remove(playerId);
                }

                clientIDToPlayerId.Remove(id);
            }
        }
    }
}
