using System;
using System.Threading.Tasks;
using UnityEngine;

namespace CarRacingGame3d
{
    /// <summary>
    /// Connection state corresponding to when a client is attempting to connect to a server. Starts the client when
    /// entering. If successful, transitions to the ClientConnected state. If not, transitions to the Offline state.
    /// </summary>
    class ClientConnectingState : OnlineState
    {
        protected ConnectionMethodBase connectionMethod;

        public ClientConnectingState Configure(ConnectionMethodBase baseConnectionMethod)
        {
            connectionMethod = baseConnectionMethod;
            return this;
        }

        public override void Enter()
        {
#pragma warning disable 4014
            ConnectClientAsync();
#pragma warning restore 4014
        }

        public override void Exit() { }

        public override void OnClientConnected(ulong _)
        {
            ConnectionStatusMessageUIManager.instance.OnConnectStatus(ConnectStatus.Success);
            ConnectionManager.instance.ChangeState(ConnectionManager.instance.clientConnected);
        }

        public override void OnClientDisconnect(ulong _)
        {
            // client ID is for sure ours here
            StartingClientFailed();
        }

        void StartingClientFailed()
        {
            var disconnectReason = ConnectionManager.instance.NetworkManager.DisconnectReason;
            if (string.IsNullOrEmpty(disconnectReason))
            {
                ConnectionStatusMessageUIManager.instance.OnConnectStatus(ConnectStatus.StartClientFailed);
            }
            else
            {
                var connectStatus = JsonUtility.FromJson<ConnectStatus>(disconnectReason);
                ConnectionStatusMessageUIManager.instance.OnConnectStatus(connectStatus);
            }
            ConnectionManager.instance.ChangeState(ConnectionManager.instance.offline);
        }


        internal async Task ConnectClientAsync()
        {
            try
            {
                // Setup NGO with current connection method
                await connectionMethod.SetupClientConnectionAsync();

                // NGO's StartClient launches everything
                if (!ConnectionManager.instance.NetworkManager.StartClient())
                {
                    throw new Exception("NetworkManager StartClient failed");
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error connecting client, see following exception");
                Debug.LogException(e);
                StartingClientFailed();
                throw;
            }
        }
    }
}
