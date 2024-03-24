using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CarRacingGame3d
{
    [RequireComponent(typeof(NetcodeHooks))]
    public class ClientRoomState : MonoBehaviour
    {
        [SerializeField] NetcodeHooks netcodeHooks;

        [SerializeField] NetworkRoom networkRoom;

        [SerializeField] List<PlayerSeatUIHandler> playerSeats;

        [SerializeField] Text numPlayersText;

        [SerializeField] private GameObject startBtn;
        [SerializeField] private GameObject readyBtn;
        [SerializeField] private GameObject leaveBtn;
        [SerializeField] private GameObject changeMapBtn;
        [SerializeField] private GameObject changeCarBtn;

        [SerializeField] private CarSelection carSelection;

        bool hasLocalPlayerLockedIn = false;
        ushort carId = 1;
        int maxPlayer = 8;

        private void Awake()
        {
            maxPlayer = ConnectionManager.instance.MaxConnectedPlayers;
            for (int i = 0; i < playerSeats.Count; ++i)
            {
                if (i < maxPlayer)
                {
                    playerSeats[i].Initialize(i);
                }
                else
                {
                    playerSeats[i].InitializeBlock(i);
                }
            }

            netcodeHooks.OnNetworkSpawnHook += OnNetworkSpawn;
            netcodeHooks.OnNetworkDespawnHook += OnNetworkDespawn;

            if (NetworkManager.Singleton.IsServer)
            {
                startBtn.SetActive(true);
                changeMapBtn.SetActive(true);
            }
            else
            {
                readyBtn.SetActive(true);
                Text btnName = readyBtn.GetComponentInChildren<Text>();
                btnName.text = "Ready";
                readyBtn.GetComponent<Button>().onClick.AddListener(() =>
                {
                    if (btnName.text.Equals("Ready"))
                    {
                        leaveBtn.GetComponent<Button>().interactable = false;
                        changeCarBtn.SetActive(false);
                        btnName.text = "Cancel";
                    }
                    else
                    {
                        leaveBtn.GetComponent<Button>().interactable = true;
                        changeCarBtn.SetActive(true);
                        btnName.text = "Ready";
                    }
                });
            }
        }

        void OnNetworkSpawn()
        {
            if (!NetworkManager.Singleton.IsClient)
            {
                enabled = false;
            }
            else
            {
                networkRoom.IsLobbyClosed.OnValueChanged += OnLobbyClosedChanged;
                networkRoom.LobbyPlayers.OnListChanged += OnLobbyPlayerStateChanged;
            }
        }

        void OnNetworkDespawn()
        {
            if (networkRoom)
            {
                networkRoom.IsLobbyClosed.OnValueChanged -= OnLobbyClosedChanged;
                networkRoom.LobbyPlayers.OnListChanged -= OnLobbyPlayerStateChanged;
            }
            if (netcodeHooks)
            {
                netcodeHooks.OnNetworkSpawnHook -= OnNetworkSpawn;
                netcodeHooks.OnNetworkDespawnHook -= OnNetworkDespawn;
            }
        }

        void UpdatePlayerCount()
        {
            int count = networkRoom.LobbyPlayers.Count;
            numPlayersText.text = $"Player: {count}/{ConnectionManager.instance.MaxConnectedPlayers}";
        }

        /// <summary>
        /// Called by the server when any of the seats in the lobby have changed. (Including ours!)
        /// </summary>
        void OnLobbyPlayerStateChanged(NetworkListEvent<NetworkRoom.LobbyPlayerState> changeEvent)
        {
            UpdateSeats();
            UpdatePlayerCount();
        }

        /// <summary>
        /// 
        /// </summary>
        void UpdateSeats()
        {
            NetworkRoom.LobbyPlayerState[] curSeats = new NetworkRoom.LobbyPlayerState[maxPlayer];
            foreach (NetworkRoom.LobbyPlayerState playerState in networkRoom.LobbyPlayers)
            {
                if (playerState.SeatId == -1 || playerState.SeatState == SeatState.Inactive)
                    continue; // this player isn't seated at all!
                
                curSeats[playerState.SeatId] = playerState;
            }

            // now actually update the seats in the UI
            for (int i = 0; i < maxPlayer; ++i)
            {
                playerSeats[i].SetState(curSeats[i].SeatState, curSeats[i].PlayerNumber, curSeats[i].PlayerName);
            }
        }

        /// <summary>
        /// Called by the server when the lobby closes (because all players are ready)
        /// </summary>
        void OnLobbyClosedChanged(bool wasLobbyClosed, bool isLobbyClosed)
        {
            if (isLobbyClosed)
            {
                startBtn.GetComponent<Button>().interactable = false;
                changeCarBtn.GetComponent<Button>().interactable = false;
                changeMapBtn.GetComponent<Button>().interactable = false;
                readyBtn.GetComponent<Button>().interactable = false;
                GameManager.instance.numberOfCarsRaceCompleteToEnd = networkRoom.LobbyPlayers.Count;
            }
        }

        //Buttons

        public void OnPlayerClickedReady()
        {
            if (networkRoom.IsSpawned)
            {
                // request to lock in or unlock if already locked in
                hasLocalPlayerLockedIn = !hasLocalPlayerLockedIn;
                networkRoom.ChangeSeatServerRpc(NetworkManager.Singleton.LocalClientId, carId, hasLocalPlayerLockedIn);
            }
        }

        public void OnLeaveRoom()
        {
            ConnectionManager.instance.RequestShutdown();
        }

        public void OnConfirmCarChanged()
        {
            carId = carSelection.GetCarIDData();
        }
    }
}
