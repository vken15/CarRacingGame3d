using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

namespace CarRacingGame3d
{
    public struct UnityServiceErrorMessage
    {
        public enum Service
        {
            Authentication,
            Lobby,
        }

        public string Title;
        public string Message;
        public Service AffectedService;
        public Exception OriginalException;

        public UnityServiceErrorMessage(string title, string message, Service service, Exception originalException = null)
        {
            Title = title;
            Message = message;
            AffectedService = service;
            OriginalException = originalException;
        }
    }

    public struct LobbyListFetchedMessage
    {
        public readonly IReadOnlyList<LocalLobby> LocalLobbies;

        public LobbyListFetchedMessage(List<LocalLobby> localLobbies)
        {
            LocalLobbies = localLobbies;
        }
    }

    /// <summary>
    /// An abstraction layer between the direct calls into the Lobby API and the outcomes you actually want.
    /// </summary>

    public class LobbyServiceFacade : MonoBehaviour
    {
        public static LobbyServiceFacade Instance = null;

        UpdateRunner m_UpdateRunner;

        const float k_HeartbeatPeriod = 8; // The heartbeat must be rate-limited to 5 calls per 30 seconds. We'll aim for longer in case periods don't align.
        float m_HeartbeatTime = 0;

        RateLimitCooldown m_RateLimitQuery;
        RateLimitCooldown m_RateLimitJoin;
        RateLimitCooldown m_RateLimitQuickJoin;
        RateLimitCooldown m_RateLimitHost;

        public Lobby CurrentUnityLobby { get; private set; }

        ILobbyEvents m_LobbyEvents;

        bool m_IsTracking = false;

        LobbyEventConnectionState m_LobbyEventConnectionState = LobbyEventConnectionState.Unknown;

        public event Action<LobbyListFetchedMessage> OnLobbyListFetched;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            m_UpdateRunner = FindAnyObjectByType<UpdateRunner>();

            //See https://docs.unity.com/lobby/rate-limits.html
            m_RateLimitQuery = new RateLimitCooldown(1f);
            m_RateLimitJoin = new RateLimitCooldown(3f);
            m_RateLimitQuickJoin = new RateLimitCooldown(10f);
            m_RateLimitHost = new RateLimitCooldown(3f);

            Application.wantsToQuit += OnWantToQuit;
        }

        void OnDestroy()
        {
            EndTracking();
        }

        public void SetRemoteLobby(Lobby lobby)
        {
            CurrentUnityLobby = lobby;
            LocalLobby.Instance.ApplyRemoteData(lobby);
        }

        /// <summary>
        /// Initiates tracking of joined lobby's events. The host also starts sending heartbeat pings here.
        /// </summary>
        public void BeginTracking()
        {
            if (!m_IsTracking)
            {
                m_IsTracking = true;
                SubscribeToJoinedLobbyAsync();

                // Only the host sends heartbeat pings to the service to keep the lobby alive
                if (LocalLobbyUser.Instance.IsHost)
                {
                    m_HeartbeatTime = 0;
                    m_UpdateRunner.Subscribe(DoLobbyHeartbeat, 1.5f);
                }
            }
        }

        /// <summary>
        /// Ends tracking of joined lobby's events and leaves or deletes the lobby. The host also stops sending heartbeat pings here.
        /// </summary>
        public void EndTracking()
        {
            if (m_IsTracking)
            {
                m_IsTracking = false;
                UnsubscribeToJoinedLobbyAsync();

                // Only the host sends heartbeat pings to the service to keep the lobby alive
                if (LocalLobbyUser.Instance.IsHost)
                {
                    m_UpdateRunner.Unsubscribe(DoLobbyHeartbeat);
                }
            }

            if (CurrentUnityLobby != null)
            {
                if (LocalLobbyUser.Instance.IsHost)
                {
                    DeleteLobbyAsync();
                }
                else
                {
                    LeaveLobbyAsync();
                }
            }
        }

        /// <summary>
        /// Attempt to create a new lobby and then join it.
        /// </summary>
        public async Task<(bool Success, Lobby Lobby)> TryCreateLobbyAsync(string lobbyName, int maxPlayers, bool isPrivate)
        {
            if (!m_RateLimitHost.CanCall)
            {
                Debug.LogWarning("Create Lobby hit the rate limit.");
                return (false, null);
            }

            try
            {
                var lobby = await LobbyAPIInterface.Instance.CreateLobby(AuthenticationService.Instance.PlayerId, lobbyName, maxPlayers, isPrivate, LocalLobbyUser.Instance.GetDataForUnityServices(), null);
                return (true, lobby);
            }
            catch (LobbyServiceException e)
            {
                if (e.Reason == LobbyExceptionReason.RateLimited)
                {
                    m_RateLimitHost.PutOnCooldown();
                }
                else
                {
                    PublishError(e);
                }
            }

            return (false, null);
        }

