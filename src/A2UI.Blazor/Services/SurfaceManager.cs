using System.Text.Json;
using A2UI.Blazor.Protocol;

namespace A2UI.Blazor.Services;

/// <summary>
/// Maintains the set of active surfaces and their state.
/// Notifies subscribers when surfaces change so Blazor components can re-render.
/// </summary>
public sealed class SurfaceManager
{
    private readonly Dictionary<string, A2UISurfaceState> _surfaces = new();
    private readonly DataBindingResolver _resolver = new();

    /// <summary>
    /// Fired whenever any surface state changes. The string parameter is the surface ID.
    /// </summary>
    public event Action<string>? OnSurfaceChanged;

    public A2UISurfaceState? GetSurface(string surfaceId) =>
        _surfaces.GetValueOrDefault(surfaceId);

    public IReadOnlyCollection<string> GetSurfaceIds() => _surfaces.Keys;

    public void CreateSurface(string surfaceId, string? catalogId, bool sendDataModel)
    {
        var surface = new A2UISurfaceState(surfaceId)
        {
            CatalogId = catalogId,
            SendDataModel = sendDataModel
        };
        _surfaces[surfaceId] = surface;
        OnSurfaceChanged?.Invoke(surfaceId);
    }

    public void UpdateComponents(string surfaceId, List<A2UIComponentData> components)
    {
        if (!_surfaces.TryGetValue(surfaceId, out var surface)) return;

        foreach (var component in components)
        {
            surface.Components[component.Id] = component;
        }

        OnSurfaceChanged?.Invoke(surfaceId);
    }

    public void UpdateDataModel(string surfaceId, string? path, JsonElement? value)
    {
        if (!_surfaces.TryGetValue(surfaceId, out var surface)) return;

        if (path is null or "" or "/")
        {
            // Replace entire data model
            if (value.HasValue)
            {
                surface.DataModel?.Dispose();
                surface.DataModel = JsonDocument.Parse(value.Value.GetRawText());
            }
        }
        else if (value.HasValue)
        {
            // Patch at specific path — rebuild the document with the value set
            var current = surface.DataModel?.RootElement;
            var updated = DataBindingResolver.SetValueAtPath(current, path, value.Value);
            surface.DataModel?.Dispose();
            surface.DataModel = JsonDocument.Parse(updated.GetRawText());
        }

        OnSurfaceChanged?.Invoke(surfaceId);
    }

    public void DeleteSurface(string surfaceId)
    {
        if (_surfaces.Remove(surfaceId, out var surface))
        {
            surface.DataModel?.Dispose();
        }
        OnSurfaceChanged?.Invoke(surfaceId);
    }

    /// <summary>
    /// Resolve a data binding path against a surface's data model.
    /// </summary>
    public JsonElement? ResolveBinding(string surfaceId, string path)
    {
        if (!_surfaces.TryGetValue(surfaceId, out var surface)) return null;
        if (surface.DataModel is null) return null;
        return _resolver.Resolve(surface.DataModel.RootElement, path);
    }
}
