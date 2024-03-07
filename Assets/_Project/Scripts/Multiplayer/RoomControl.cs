using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CarRacingGame3d
{
    public class RoomControl : NetworkBehaviour
    {
        [SerializeField] private string inGameSceneName = "Online";

        // Minimum player count required to transition to next level
        [SerializeField] private int minimumPlayerCount = 2;

        [SerializeField] private GridLayoutGroup layout;

        [SerializeField] private GameObject playerItem;

        private bool allPlayersInRoom;
        private Dictionary<ulong, bool> clientsInRoom;
        private Dictionary<ulong, GameObject> playerGroup;

        public override void OnNetworkSpawn()
        {
            clientsInRoom = new Dictionary<ulong, bool>()
            {
                //Always add ourselves to the list at first
                { NetworkManager.Singleton.LocalClientId, false }
            };

            GameObject player = Instantiate(playerItem, layout.transform);
            playerGroup = new Dictionary<ulong, GameObject>()
            {
                { NetworkManager.Singleton.LocalClientId, player }
            };

            //If we are hosting, then handle the server side for detecting when clients have connected
            //and when their room scenes are finished loading.
            if (IsServer)
            {
                allPlayersInRoom = false;

                clientsInRoom[NetworkManager.Singleton.LocalClientId] = true;

                //Server will be notified when a client connects
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
                SceneTransitionHandler.sceneTransitionHandler.OnClientLoadedScene += ClientLoadedScene;

                GenerateUserStatsForRoom();
            }
            else
            {
                NetworkManager.Singleton.OnClientDisconnectCallback += OnServerShutdown;
            }

            SceneTransitionHandler.sceneTransitionHandler.SetSceneState(SceneTransitionHandler.SceneStates.Lobby);
            base.OnNetworkSpawn();
        }

        private void OnGUI()
        {
            //if (RoomText != null) RoomText.text = userRoomStatusText;
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
                var userRoomStatusText = "PLAYER_" + clientRoomStatus.Key + "          ";
                if (clientRoomStatus.Value)
                    userRoomStatusText += "(READY)\n";
                else
                    userRoomStatusText += "(NOT READY)\n";

                playerGroup[clientRoomStatus.Key].GetComponentInChildren<TMP_Text>().text = userRoomStatusText;
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
                    SendClientReadyStatusUpdatesClientRpc(clientRoomStatus.Key, clientRoomStatus.Value);
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
                    clientsInRoom.Add(clientId, false);
                    GameObject player = Instantiate(playerItem, layout.transform);
                    playerGroup.Add(clientId, player);
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
                    clientsInRoom.Remove(clientId);
                    playerGroup.Remove(clientId);
                    GenerateUserStatsForRoom();
                    UpdateAndCheckPlayersInRoom();
                    RemovePlayerClientRpc(clientId);
                    Debug.Log(clientId + "disconnected!");
                }
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
                    clientsInRoom.Add(clientId, isReady);
                    GameObject player = Instantiate(playerItem, layout.transform);
                    playerGroup.Add(clientId, player);
                }
                else
                    clientsInRoom[clientId] = isReady;
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
                    clientsInRoom.Remove(clientId);
                    playerGroup.Remove(clientId);
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
                clientsInRoom[clientId] = isReady;
                SendClientReadyStatusUpdatesClientRpc(clientId, isReady);
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
                    if (!clientRoomStatus.Value)
                        //If some clients are still loading into the Room scene then this is false
                        allPlayersAreReady = false;

                //Only if all players are ready
                if (allPlayersAreReady)
                {
                    GameManager.instance.ClearDriverList();
                    int i = 0;
                    foreach (var id in clientsInRoom)
                    {
                        i++;
                        GameManager.instance.AddDriverToList(i, "Test" + i, 1, false, id.Key);
                    }

                    //Remove our client connected callback
                    NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;

                    //Remove our client disconnected callback
                    NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;

                    //Remove our scene loaded callback
                    SceneTransitionHandler.sceneTransitionHandler.OnClientLoadedScene -= ClientLoadedScene;

                    //Transition to the ingame scene
                    SceneTransitionHandler.sceneTransitionHandler.SwitchScene(inGameSceneName);
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
            clientsInRoom[NetworkManager.Singleton.LocalClientId] = !clientsInRoom[NetworkManager.Singleton.LocalClientId];
            OnClientIsReadyServerRpc(clientsInRoom[NetworkManager.Singleton.LocalClientId]);
        }

        public void LeaveRoom()
        {
            if (IsServer)
            {
                var discovery = FindAnyObjectByType<ExampleNetworkDiscovery>();
                if (discovery != null)
                {
                    discovery.StopServer();
                }
                SceneTransitionHandler.sceneTransitionHandler.CancelCallbacks();
            }
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene("Lobby");
        }
    }
}