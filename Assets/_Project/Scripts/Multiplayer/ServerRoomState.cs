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

        public NetworkRoom NetworkRoom { get; private set; }

        Coroutine m_WaitToEndLobbyCoroutine;

        private void Awake()
        {
            NetworkRoom = GetComponent<NetworkRoom>();

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
                throw new Exception($"OnClientChangedSeat: client ID {clientId} is not a lobby player and shouldn't be here!");
            }

            NetworkRoom.LobbyPlayers[id] = new NetworkRoom.LobbyPlayerState(clientId,
                NetworkRoom.LobbyPlayers[id].PlayerName,
                NetworkRoom.LobbyPlayers[id].PlayerNumber,
                NetworkManager.Singleton.LocalClientId == clientId ? SeatState.Host : lockedIn ? SeatState.LockedIn : SeatState.Active, NetworkRoom.LobbyPlayers[id].SeatId, carId);
        }

        /// <summary>
        /// Returns the index of a client in the master LobbyPlayer list, or -1 if not found
        /// </summary>
        int FindLobbyPlayerId(ulong clientId)
        {
            for (int i = 0; i < NetworkRoom.LobbyPlayers.Count; ++i)
            {
                if (NetworkRoom.LobbyPlayers[i].ClientId == clientId)
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
            NetworkRoom.IsLobbyClosed.Value = false;
        }

        void SaveLobbyResults()
        {
            GameManager.instance.ClearDriverList();
            foreach (NetworkRoom.LobbyPlayerState playerInfo in NetworkRoom.LobbyPlayers)
            {
                if (playerInfo.SeatState == SeatState.AI)
                {
                    GameManager.instance.AddDriverToList(playerInfo.PlayerNumber, playerInfo.PlayerName, playerInfo.CarId, true, playerInfo.ClientId);
                }
                else
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
        }

        IEnumerator WaitToEndLobby()
        {
            yield return new WaitForSeconds(3);
            SceneTransitionHandler.Instance.SwitchScene(GameManager.instance.map.Scene, true);
        }

        public void OnNetworkDespawn()
        {
            if (NetworkManager.Singleton)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
                SceneTransitionHandler.Instance.OnClientLoadedScene -= OnClientLoadedScene;
            }
            if (NetworkRoom)
            {
                NetworkRoom.OnClientReady -= OnClientReady;
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
                NetworkRoom.OnClientReady += OnClientReady;
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
            foreach (NetworkRoom.LobbyPlayerState playerState in NetworkRoom.LobbyPlayers)
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
            if (NetworkRoom.IsLobbyClosed.Value)
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
                    NetworkRoom.LobbyPlayers.Add(new NetworkRoom.LobbyPlayerState(clientId, playerData.PlayerName, playerData.PlayerNumber, SeatState.Host, playerData.PlayerNumber - 1));
                else
                    NetworkRoom.LobbyPlayers.Add(new NetworkRoom.LobbyPlayerState(clientId, playerData.PlayerName, playerData.PlayerNumber, SeatState.Active, playerData.PlayerNumber - 1));

                SessionManager<SessionPlayerData>.Instance.SetPlayerData(clientId, playerData);
            }
        }

        void OnClientDisconnectCallback(ulong clientId)
        {
            // clear this client's PlayerNumber and any associated visuals (so other players know they're gone).
            for (int i = 0; i < NetworkRoom.LobbyPlayers.Count; ++i)
            {
                if (NetworkRoom.LobbyPlayers[i].ClientId == clientId)
                {
                    NetworkRoom.LobbyPlayers.RemoveAt(i);
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
            foreach (NetworkRoom.LobbyPlayerState playerInfo in NetworkRoom.LobbyPlayers)
            {
                if (playerInfo.SeatState != SeatState.LockedIn && playerInfo.SeatState != SeatState.Host && playerInfo.SeatState != SeatState.AI)
                    return; // nope, at least one player isn't locked in yet!
            }

            // everybody's ready at the same time! Lock it down!
            NetworkRoom.IsLobbyClosed.Value = true;

            // remember our choices so the next scene can use the info
            SaveLobbyResults();

            // prevent other player from joining
            ConnectionManager.instance.GameStarted = true;

            // Delay a few seconds to give the UI time to react, then switch scenes
            m_WaitToEndLobbyCoroutine = StartCoroutine(WaitToEndLobby());
        }

        public void OnAddAI(string number)
        {
            ushort playerNumber = ushort.Parse(number);
            NetworkRoom.LobbyPlayers.Add(new NetworkRoom.LobbyPlayerState(NetworkManager.Singleton.LocalClientId, "AI", playerNumber, SeatState.AI, playerNumber - 1));
        }

        public void OnRemoveAI(string number)
        {
            ushort playerNumber = ushort.Parse(number);
            for (int i = 0; i < NetworkRoom.LobbyPlayers.Count; ++i)
            {
                if (NetworkRoom.LobbyPlayers[i].PlayerNumber == playerNumber)
                {
                    NetworkRoom.LobbyPlayers.RemoveAt(i);
                    break;
                }
            }
        }
    }
}
