using UnityEngine;
using UnityEngine.SceneManagement;

namespace CarRacingGame3d
{
    /// <summary>
    /// Connection state corresponding to when the NetworkManager is shut down. From this state we can transition to the
    /// ClientConnecting sate, if starting as a client, or the StartingHost state, if starting as a host.
    /// </summary>
    class OfflineState : ConnectionState
    {
        const string mainMenuSceneName = "MainMenu";
        const string lobbySceneName = "Lobby";
        const string onlineLobbySceneName = "OnlineLobby";

        public override void Enter()
        {
            LobbyServiceFacade.Instance.EndTracking();
            ConnectionManager.instance.NetworkManager.Shutdown();
            var name = SceneManager.GetActiveScene().name;
            if (name != mainMenuSceneName && name != lobbySceneName && name != onlineLobbySceneName)
            {
                SceneTransitionHandler.Instance.SwitchScene(mainMenuSceneName, false);
            }
        }

        public override void Exit() { }

        public override void StartClientIP(string playerName, string ipaddress, int port)
        {
            var connectionMethod = new ConnectionMethodIP(ipaddress, (ushort)port, ConnectionManager.instance, ProfileManager.Instance, playerName);
            ConnectionManager.instance.m_ClientReconnecting.Configure(connectionMethod);
            ConnectionManager.instance.ChangeState(ConnectionManager.instance.m_ClientConnecting.Configure(connectionMethod));
        }

        public override void StartClientLobby(string playerName)
        {
            var connectionMethod = new ConnectionMethodRelay(ConnectionManager.instance, ProfileManager.Instance, playerName);
            ConnectionManager.instance.m_ClientReconnecting.Configure(connectionMethod);
            ConnectionManager.instance.ChangeState(ConnectionManager.instance.m_ClientConnecting.Configure(connectionMethod));
        }

        public override void StartHostIP(string playerName, string ipaddress, int port)
        {
            var connectionMethod = new ConnectionMethodIP(ipaddress, (ushort)port, ConnectionManager.instance, ProfileManager.Instance, playerName);
            ConnectionManager.instance.ChangeState(ConnectionManager.instance.m_StartingHost.Configure(connectionMethod));
        }

        public override void StartHostLobby(string playerName)
        {
            var connectionMethod = new ConnectionMethodRelay(ConnectionManager.instance, ProfileManager.Instance, playerName);
            ConnectionManager.instance.ChangeState(ConnectionManager.instance.m_StartingHost.Configure(connectionMethod));
        }
    }
}
