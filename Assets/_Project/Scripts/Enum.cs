namespace CarRacingGame3d
{
    public enum GameStates 
    { 
        Countdown, 
        Running, 
        RaceOverCountDown, 
        RaceOver
    }

    public enum NetworkStatus { online, offline }
    public enum AIDifficult { Easy, Normal, Hard }
    public enum Axel
    {
        Front,
        Rear
    }
    public enum DriveStyle
    {
        FWD,
        RWD,
        AWD
    }
    public enum InputType { keyboard, mouse }
    public enum AuthorityMode
    {
        Server,
        Client
    }
    public enum GameMode
    {
        TimeLimit,
        Round
    }
    public enum SeatState
    {
        Inactive,
        Active,
        LockedIn,
        Host,
        Block,
        AI
    }
    public enum ConnectStatus
    {
        Undefined,
        Success,                  //client successfully connected. This may also be a successful reconnect.
        ServerFull,               //can't join, server is already at capacity.
        GameStarted,              //can't join, game already started.
        LoggedInAgain,            //logged in on a separate client, causing this one to be kicked out.
        UserRequestedDisconnect,  //Intentional Disconnect triggered by the user.
        GenericDisconnect,        //server disconnected, but no specific reason given.
        Reconnecting,             //client lost connection and is attempting to reconnect.
        IncompatibleBuildType,    //client build type is incompatible with server.
        HostEndedSession,         //host intentionally ended the session.
        StartHostFailed,          // server failed to bind
        StartClientFailed         // failed to connect to server and/or invalid network endpoint
    }
}