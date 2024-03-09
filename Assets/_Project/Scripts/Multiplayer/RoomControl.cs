using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CarRacingGame3d
{
    public struct PlayerData
    {
        public string playerName;
        public ushort carId;
        public GameObject playerView;
        public bool isReady;
    }

    public class RoomControl : NetworkBehaviour
    {
        // Minimum player count required to transition to next level
        [SerializeField] private int minimumPlayerCount = 2;

        [SerializeField] private GridLayoutGroup layout;

        [SerializeField] private GameObject playerItem;

        private bool allPlayersInRoom;
        private Dictionary<ulong, PlayerData> clientsInRoom;

        public override void OnNetworkSpawn()
        {
            if (GameManager.instance.clientsInRoom.Count == 0 || !IsServer)
            {
                PlayerData data = new()
                {
                    playerName = "TEST",
                    carId = 1,
                    playerView = Instantiate(playerItem, layout.transform),
                    isReady = IsServer,
                };

                clientsInRoom = new()
                {
                    //Always add ourselves to the list at first
                    { NetworkManager.Singleton.LocalClientId, data }
                };
            } else
            {
                clientsInRoom = GameManager.instance.clientsInRoom;
                foreach (var client in clientsInRoom)
                {
                    var data = client.Value;
                    data.playerView = Instantiate(playerItem, layout.transform);
                    clientsInRoom[client.Key] = data;
                }
            }

            //If we are hosting, then handle the server side for detecting when clients have connected
            //and when their room scenes are finished loading.
            if (IsServer)
            {
                allPlayersInRoom = false;

                //Server will be notified when a client connects
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
                SceneTransitionHandler.sceneTransitionHandler.OnClientLoadedScene += ClientLoadedScene;

                GenerateUserStatsForRoom();
            }
            else
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnConnectedToServerCallback;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnServerShutdown;
            }

            SceneTransitionHandler.sceneTransitionHandler.SetSceneState(SceneTransitionHandler.SceneStates.Lobby);
            base.OnNetworkSpawn();
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
                SceneTransitionHandler.sceneTransitionHandler.OnClientLoadedScene -= ClientLoadedScene;
            } else
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnConnectedToServerCallback;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnServerShutdown;
            }
        }

        /// <summary>
        ///     GenerateUserStatsForRoom
        ///     Psuedo code for setting player state
        ///     Just updating a text field, this could use a lot of "refactoring"
        /// </summary>
        private void GenerateUserStatsForRoom()
        {
            foreach (var clientRoomStatus in clientsInRoom)
            {
                var userRoomStatusText = clientRoomStatus.Value.playerName + "_" + clientRoomStatus.Key + "          ";
                if (clientRoomStatus.Value.isReady)
                    userRoomStatusText += "(READY)\n";
                else
                    userRoomStatusText += "(NOT READY)\n";

                clientRoomStatus.Value.playerView.GetComponentInChildren<TMP_Text>().text = userRoomStatusText;
            }
        }

        /// <summary>
        ///     UpdateAndCheckPlayersInRoom
        ///     Checks to see if we have at least 2 or more people to start
        /// </summary>
        private void UpdateAndCheckPlayersInRoom()
        {
            allPlayersInRoom = clientsInRoom.Count >= minimumPlayerCount;

            foreach (var clientRoomStatus in clientsInRoom)
            {
                if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(clientRoomStatus.Key))
                    //If some clients are still loading into the Room scene then this is false
                    allPlayersInRoom = false;
            }
        }

        /// <summary>
        ///     ClientLoadedScene
        ///     Invoked when a client has loaded this scene
        /// </summary>
        /// <param name="clientId"></param>
        private void ClientLoadedScene(ulong clientId)
        {
            if (IsServer)
            {
                allPlayersInRoom = clientsInRoom.Count >= minimumPlayerCount;

                foreach (var clientRoomStatus in clientsInRoom)
                {
                    SendPlayerDataClientRpc(clientRoomStatus.Key, clientRoomStatus.Value.playerName, clientRoomStatus.Value.carId, clientRoomStatus.Value.isReady);
                    if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(clientRoomStatus.Key))
                        allPlayersInRoom = false;
                }
            }
        }

        /// <summary>
        ///     OnClientConnectedCallback
        ///     Since we are entering a Room and Netcode's NetworkManager is spawning the player,
        ///     the server can be configured to only listen for connected clients at this stage.
        /// </summary>
        /// <param name="clientId"></param>
        private void OnClientConnectedCallback(ulong clientId)
        {
            if (IsServer)
            {
                if (!clientsInRoom.ContainsKey(clientId))
                {
                    PlayerData data = new()
                    {
                        playerName = "Loading...",
                        carId = 1,
                        playerView = Instantiate(playerItem, layout.transform),
                        isReady = false
                    };
                    clientsInRoom.Add(clientId, data);
                    //SendClientReadyStatusUpdatesClientRpc(clientId, false);
                }

                GenerateUserStatsForRoom();
                UpdateAndCheckPlayersInRoom();
                Debug.Log(clientId + "connected!");
            }
        }

        /// <summary>
        ///     OnClientDisconnectCallback
        ///     Remove player from the room on disconnect
        /// </summary>
        /// <param name="clientId"></param>
        private void OnClientDisconnectCallback(ulong clientId)
        {
            if (IsServer)
            {
                if (clientsInRoom.ContainsKey(clientId))
                {
                    GameObject playerView = clientsInRoom[clientId].playerView;
                    clientsInRoom.Remove(clientId);
                    Destroy(playerView);
                    GenerateUserStatsForRoom();
                    UpdateAndCheckPlayersInRoom();
                    RemovePlayerClientRpc(clientId);
                    Debug.Log(clientId + "disconnected!");
                }
            }
        }

        private void OnConnectedToServerCallback(ulong clientId)
        {
            if (!IsServer)
            {
                PlayerData data = clientsInRoom[clientId];
                SendPlayerDataToServerRpc(data.playerName, data.carId);
            }
        }

        private void OnServerShutdown(ulong clientId)
        {
            SceneManager.LoadScene("Lobby");
        }

        /// <summary>
        ///     SendClientReadyStatusUpdatesClientRpc
        ///     Sent from the server to the client when a player's status is updated.
        ///     This also populates the connected clients' (excluding host) player state in the Room
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="isReady"></param>
        [ClientRpc]
        private void SendClientReadyStatusUpdatesClientRpc(ulong clientId, bool isReady)
        {
            if (!IsServer)
            {
                if (!clientsInRoom.ContainsKey(clientId))
                {
                    PlayerData data = new()
                    {
                        playerName = "TEST",
                        carId = 1,
                        playerView = Instantiate(playerItem, layout.transform),
                        isReady = isReady
                    };
                    clientsInRoom.Add(clientId, data);
                }
                else
                {
                    PlayerData data = clientsInRoom[clientId];
                    data.isReady = isReady;
                    clientsInRoom[clientId] = data;
                }

                GenerateUserStatsForRoom();
            }
        }

        [ClientRpc]
        private void SendPlayerDataClientRpc(ulong clientId, string playerName, ushort carId, bool isReady)
        {
            if (!IsServer)
            {
                if (!clientsInRoom.ContainsKey(clientId))
                {
                    PlayerData data = new()
                    {
                        playerName = playerName,
                        carId = carId,
                        playerView = Instantiate(playerItem, layout.transform),
                        isReady = isReady
                    };
                    clientsInRoom.Add(clientId, data);
                }
                else
                {
                    PlayerData data = clientsInRoom[clientId];
                    data.isReady = isReady;
                    clientsInRoom[clientId] = data;
                }

                GenerateUserStatsForRoom();
            }
        }


        /// <summary>
        /// RemovePlayerClientRpc
        /// Send from the server to the client when a player's diconnect or kicked.
        /// </summary>
        /// <param name="clientId"></param>
        [ClientRpc]
        private void RemovePlayerClientRpc(ulong clientId)
        {
            if (!IsServer)
            {
                if (clientsInRoom.ContainsKey(clientId))
                {
                    GameObject playerView = clientsInRoom[clientId].playerView;
                    clientsInRoom.Remove(clientId);
                    Destroy(playerView);
                }

                GenerateUserStatsForRoom();
            }
        }

        /// <summary>
        ///     OnClientIsReadyServerRpc
        ///     Sent to the server when the player clicks the ready button
        /// </summary>
        /// <param name="clientid">clientId that is ready</param>
        [ServerRpc(RequireOwnership = false)]
        private void OnClientIsReadyServerRpc(bool isReady, ServerRpcParams serverRpcParams = default)
        {
            var clientId = serverRpcParams.Receive.SenderClientId;
            if (clientsInRoom.ContainsKey(clientId))
            {
                PlayerData data = clientsInRoom[clientId];
                data.isReady = isReady;
                clientsInRoom[clientId] = data;
                SendClientReadyStatusUpdatesClientRpc(clientId, isReady);
                GenerateUserStatsForRoom();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void SendPlayerDataToServerRpc(string playerName, ushort carId, ServerRpcParams serverRpcParams = default)
        {
            var clientId = serverRpcParams.Receive.SenderClientId;
            if (clientsInRoom.ContainsKey(clientId))
            {
                var data = clientsInRoom[clientId];
                data.playerName = playerName;
                data.carId = carId;
                clientsInRoom[clientId] = data;
                GenerateUserStatsForRoom();
            }
        }

        [ServerRpc]
        private void SendChangedCarToServerRpc(ushort carId, ServerRpcParams serverRpcParams = default)
        {
            var clientId = serverRpcParams.Receive.SenderClientId;
            if (clientsInRoom.ContainsKey(clientId))
            {
                var data = clientsInRoom[clientId];
                data.carId = carId;
                clientsInRoom[clientId] = data;
                GenerateUserStatsForRoom();
            }
        }

        //Buttons

        /// <summary>
        ///     CheckForAllPlayersReady
        ///     Checks to see if all players are ready, and if so launches the game
        /// </summary>
        public void CheckForAllPlayersReady()
        {
            if (allPlayersInRoom)
            {
                var allPlayersAreReady = true;
                foreach (var clientRoomStatus in clientsInRoom)
                    if (!clientRoomStatus.Value.isReady)
                        //If some clients are still loading into the Room scene then this is false
                        allPlayersAreReady = false;

                //Only if all players are ready
                if (allPlayersAreReady)
                {
                    GameManager.instance.ClearDriverList();
                    ushort i = 0;
                    foreach (var id in clientsInRoom)
                    {
                        i++;
                        GameManager.instance.AddDriverToList(i, id.Value.playerName, id.Value.carId, false, id.Key);
                    }

                    //Remove our client connected callback
                    NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;

                    //Remove our client disconnected callback
                    NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;

                    //Remove our scene loaded callback
                    SceneTransitionHandler.sceneTransitionHandler.OnClientLoadedScene -= ClientLoadedScene;

                    //Transition to the ingame scene
                    SceneTransitionHandler.sceneTransitionHandler.SwitchScene(GameManager.instance.map.Scene);
                }
                else
                {
                    Debug.Log("All player is not ready");
                }
            }
            Debug.Log("Need at least " + minimumPlayerCount + " to start the game!");
        }

        /// <summary>
        ///     PlayerIsReady
        ///     Tied to the Ready button in the Room scene
        /// </summary>
        public void PlayerIsReady()
        {
            PlayerData data = clientsInRoom[NetworkManager.Singleton.LocalClientId];
            data.isReady = !data.isReady;
            clientsInRoom[NetworkManager.Singleton.LocalClientId] = data;
            OnClientIsReadyServerRpc(data.isReady);
        }

        public void LeaveRoom()
        {
            if (IsServer)
                SceneTransitionHandler.sceneTransitionHandler.CancelCallbacks();

            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene("Lobby");
        }
    
        public void OnConfirmCarChanged()
        {
            var carId = FindAnyObjectByType<CarSelection>().GetCarIDData();
            SendChangedCarToServerRpc(carId);
        }
    }
}