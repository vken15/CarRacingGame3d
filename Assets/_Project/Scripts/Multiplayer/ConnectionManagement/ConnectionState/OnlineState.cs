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
            //m_ConnectStatusPublisher.Publish(ConnectStatus.UserRequestedDisconnect);
            ConnectionStatusMessageUIManager.instance.OnConnectStatus(ConnectStatus.UserRequestedDisconnect);
            ConnectionManager.instance.ChangeState(ConnectionManager.instance.m_Offline);
        }

        public override void OnTransportFailure()
        {
            // This behaviour will be the same for every online state
            ConnectionManager.instance.ChangeState(ConnectionManager.instance.m_Offline);
        }
    }
}
