using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace CarRacingGame3d
{
    /// <summary>
    /// Server specialization of Character Select game state.
    /// </summary>
    [RequireComponent(typeof(NetcodeHooks), typeof(NetworkRoom))]
    public class ServerRoomState : MonoBehaviour
    {
        [SerializeField] NetcodeHooks netcodeHooks;

        public NetworkRoom networkRoom { get; private set; }

        Coroutine m_WaitToEndLobbyCoroutine;

        private void Awake()
        {
            networkRoom = GetComponent<NetworkRoom>();

            netcodeHooks.OnNetworkSpawnHook += OnNetworkSpawn;
            netcodeHooks.OnNetworkDespawnHook += OnNetworkDespawn;
        }

        private void OnDestroy()
        {
            if (netcodeHooks)
            {
                netcodeHooks.OnNetworkSpawnHook -= OnNetworkSpawn;
                netcodeHooks.OnNetworkDespawnHook -= OnNetworkDespawn;
            }
        }

        void OnClientReady(ulong clientId, ushort carId, bool lockedIn)
        {
            int id = FindLobbyPlayerId(clientId);
            if (id == -1)
            {
                throw new Exception($"OnClientChangedSeat: client ID {clientId} is not a lobby player and cannot change seats! Shouldn't be here!");
            }

            networkRoom.LobbyPlayers[id] = new NetworkRoom.LobbyPlayerState(clientId,
                networkRoom.LobbyPlayers[id].PlayerName,
                networkRoom.LobbyPlayers[id].PlayerNumber,
                lockedIn ? SeatState.LockedIn : SeatState.Active, networkRoom.LobbyPlayers[id].SeatId, carId);
        }

        /// <summary>
        /// Returns the index of a client in the master LobbyPlayer list, or -1 if not found
        /// </summary>
        int FindLobbyPlayerId(ulong clientId)
        {
            for (int i = 0; i < networkRoom.LobbyPlayers.Count; ++i)
            {
                if (networkRoom.LobbyPlayers[i].ClientId == clientId)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Cancels the process of closing the lobby, so that if a new player joins, they are able to chose a character.
        /// </summary>
        void CancelCloseLobby()
        {
            if (m_WaitToEndLobbyCoroutine != null)
            {
                StopCoroutine(m_WaitToEndLobbyCoroutine);
            }
            networkRoom.IsLobbyClosed.Value = false;
        }

        void SaveLobbyResults()
        {
            GameManager.instance.ClearDriverList();
            foreach (NetworkRoom.LobbyPlayerState playerInfo in networkRoom.LobbyPlayers)
            {
                GameManager.instance.AddDriverToList(playerInfo.PlayerNumber, playerInfo.PlayerName, playerInfo.CarId, false, playerInfo.ClientId);
                SessionPlayerData? sessionPlayerData = SessionManager<SessionPlayerData>.Instance.GetPlayerData(playerInfo.ClientId);
                if (sessionPlayerData.HasValue)
                {
                    var playerData = sessionPlayerData.Value;
                    GameManager.instance.AddPoints(playerInfo.PlayerNumber, playerData.Score);
                }
            }
        }

        IEnumerator WaitToEndLobby()
        {
            yield return new WaitForSeconds(3);
            SceneTransitionHandler.Instance.SwitchScene(GameManager.instance.map.Scene);
        }

        public void OnNetworkDespawn()
        {
            if (NetworkManager.Singleton)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
                SceneTransitionHandler.Instance.OnClientLoadedScene -= OnClientLoadedScene;
            }
            if (networkRoom)
            {
                networkRoom.OnClientReady -= OnClientReady;
            }
        }

        public void OnNetworkSpawn()
        {
            if (!NetworkManager.Singleton.IsServer)
            {
                enabled = false;
            }
            else
            {
                ConnectionManager.instance.GameStarted = false;

                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
                networkRoom.OnClientReady += OnClientReady;
                SceneTransitionHandler.Instance.OnClientLoadedScene += OnClientLoadedScene;
            }
        }

        void OnClientLoadedScene(ulong clientId)
        {
            SeatNewPlayer(clientId);
        }

        ushort GetAvailablePlayerNumber()
        {
            for (ushort possiblePlayerNumber = 1; possiblePlayerNumber <= ConnectionManager.instance.MaxConnectedPlayers; ++possiblePlayerNumber)
            {
                if (IsPlayerNumberAvailable(possiblePlayerNumber))
                {
                    return possiblePlayerNumber;
                }
            }
            // we couldn't get a Player# for this person... which means the lobby is full!
            return 0;
        }

        bool IsPlayerNumberAvailable(int playerNumber)
        {
            bool found = false;
            foreach (NetworkRoom.LobbyPlayerState playerState in networkRoom.LobbyPlayers)
            {
                if (playerState.PlayerNumber == playerNumber)
                {
                    found = true;
                    break;
                }
            }

            return !found;
        }

        void SeatNewPlayer(ulong clientId)
        {
            // If lobby is closing and waiting to start the game, cancel to allow that new player to select a character
            if (networkRoom.IsLobbyClosed.Value)
            {
                CancelCloseLobby();
            }

            SessionPlayerData? sessionPlayerData = SessionManager<SessionPlayerData>.Instance.GetPlayerData(clientId);
            if (sessionPlayerData.HasValue)
            {
                var playerData = sessionPlayerData.Value;
                if (playerData.PlayerNumber == 0 || !IsPlayerNumberAvailable(playerData.PlayerNumber))
                {
                    // If no player num already assigned or if player num is no longer available, get an available one.
                    playerData.PlayerNumber = GetAvailablePlayerNumber();
                }
                if (playerData.PlayerNumber == 0)
                {
                    // Sanity check. We ran out of seats... there was no room!
                    throw new Exception($"we shouldn't be here, connection approval should have refused this connection already for client ID {clientId} and player num {playerData.PlayerNumber}");
                }

                if (NetworkManager.Singleton.IsHost && clientId == NetworkManager.Singleton.LocalClientId)
                    networkRoom.LobbyPlayers.Add(new NetworkRoom.LobbyPlayerState(clientId, playerData.PlayerName, playerData.PlayerNumber, SeatState.Host, playerData.PlayerNumber - 1));
                else
                    networkRoom.LobbyPlayers.Add(new NetworkRoom.LobbyPlayerState(clientId, playerData.PlayerName, playerData.PlayerNumber, SeatState.Active, playerData.PlayerNumber - 1));

                SessionManager<SessionPlayerData>.Instance.SetPlayerData(clientId, playerData);
            }
        }

        void OnClientDisconnectCallback(ulong clientId)
        {
            // clear this client's PlayerNumber and any associated visuals (so other players know they're gone).
            for (int i = 0; i < networkRoom.LobbyPlayers.Count; ++i)
            {
                if (networkRoom.LobbyPlayers[i].ClientId == clientId)
                {
                    networkRoom.LobbyPlayers.RemoveAt(i);
                    break;
                }
            }
        }

        //Button

        /// <summary>
        /// Looks through all our connections and sees if everyone has ready;
        /// if so, we lock in the whole lobby, save state, and begin the transition to gameplay
        /// </summary>
        public void OnStartGame()
        {
            foreach (NetworkRoom.LobbyPlayerState playerInfo in networkRoom.LobbyPlayers)
            {
                if (playerInfo.SeatState != SeatState.LockedIn && playerInfo.SeatState != SeatState.Host)
                    return; // nope, at least one player isn't locked in yet!
            }

            // everybody's ready at the same time! Lock it down!
            networkRoom.IsLobbyClosed.Value = true;

            // remember our choices so the next scene can use the info
            SaveLobbyResults();

            // prevent other player from joining
            ConnectionManager.instance.GameStarted = true;

            // Delay a few seconds to give the UI time to react, then switch scenes
            m_WaitToEndLobbyCoroutine = StartCoroutine(WaitToEndLobby());
        }
    }
}