        /// <summary>
        /// Attempt to join an existing lobby. Will try to join via code, if code is null - will try to join via ID.
        /// </summary>
        public async Task<(bool Success, Lobby Lobby)> TryJoinLobbyAsync(string lobbyId, string lobbyCode)
        {
            if (!m_RateLimitJoin.CanCall ||
                (lobbyId == null && lobbyCode == null))
            {
                Debug.LogWarning("Join Lobby hit the rate limit.");
                return (false, null);
            }

            try
            {
                if (!string.IsNullOrEmpty(lobbyCode))
                {
                    var lobby = await LobbyAPIInterface.Instance.JoinLobbyByCode(AuthenticationService.Instance.PlayerId, lobbyCode, LocalLobbyUser.Instance.GetDataForUnityServices());
                    return (true, lobby);
                }
                else
                {
                    var lobby = await LobbyAPIInterface.Instance.JoinLobbyById(AuthenticationService.Instance.PlayerId, lobbyId, LocalLobbyUser.Instance.GetDataForUnityServices());
                    return (true, lobby);
                }
            }
            catch (LobbyServiceException e)
            {
                if (e.Reason == LobbyExceptionReason.RateLimited)
                {
                    m_RateLimitJoin.PutOnCooldown();
                }
                else
                {
                    PublishError(e);
                }
            }

            return (false, null);
        }

        /// <summary>
        /// Attempt to join the first lobby among the available lobbies that match the filtered onlineMode.
        /// </summary>
        public async Task<(bool Success, Lobby Lobby)> TryQuickJoinLobbyAsync()
        {
            if (!m_RateLimitQuickJoin.CanCall)
            {
                Debug.LogWarning("Quick Join Lobby hit the rate limit.");
                return (false, null);
            }

            try
            {
                var lobby = await LobbyAPIInterface.Instance.QuickJoinLobby(AuthenticationService.Instance.PlayerId, LocalLobbyUser.Instance.GetDataForUnityServices());
                return (true, lobby);
            }
            catch (LobbyServiceException e)
            {
                if (e.Reason == LobbyExceptionReason.RateLimited)
                {
                    m_RateLimitQuickJoin.PutOnCooldown();
                }
                else
                {
                    PublishError(e);
                }
            }

            return (false, null);
        }

        void ResetLobby()
        {
            CurrentUnityLobby = null;
            if (LocalLobbyUser.Instance != null)
            {
                LocalLobbyUser.Instance.ResetState();
            }
            if (LocalLobby.Instance != null)
            {
                LocalLobby.Instance.Reset(LocalLobbyUser.Instance);
            }

            // no need to disconnect Netcode, it should already be handled by Netcode's callback to disconnect
        }

        void OnLobbyChanges(ILobbyChanges changes)
        {
            if (changes.LobbyDeleted)
            {
                Debug.Log("Lobby deleted");
                ResetLobby();
                EndTracking();
            }
            else
            {
                Debug.Log("Lobby updated");
                changes.ApplyToLobby(CurrentUnityLobby);
                LocalLobby.Instance.ApplyRemoteData(CurrentUnityLobby);

                // as client, check if host is still in lobby
                if (!LocalLobbyUser.Instance.IsHost)
                {
                    foreach (var lobbyUser in LocalLobby.Instance.LobbyUsers)
                    {
                        if (lobbyUser.Value.IsHost)
                        {
                            return;
                        }
                    }

                    ConnectionStatusMessageUIManager.instance.ServiceErrorHandler(new UnityServiceErrorMessage("Host left the lobby", "Disconnecting.", UnityServiceErrorMessage.Service.Lobby));
                    EndTracking();
                    // no need to disconnect Netcode, it should already be handled by Netcode's callback to disconnect
                }
            }
        }

        void OnKickedFromLobby()
        {
            Debug.Log("Kicked from Lobby");
            ResetLobby();
            EndTracking();
        }

        void OnLobbyEventConnectionStateChanged(LobbyEventConnectionState lobbyEventConnectionState)
        {
            m_LobbyEventConnectionState = lobbyEventConnectionState;
            Debug.Log($"LobbyEventConnectionState changed to {lobbyEventConnectionState}");
        }

