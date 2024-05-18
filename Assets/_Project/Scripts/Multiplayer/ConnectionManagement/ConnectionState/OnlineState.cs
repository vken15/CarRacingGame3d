namespace CarRacingGame3d
{
    /// <summary>
    /// Base class representing an online connection state.
    /// </summary>
    abstract class OnlineState : ConnectionState
    {
        public override void OnUserRequestedShutdown()
        {
            // This behaviour will be the same for every online state
            ConnectionStatusMessageUIManager.instance.OnConnectStatus(ConnectStatus.UserRequestedDisconnect);
            ConnectionManager.instance.ChangeState(ConnectionManager.instance.offline);
        }

        public override void OnTransportFailure()
        {
            // This behaviour will be the same for every online state
            ConnectionManager.instance.ChangeState(ConnectionManager.instance.offline);
        }
    }
}
