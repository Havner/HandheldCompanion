namespace ControllerCommon.Pipes
{
    public enum PipeCode
    {
        SERVER_PING = 0,                    // Sent to client during initialization
                                            // args: ...

        CLIENT_PROFILE = 1,                 // Sent to server to switch profiles
                                            // args: process id, process path

        SERVER_TOAST = 2,                   // Sent to client to display toast notification.
                                            // args: title, message

        SERVER_SETTINGS = 6,                // Sent to client during initialization
                                            // args: ...

        CLIENT_INPUT = 7,                   // Sent to server to request a specific gamepad input
                                            // args: ...

        CLIENT_SETTINGS = 8,                // Sent to server to update settings
                                            // args: ...

        SERVER_VIBRATION = 16,              // Sent to client to notify a vibration feedback arrived
                                            // args: ...
    }
}
