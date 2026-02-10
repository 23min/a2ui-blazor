namespace A2UI.Blazor.Diagnostics;

/// <summary>
/// Log event IDs for A2UI Blazor services.
/// </summary>
internal static class LogEvents
{
    // A2UIStreamClient (1000-1099)
    public const int Connecting = 1000;
    public const int Connected = 1001;
    public const int Reconnecting = 1002;
    public const int Disconnected = 1003;
    public const int StreamEnded = 1004;
    public const int ClientError = 1005;
    public const int StreamError = 1006;
    public const int SendingAction = 1010;
    public const int ActionFailed = 1011;

    // MessageDispatcher (2000-2099)
    public const int UnknownMessageType = 2001;
    public const int NullSurfaceId = 2002;

    // SurfaceManager (3000-3099)
    public const int SurfaceCreated = 3000;
    public const int SurfaceDeleted = 3001;
    public const int UnknownSurface = 3005;

    // JsonlStreamReader (4000-4099)
    public const int ParseError = 4001;

    // ComponentRegistry (5000-5099)
    public const int ComponentNotFound = 5002;
}
