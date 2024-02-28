using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace CarRacingGame3d
{
    public class RoomControl : NetworkBehaviour
    {
        [SerializeField]
        private string m_InGameSceneName = "Online";

        // Minimum player count required to transition to next level
        [SerializeField]
        private int m_MinimumPlayerCount = 2;

        public TMP_Text RoomText;
        private bool m_AllPlayersInRoom;

        private Dictionary<ulong, bool> m_ClientsInRoom;
        private string m_UserRoomStatusText;

        private void Awake()
        {
            //m_InGameSceneName = GameManager.instance.GetMapScene();
        }

        public override void OnNetworkSpawn()
        {
            m_ClientsInRoom = new Dictionary<ulong, bool>
        {
            //Always add ourselves to the list at first
            { NetworkManager.LocalClientId, false }
        };

            //If we are hosting, then handle the server side for detecting when clients have connected
            //and when their room scenes are finished loading.
            if (IsServer)
            {
                m_AllPlayersInRoom = false;

                m_ClientsInRoom[NetworkManager.LocalClientId] = true;

                //Server will be notified when a client connects
                NetworkManager.OnClientConnectedCallback += OnClientConnectedCallback;
                SceneTransitionHandler.sceneTransitionHandler.OnClientLoadedScene += ClientLoadedScene;
            }

            //Update our room
            GenerateUserStatsForRoom();

            SceneTransitionHandler.sceneTransitionHandler.SetSceneState(SceneTransitionHandler.SceneStates.Lobby);
        }

        private void OnGUI()
        {
            if (RoomText != null) RoomText.text = m_UserRoomStatusText;
        }

        /// <summary>
        ///     GenerateUserStatsForRoom
        ///     Psuedo code for setting player state
        ///     Just updating a text field, this could use a lot of "refactoring"
        /// </summary>
        private void GenerateUserStatsForRoom()
        {
            m_UserRoomStatusText = string.Empty;
            foreach (var clientRoomStatus in m_ClientsInRoom)
            {
                m_UserRoomStatusText += "PLAYER_" + clientRoomStatus.Key + "          ";
                if (clientRoomStatus.Value)
                    m_UserRoomStatusText += "(READY)\n";
                else
                    m_UserRoomStatusText += "(NOT READY)\n";
            }
        }

        /// <summary>
        ///     UpdateAndCheckPlayersInRoom
        ///     Checks to see if we have at least 2 or more people to start
        /// </summary>
        private void UpdateAndCheckPlayersInRoom()
        {
            m_AllPlayersInRoom = m_ClientsInRoom.Count >= m_MinimumPlayerCount;

            foreach (var clientRoomStatus in m_ClientsInRoom)
            {
                SendClientReadyStatusUpdatesClientRpc(clientRoomStatus.Key, clientRoomStatus.Value);
                if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(clientRoomStatus.Key))

                    //If some clients are still loading into the Room scene then this is false
                    m_AllPlayersInRoom = false;
            }

            //CheckForAllPlayersReady();
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
                if (!m_ClientsInRoom.ContainsKey(clientId))
                {
                    m_ClientsInRoom.Add(clientId, false);
                    GenerateUserStatsForRoom();
                }

                UpdateAndCheckPlayersInRoom();
            }
        }

        /// <summary>
        ///     OnClientConnectedCallback
        ///     Since we are entering a Room and Netcode's NetworkManager is spawning the player,
        ///     the server can be configured to only listen for connected clients at this stage.
        /// </summary>
        /// <param name="clientId">client that connected</param>
        private void OnClientConnectedCallback(ulong clientId)
        {
            if (IsServer)
            {
                if (!m_ClientsInRoom.ContainsKey(clientId)) m_ClientsInRoom.Add(clientId, false);
                GenerateUserStatsForRoom();

                UpdateAndCheckPlayersInRoom();
            }
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
                if (!m_ClientsInRoom.ContainsKey(clientId))
                    m_ClientsInRoom.Add(clientId, isReady);
                else
                    m_ClientsInRoom[clientId] = isReady;
                GenerateUserStatsForRoom();
            }
        }

        /// <summary>
        ///     CheckForAllPlayersReady
        ///     Checks to see if all players are ready, and if so launches the game
        /// </summary>
        public void CheckForAllPlayersReady()
        {
            if (m_AllPlayersInRoom)
            {
                var allPlayersAreReady = true;
                foreach (var clientRoomStatus in m_ClientsInRoom)
                    if (!clientRoomStatus.Value)
                        //If some clients are still loading into the Room scene then this is false
                        allPlayersAreReady = false;

                //Only if all players are ready
                if (allPlayersAreReady)
                {
                    GameManager.instance.ClearDriverList();
                    int i = 0;
                    foreach (var id in m_ClientsInRoom)
                    {
                        i++;
                        GameManager.instance.AddDriverToList(i, "Test" + i, 1, false, AIDifficult.Easy, id.Key);
                    }

                    //Remove our client connected callback
                    NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;

                    //Remove our scene loaded callback
                    SceneTransitionHandler.sceneTransitionHandler.OnClientLoadedScene -= ClientLoadedScene;

                    //Transition to the ingame scene
                    SceneTransitionHandler.sceneTransitionHandler.SwitchScene(m_InGameSceneName);
                }
                else
                {
                    Debug.Log("All player is not ready");
                }
            }
            Debug.Log("Need at least " + m_MinimumPlayerCount + " to start the game!");
        }

        /// <summary>
        ///     PlayerIsReady
        ///     Tied to the Ready button in the Room scene
        /// </summary>
        public void PlayerIsReady()
        {
            m_ClientsInRoom[NetworkManager.Singleton.LocalClientId] = !m_ClientsInRoom[NetworkManager.Singleton.LocalClientId];
            if (IsServer)
            {
                UpdateAndCheckPlayersInRoom();
            }
            else
            {
                OnClientIsReadyServerRpc(NetworkManager.Singleton.LocalClientId);
            }

            GenerateUserStatsForRoom();
        }

        /// <summary>
        ///     OnClientIsReadyServerRpc
        ///     Sent to the server when the player clicks the ready button
        /// </summary>
        /// <param name="clientid">clientId that is ready</param>
        [ServerRpc(RequireOwnership = false)]
        private void OnClientIsReadyServerRpc(ulong clientid)
        {
            if (m_ClientsInRoom.ContainsKey(clientid))
            {
                m_ClientsInRoom[clientid] = true;
                UpdateAndCheckPlayersInRoom();
                GenerateUserStatsForRoom();
            }
        }
    }
}