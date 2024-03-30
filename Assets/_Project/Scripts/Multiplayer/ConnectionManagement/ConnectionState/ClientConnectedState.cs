using UnityEngine;

namespace CarRacingGame3d
{
    /// <summary>
    /// Connection state corresponding to a connected client. When being disconnected, transitions to the
    /// ClientReconnecting state if no reason is given, or to the Offline state.
    /// </summary>
    class ClientConnectedState : OnlineState
    {
        public override void Enter()
        {
            if (LobbyServiceFacade.Instance.CurrentUnityLobby != null)
            {
                LobbyServiceFacade.Instance.BeginTracking();
            }
        }

        public override void Exit() { }

        public override void OnClientDisconnect(ulong _)
        {
            var disconnectReason = ConnectionManager.instance.NetworkManager.DisconnectReason;
            if (string.IsNullOrEmpty(disconnectReason))
            {
                ConnectionStatusMessageUIManager.instance.OnConnectStatus(ConnectStatus.Reconnecting);
                ConnectionManager.instance.ChangeState(ConnectionManager.instance.m_ClientReconnecting);
            }
            else
            {
                var connectStatus = JsonUtility.FromJson<ConnectStatus>(disconnectReason);
                ConnectionStatusMessageUIManager.instance.OnConnectStatus(connectStatus);
                ConnectionManager.instance.ChangeState(ConnectionManager.instance.m_Offline);
            }
        }
    }
}
