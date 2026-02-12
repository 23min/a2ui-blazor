using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace A2UI.Blazor.Services;

/// <summary>
/// Registry for client-side local actions (functionCall).
/// Components execute registered actions locally without a server round-trip.
/// </summary>
public sealed class LocalActionRegistry
{
    private readonly Dictionary<string, Func<Dictionary<string, JsonElement>?, object?>> _handlers = new();

    /// <summary>
    /// Register a local action handler that returns a result.
    /// </summary>
    public void Register(string name, Func<Dictionary<string, JsonElement>?, object?> handler)
    {
        _handlers[name] = handler;
    }

    /// <summary>
    /// Register a local action handler that returns no result.
    /// </summary>
    public void Register(string name, Action<Dictionary<string, JsonElement>?> handler)
    {
        _handlers[name] = args =>
        {
            handler(args);
            return null;
        };
    }

    /// <summary>
    /// Execute a registered local action. Returns null if the action is not registered.
    /// </summary>
    public object? Execute(string name, Dictionary<string, JsonElement>? args)
    {
        if (_handlers.TryGetValue(name, out var handler))
            return handler(args);

        return null;
    }

    /// <summary>
    /// Check whether a local action is registered.
    /// </summary>
    public bool IsRegistered(string name) => _handlers.ContainsKey(name);
}
