using A2UI.Blazor.Server.Streaming;

namespace A2UI.Blazor.Server.Agents;

/// <summary>
/// Interface for A2UI agents. Implement this to create an agent that
/// streams A2UI surfaces to clients.
/// </summary>
public interface IA2UIAgent
{
    /// <summary>
    /// Unique route path for this agent (e.g. "/agents/restaurant").
    /// </summary>
    string Route { get; }

    /// <summary>
    /// Handle a new client connection. Write A2UI messages to the writer
    /// to stream surfaces to the client.
    /// </summary>
    Task HandleAsync(A2UIStreamWriter writer, CancellationToken cancellationToken);

    /// <summary>
    /// Handle a user action from the client.
    /// </summary>
    Task HandleActionAsync(A2UIStreamWriter writer, UserActionRequest action, CancellationToken cancellationToken);
}

/// <summary>
/// Represents a user action received from the client.
/// </summary>
public sealed class UserActionRequest
{
    public string Name { get; set; } = string.Empty;
    public string SurfaceId { get; set; } = string.Empty;
    public string SourceComponentId { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
    public Dictionary<string, object?>? Context { get; set; }
}