        async void SubscribeToJoinedLobbyAsync()
        {
            var lobbyEventCallbacks = new LobbyEventCallbacks();
            lobbyEventCallbacks.LobbyChanged += OnLobbyChanges;
            lobbyEventCallbacks.KickedFromLobby += OnKickedFromLobby;
            lobbyEventCallbacks.LobbyEventConnectionStateChanged += OnLobbyEventConnectionStateChanged;
            // The LobbyEventCallbacks object created here will now be managed by the Lobby SDK. The callbacks will be
            // unsubscribed from when we call UnsubscribeAsync on the ILobbyEvents object we receive and store here.
            m_LobbyEvents = await LobbyAPIInterface.Instance.SubscribeToLobby(LocalLobby.Instance.LobbyID, lobbyEventCallbacks);
        }

        async void UnsubscribeToJoinedLobbyAsync()
        {
            if (m_LobbyEvents != null && m_LobbyEventConnectionState != LobbyEventConnectionState.Unsubscribed)
            {
#if UNITY_EDITOR
                //try
                //{
                //    await m_LobbyEvents.UnsubscribeAsync();
                //}
                //catch (WebSocketException e)
                //{
                //    // This exception occurs in the editor when exiting play mode without first leaving the lobby.
                //    // This is because Wire closes the websocket internally when exiting playmode in the editor.
                //    Debug.Log(e.Message);
                //}
                await m_LobbyEvents.UnsubscribeAsync();
#else
                await m_LobbyEvents.UnsubscribeAsync();
#endif
            }
        }

        /// <summary>
        /// Used for getting the list of all active lobbies, without needing full info for each.
        /// </summary>
        public async Task RetrieveAndPublishLobbyListAsync()
        {
            if (!m_RateLimitQuery.CanCall)
            {
                Debug.LogWarning("Retrieve Lobby list hit the rate limit. Will try again soon...");
                return;
            }

            try
            {
                var response = await LobbyAPIInterface.Instance.QueryAllLobbies();
                Debug.Log("Fetched Lobbies");
                OnLobbyListFetched?.Invoke(new LobbyListFetchedMessage(LocalLobby.CreateLocalLobbies(response)));
            }
            catch (LobbyServiceException e)
            {
                if (e.Reason == LobbyExceptionReason.RateLimited)
                {
                    m_RateLimitQuery.PutOnCooldown();
                }
                else
                {
                    PublishError(e);
                }
            }
        }

        public async Task<Lobby> ReconnectToLobbyAsync()
        {
            try
            {
                return await LobbyAPIInterface.Instance.ReconnectToLobby(LocalLobby.Instance.LobbyID);
            }
            catch (LobbyServiceException e)
            {
                // If Lobby is not found and if we are not the host, it has already been deleted. No need to publish the error here.
                if (e.Reason != LobbyExceptionReason.LobbyNotFound && !LocalLobbyUser.Instance.IsHost)
                {
                    PublishError(e);
                }
            }

            return null;
        }

        /// <summary>
        /// Attempt to leave a lobby
        /// </summary>
        async void LeaveLobbyAsync()
        {
            string uasId = AuthenticationService.Instance.PlayerId;
            try
            {
                await LobbyAPIInterface.Instance.RemovePlayerFromLobby(uasId, LocalLobby.Instance.LobbyID);
            }
            catch (LobbyServiceException e)
            {
                // If Lobby is not found and if we are not the host, it has already been deleted. No need to publish the error here.
                if (e.Reason != LobbyExceptionReason.LobbyNotFound && !LocalLobbyUser.Instance.IsHost)
                {
                    PublishError(e);
                }
            }
            finally
            {
                ResetLobby();
            }

        }

        public async void RemovePlayerFromLobbyAsync(string uasId)
        {
            if (LocalLobbyUser.Instance.IsHost)
            {
                try
                {
                    await LobbyAPIInterface.Instance.RemovePlayerFromLobby(uasId, LocalLobby.Instance.LobbyID);
                }
                catch (LobbyServiceException e)
                {
                    PublishError(e);
                }
            }
            else
            {
                Debug.LogError("Only the host can remove other players from the lobby.");
            }
        }

        async void DeleteLobbyAsync()
        {
            if (LocalLobbyUser.Instance.IsHost)
            {
                try
                {
                    await LobbyAPIInterface.Instance.DeleteLobby(LocalLobby.Instance.LobbyID);
                }
                catch (LobbyServiceException e)
                {
                    PublishError(e);
                }
                finally
                {
                    ResetLobby();
                }
            }
            else
            {
                Debug.LogError("Only the host can delete a lobby.");
            }
        }

