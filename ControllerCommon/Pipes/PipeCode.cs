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

        CLIENT_CONTROLLER_CONNECT = 9,      // Sent to server to share current controller details

        CLIENT_CONTROLLER_DISCONNECT = 11,  // Sent to server to warn current controller was disconnected

        SERVER_SENSOR = 13,                 // Sent to client to share sensor values
                                            // args: ...

        CLIENT_NAVIGATED = 14,              // Sent to server to share current navigated page
                                            // args: ...

        CLIENT_OVERLAY = 15,                // Sent to server to share current overlay status
                                            // args: ...

        SERVER_VIBRATION = 16,              // Sent to client to notify a vibration feedback arrived
                                            // args: ...

        CLIENT_MOVEMENTS = 17,              // Sent to server to inform on controller/device movements
    }
}
