using System;
using Unity.Netcode;
using UnityEngine;

namespace CarRacingGame3d
{
    /// <summary>
    /// Connection state corresponding to a host starting up. Starts the host when entering the state. If successful,
    /// transitions to the Hosting state, if not, transitions back to the Offline state.
    /// </summary>
    class StartingHostState : OnlineState
    {
        ConnectionMethodBase m_ConnectionMethod;

        public StartingHostState Configure(ConnectionMethodBase baseConnectionMethod)
        {
            m_ConnectionMethod = baseConnectionMethod;
            return this;
        }

        public override void Enter()
        {
            StartHost();
        }

        public override void Exit() { }

        public override void OnServerStarted()
        {
            //m_ConnectStatusPublisher.Publish(ConnectStatus.Success);
            ConnectionStatusMessageUIManager.instance.OnConnectStatus(ConnectStatus.Success);
            ConnectionManager.instance.ChangeState(ConnectionManager.instance.m_Hosting);
        }

        public override void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            var connectionData = request.Payload;
            var clientId = request.ClientNetworkId;
            // This happens when starting as a host, before the end of the StartHost call. In that case, we simply approve ourselves.
            if (clientId == ConnectionManager.instance.NetworkManager.LocalClientId)
            {
                var payload = System.Text.Encoding.UTF8.GetString(connectionData);
                var connectionPayload = JsonUtility.FromJson<ConnectionPayload>(payload); // https://docs.unity3d.com/2020.2/Documentation/Manual/JSONSerialization.html

                Debug.Log(clientId + " " + connectionPayload.playerId + " " + connectionPayload.playerName);

                SessionManager<SessionPlayerData>.Instance.SetupConnectingPlayerSessionData(clientId, connectionPayload.playerId,
                    new SessionPlayerData(clientId, connectionPayload.playerName, true));

                // connection approval will create a player object for you
                response.Approved = true;
                response.CreatePlayerObject = true;
            }
        }

        public override void OnServerStopped()
        {
            StartHostFailed();
        }

        async void StartHost()
        {
            try
            {
                await m_ConnectionMethod.SetupHostConnectionAsync();

                // NGO's StartHost launches everything
                if (!ConnectionManager.instance.NetworkManager.StartHost())
                {
                    StartHostFailed();
                }
            }
            catch (Exception)
            {
                StartHostFailed();
                throw;
            }
        }

        void StartHostFailed()
        {
            //m_ConnectStatusPublisher.Publish(ConnectStatus.StartHostFailed);
            ConnectionStatusMessageUIManager.instance.OnConnectStatus(ConnectStatus.StartHostFailed);
            ConnectionManager.instance.ChangeState(ConnectionManager.instance.m_Offline);
        }
    }
}