        /// <summary>
        /// Attempt to push a set of key-value pairs associated with the local player which will overwrite any existing
        /// data for these keys. Lobby can be provided info about Relay (or any other remote allocation) so it can add
        /// automatic disconnect handling.
        /// </summary>
        public async Task UpdatePlayerDataAsync(string allocationId, string connectionInfo)
        {
            if (!m_RateLimitQuery.CanCall)
            {
                return;
            }

            try
            {
                var result = await LobbyAPIInterface.Instance.UpdatePlayer(CurrentUnityLobby.Id, AuthenticationService.Instance.PlayerId, LocalLobbyUser.Instance.GetDataForUnityServices(), allocationId, connectionInfo);

                if (result != null)
                {
                    CurrentUnityLobby = result; // Store the most up-to-date lobby now since we have it, instead of waiting for the next heartbeat.
                }
            }
            catch (LobbyServiceException e)
            {
                if (e.Reason == LobbyExceptionReason.RateLimited)
                {
                    m_RateLimitQuery.PutOnCooldown();
                }
                else if (e.Reason != LobbyExceptionReason.LobbyNotFound && !LocalLobbyUser.Instance.IsHost) // If Lobby is not found and if we are not the host, it has already been deleted. No need to publish the error here.
                {
                    PublishError(e);
                }
            }
        }

        /// <summary>
        /// Attempt to update the set of key-value pairs associated with a given lobby and unlocks it so clients can see it.
        /// </summary>
        public async Task UpdateLobbyDataAndUnlockAsync()
        {
            if (!m_RateLimitQuery.CanCall)
            {
                return;
            }

            var localData = LocalLobby.Instance.GetDataForUnityServices();

            var dataCurr = CurrentUnityLobby.Data;
            if (dataCurr == null)
            {
                dataCurr = new Dictionary<string, DataObject>();
            }

            foreach (var dataNew in localData)
            {
                if (dataCurr.ContainsKey(dataNew.Key))
                {
                    dataCurr[dataNew.Key] = dataNew.Value;
                }
                else
                {
                    dataCurr.Add(dataNew.Key, dataNew.Value);
                }
            }

            try
            {
                var result = await LobbyAPIInterface.Instance.UpdateLobby(CurrentUnityLobby.Id, dataCurr, shouldLock: false);

                if (result != null)
                {
                    CurrentUnityLobby = result;
                }
            }
            catch (LobbyServiceException e)
            {
                if (e.Reason == LobbyExceptionReason.RateLimited)
                {
                    m_RateLimitQuery.PutOnCooldown();
                }
                else
                {
                    PublishError(e);
                }
            }
        }

        /// <summary>
        /// Lobby requires a periodic ping to detect rooms that are still active, in order to mitigate "zombie" lobbies.
        /// </summary>
        void DoLobbyHeartbeat(float dt)
        {
            m_HeartbeatTime += dt;
            if (m_HeartbeatTime > k_HeartbeatPeriod)
            {
                m_HeartbeatTime -= k_HeartbeatPeriod;
                try
                {
                    LobbyAPIInterface.Instance.SendHeartbeatPing(CurrentUnityLobby.Id);
                }
                catch (LobbyServiceException e)
                {
                    // If Lobby is not found and if we are not the host, it has already been deleted. No need to publish the error here.
                    if (e.Reason != LobbyExceptionReason.LobbyNotFound && !LocalLobbyUser.Instance.IsHost)
                    {
                        PublishError(e);
                    }
                }
            }
        }

        void PublishError(LobbyServiceException e)
        {
            var reason = e.InnerException == null ? e.Message : $"{e.Message} ({e.InnerException.Message})"; // Lobby error type, then HTTP error type.
            ConnectionStatusMessageUIManager.instance.ServiceErrorHandler(new UnityServiceErrorMessage("Lobby Error", reason, UnityServiceErrorMessage.Service.Lobby, e));
        }

        private bool OnWantToQuit()
        {
            Application.wantsToQuit -= OnWantToQuit;

            var canQuit = LocalLobby.Instance != null && string.IsNullOrEmpty(LocalLobby.Instance.LobbyID);
            if (!canQuit)
            {
                StartCoroutine(LeaveBeforeQuit());
            }

            return canQuit;
        }

        /// <summary>
        ///     In builds, if we are in a lobby and try to send a Leave request on application quit, it won't go through if we're quitting on the same frame.
        ///     So, we need to delay just briefly to let the request happen (though we don't need to wait for the result).
        /// </summary>
        private IEnumerator LeaveBeforeQuit()
        {
            // We want to quit anyways, so if anything happens while trying to leave the Lobby, log the exception then carry on
            try
            {
                EndTracking();
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }

            yield return null;
            Application.Quit();
        }
    }
    
}
