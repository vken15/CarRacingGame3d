using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace CarRacingGame3d
{
    public class RoomChat : MonoBehaviour
    {
        [SerializeField] private ChatMessage chatMessagePrefab;
        [SerializeField] private Transform chatBoxTransform;
        [SerializeField] private TMP_InputField chatInput;

        [SerializeField] NetworkRoom networkRoom;
        [SerializeField] NetcodeHooks netcodeHooks;

        private const int maxNumberOfMessagesInList = 20;
        private List<ChatMessage> messages = new();

        private const float minIntervalBetweenChatMessages = 1f;
        private float clientSendTimer;
        private Controls controls;

        private void Awake()
        {
            netcodeHooks.OnNetworkSpawnHook += OnNetworkSpawn;
            netcodeHooks.OnNetworkDespawnHook += OnNetworkDespawn;
        }

        private void Start()
        {
            controls = InputManager.instance.Controllers;
        }

        void OnNetworkSpawn()
        {
            networkRoom.OnChatSent += AddMessage;
        }

        void OnNetworkDespawn()
        {
            if (networkRoom)
            {
                networkRoom.OnChatSent -= AddMessage;
            }
            if (netcodeHooks)
            {
                netcodeHooks.OnNetworkSpawnHook -= OnNetworkSpawn;
                netcodeHooks.OnNetworkDespawnHook -= OnNetworkDespawn;
            }
        }

        private void Update()
        {
            clientSendTimer += Time.deltaTime;

            if (controls.Player.Chat.WasPressedThisFrame())
            {
                if (chatInput.text.Length > 0 && clientSendTimer > minIntervalBetweenChatMessages)
                {
                    SendChatMessage();
                    chatInput.DeactivateInputField(true);
                } else
                {
                    chatInput.Select();
                    chatInput.ActivateInputField();
                }
            }
        }

        public void SendChatMessage()
        {
            string message = chatInput.text;
            chatInput.text = "";

            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            clientSendTimer = 0;

            networkRoom.SendChatMessageServerRpc(message, NetworkManager.Singleton.LocalClientId);
        }

        int FindLobbyPlayerId(ulong clientId)
        {
            for (int i = 0; i < networkRoom.LobbyPlayers.Count; ++i)
            {
                if (networkRoom.LobbyPlayers[i].ClientId == clientId)
                    return i;
            }
            return -1;
        }

        private void AddMessage(string message, ulong senderPlayerId)
        {
            var msg = Instantiate(chatMessagePrefab, chatBoxTransform);

            int id = FindLobbyPlayerId(senderPlayerId);
            if (id == -1)
            {
                throw new Exception($"AddMessage: client ID {senderPlayerId} is not a lobby player and shouldn't be here!");
            }

            NetworkRoom.LobbyPlayerState player = networkRoom.LobbyPlayers[id];
            msg.SetMessage(player.PlayerName, player.PlayerNumber, message);

            messages.Add(msg);

            if (message.Length > maxNumberOfMessagesInList)
            {
                Destroy(messages[0]);
                messages.RemoveAt(0);
            }
        }
    }
}