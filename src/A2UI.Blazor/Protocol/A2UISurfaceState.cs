using System.Text.Json;

namespace A2UI.Blazor.Protocol;

/// <summary>
/// Represents the runtime state of a single A2UI surface.
/// </summary>
public sealed class A2UISurfaceState
{
    public string SurfaceId { get; }
    public string? CatalogId { get; set; }

    /// <summary>
    /// Flat dictionary of components keyed by their ID.
    /// </summary>
    public Dictionary<string, A2UIComponentData> Components { get; } = new();

    /// <summary>
    /// The surface's data model — a mutable JSON document.
    /// </summary>
    public JsonDocument? DataModel { get; set; }

    /// <summary>
    /// Whether the server requested the client to send data model updates back.
    /// </summary>
    public bool SendDataModel { get; set; }

    public A2UISurfaceState(string surfaceId)
    {
        SurfaceId = surfaceId;
    }

    /// <summary>
    /// Get the root component (id == "root"), or null if not yet received.
    /// </summary>
    public A2UIComponentData? GetRoot() =>
        Components.GetValueOrDefault("root");

    /// <summary>
    /// Get all children whose parent property points to the given component ID.
    /// Components use a "children" array of IDs, so we look up each referenced ID.
    /// </summary>
    public List<A2UIComponentData> GetChildren(string parentId)
    {
        if (!Components.TryGetValue(parentId, out var parent))
            return [];

        if (parent.Properties is null)
            return [];

        if (!parent.Properties.TryGetValue("children", out var childrenElement))
            return [];

        var result = new List<A2UIComponentData>();
        if (childrenElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in childrenElement.EnumerateArray())
            {
                var childId = child.GetString();
                if (childId is not null && Components.TryGetValue(childId, out var childComponent))
                {
                    result.Add(childComponent);
                }
            }
        }

        return result;
    }
}
