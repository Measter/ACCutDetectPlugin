namespace ACCutDetectorPlugin
{
    enum ACSProtocol : byte
    {
        NewSession = 50,
        NewConnection = 51,
        ConnectionClosed = 52,
        CarUpdate = 53,
        CarInfo = 54, // Sent as response to ACSP_GET_CAR_INFO command
        EndSession = 55,
        LapCompleted = 73,
        Version = 56,
        Chat = 57,
        ClientLoaded = 58,
        SessionInfo = 59,
        Error = 60,

        ClientEvent = 130,
    }

    enum ACSProtocolEvents : byte
    {
        CECollisionWithCar = 10,
        CECollisionWithEnv = 11
    }

    enum ACSProtocolCommands : byte
    {
        // COMMANDS
        RealtimeposInterval = 200,
        GetCarInfo = 201,
        SendChat = 202, // Sends chat to one car
        BroadcastChat = 203, // Sends chat to everybody
        GetSessionInfo = 204,
        SetSessionInfo = 205,
        KickUser = 206,
        NextSession = 207,
        RestartSession = 208,
        AdminCommand = 209, // Send message plus a stringW with the command
    }
}
