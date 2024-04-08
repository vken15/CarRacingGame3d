using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace CarRacingGame3d
{
    public class LoadingProgressManager : NetworkBehaviour
    {
        [SerializeField]
        GameObject progressTrackerPrefab;

        /// <summary>
        /// Dictionary containing references to the NetworkedLoadingProgessTrackers that contain the loading progress of
        /// each client. Keys are ClientIds.
        /// </summary>
        public Dictionary<ulong, NetworkedLoadingProgressTracker> ProgressTrackers { get; } = new Dictionary<ulong, NetworkedLoadingProgressTracker>();

        /// <summary>
        /// This is the AsyncOperation of the current load operation. This property should be set each time a new
        /// loading operation begins.
        /// </summary>
        public AsyncOperation LocalLoadOperation
        {
            set
            {
                isLoading = true;
                LocalProgress = 0;
                localLoadOperation = value;
            }
        }

        AsyncOperation localLoadOperation;

        float localProgress;

        bool isLoading;

        /// <summary>
        /// This event is invoked each time the dictionary of progress trackers is updated (if one is removed or added, for example.)
        /// </summary>
        public event Action OnTrackersUpdated;

        /// <summary>
        /// The current loading progress for the local client. Handled by a local field if not in a networked session,
        /// or by a progress tracker from the dictionary.
        /// </summary>
        public float LocalProgress
        {
            get => IsSpawned && ProgressTrackers.ContainsKey(NetworkManager.LocalClientId) ?
                ProgressTrackers[NetworkManager.LocalClientId].Progress.Value : localProgress;
            private set
            {
                if (IsSpawned && ProgressTrackers.ContainsKey(NetworkManager.LocalClientId) && ProgressTrackers[NetworkManager.LocalClientId].IsSpawned)
                {
                    ProgressTrackers[NetworkManager.LocalClientId].Progress.Value = value;
                }
                else
                {
                    localProgress = value;
                }
            }
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                NetworkManager.OnClientConnectedCallback += AddTracker;
                NetworkManager.OnClientDisconnectCallback += RemoveTracker;
                AddTracker(NetworkManager.LocalClientId);
            }
        }
        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                NetworkManager.OnClientConnectedCallback -= AddTracker;
                NetworkManager.OnClientDisconnectCallback -= RemoveTracker;
            }
            ProgressTrackers.Clear();
            OnTrackersUpdated?.Invoke();
        }

        void Update()
        {
            if (localLoadOperation != null && isLoading)
            {
                if (localLoadOperation.isDone)
                {
                    isLoading = false;
                    LocalProgress = 1;
                }
                else
                {
                    LocalProgress = localLoadOperation.progress;
                }
            }
        }

        [ClientRpc]
        void UpdateTrackersClientRpc()
        {
            if (!IsHost)
            {
                ProgressTrackers.Clear();
                foreach (var tracker in FindObjectsOfType<NetworkedLoadingProgressTracker>())
                {
                    // If a tracker is despawned but not destroyed yet, don't add it
                    if (tracker.IsSpawned)
                    {
                        ProgressTrackers[tracker.OwnerClientId] = tracker;
                        if (tracker.OwnerClientId == NetworkManager.LocalClientId)
                        {
                            LocalProgress = Mathf.Max(localProgress, LocalProgress);
                        }
                    }
                }
            }
            OnTrackersUpdated?.Invoke();
        }

        void AddTracker(ulong clientId)
        {
            if (IsServer)
            {
                var tracker = Instantiate(progressTrackerPrefab);
                var networkObject = tracker.GetComponent<NetworkObject>();
                networkObject.SpawnWithOwnership(clientId);
                ProgressTrackers[clientId] = tracker.GetComponent<NetworkedLoadingProgressTracker>();
                UpdateTrackersClientRpc();
            }
        }

        void RemoveTracker(ulong clientId)
        {
            if (IsServer)
            {
                if (ProgressTrackers.ContainsKey(clientId))
                {
                    var tracker = ProgressTrackers[clientId];
                    ProgressTrackers.Remove(clientId);
                    tracker.NetworkObject.Despawn();
                    UpdateTrackersClientRpc();
                }
            }
        }
    }
}
