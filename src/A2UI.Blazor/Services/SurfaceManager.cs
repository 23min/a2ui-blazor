using System.Text.Json;
using A2UI.Blazor.Diagnostics;
using A2UI.Blazor.Protocol;
using Microsoft.Extensions.Logging;

namespace A2UI.Blazor.Services;

/// <summary>
/// Maintains the set of active surfaces and their state.
/// Notifies subscribers when surfaces change so Blazor components can re-render.
/// </summary>
public sealed class SurfaceManager
{
    private readonly Dictionary<string, A2UISurfaceState> _surfaces = new();
    private readonly DataBindingResolver _resolver = new();
    private readonly ILogger<SurfaceManager> _logger;

    public SurfaceManager(ILogger<SurfaceManager> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Fired whenever any surface state changes. The string parameter is the surface ID.
    /// </summary>
    public event Action<string>? OnSurfaceChanged;

    public A2UISurfaceState? GetSurface(string surfaceId) =>
        _surfaces.GetValueOrDefault(surfaceId);

    public IReadOnlyCollection<string> GetSurfaceIds() => _surfaces.Keys;

    public void CreateSurface(string surfaceId, string? catalogId, bool sendDataModel, JsonElement? theme = null)
    {
        _logger.LogInformation(LogEvents.SurfaceCreated, "Creating surface {SurfaceId} with catalogId {CatalogId}", surfaceId, catalogId);
        var surface = new A2UISurfaceState(surfaceId)
        {
            CatalogId = catalogId,
            SendDataModel = sendDataModel,
            Theme = theme
        };
        _surfaces[surfaceId] = surface;
    }

    public void UpdateComponents(string surfaceId, List<A2UIComponentData> components)
    {
        if (!_surfaces.TryGetValue(surfaceId, out var surface))
        {
            _logger.LogWarning(LogEvents.UnknownSurface, "Cannot update components for unknown surface {SurfaceId}", surfaceId);
            return;
        }

        _logger.LogDebug("Updating {ComponentCount} components for surface {SurfaceId}", components.Count, surfaceId);
        foreach (var component in components)
        {
            surface.Components[component.Id] = component;
        }

        if (!surface.IsReady && surface.GetRoot() is not null)
        {
            surface.IsReady = true;
            _logger.LogInformation(LogEvents.SurfaceReady, "Surface {SurfaceId} is ready (root component received)", surfaceId);
        }

        if (surface.IsReady)
        {
            NotifySurfaceChanged(surfaceId);
        }
    }

    public void UpdateDataModel(string surfaceId, string? path, JsonElement? value)
    {
        if (!_surfaces.TryGetValue(surfaceId, out var surface))
        {
            _logger.LogWarning(LogEvents.UnknownSurface, "Cannot update data model for unknown surface {SurfaceId}", surfaceId);
            return;
        }

        try
        {
            if (path is null or "" or "/")
            {
                // Replace entire data model
                if (value.HasValue)
                {
                    _logger.LogDebug("Replacing entire data model for surface {SurfaceId}", surfaceId);
                    surface.DataModel?.Dispose();
                    surface.DataModel = JsonDocument.Parse(value.Value.GetRawText());
                }
            }
            else if (value.HasValue)
            {
                // Patch at specific path — rebuild the document with the value set
                _logger.LogDebug("Patching data model at path {Path} for surface {SurfaceId}", path, surfaceId);
                var current = surface.DataModel?.RootElement;
                var updated = DataBindingResolver.SetValueAtPath(current, path, value.Value);
                surface.DataModel?.Dispose();
                surface.DataModel = JsonDocument.Parse(updated.GetRawText());
            }

            if (surface.IsReady)
            {
                NotifySurfaceChanged(surfaceId);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse data model update for surface {SurfaceId} at path {Path}", surfaceId, path);
        }
    }

    public void DeleteSurface(string surfaceId)
    {
        if (_surfaces.Remove(surfaceId, out var surface))
        {
            _logger.LogInformation(LogEvents.SurfaceDeleted, "Deleting surface {SurfaceId}", surfaceId);
            surface.DataModel?.Dispose();
            NotifySurfaceChanged(surfaceId);
        }
        else
        {
            _logger.LogWarning(LogEvents.UnknownSurface, "Cannot delete unknown surface {SurfaceId}", surfaceId);
        }
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

    private void NotifySurfaceChanged(string surfaceId)
    {
        try
        {
            OnSurfaceChanged?.Invoke(surfaceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Subscriber error in OnSurfaceChanged for surface {SurfaceId}", surfaceId);
        }
    }
}
