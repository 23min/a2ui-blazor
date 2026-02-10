namespace A2UI.Blazor.Services;

/// <summary>
/// Represents the connection state of an A2UI stream client.
/// </summary>
public enum StreamConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Reconnecting,
}
