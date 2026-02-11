namespace A2UI.Blazor.Server.Builders;

/// <summary>
/// Fluent builder for constructing A2UI surface messages.
/// </summary>
public sealed class SurfaceBuilder
{
    private readonly string _surfaceId;
    private string? _catalogId;
    private bool _sendDataModel;
    private object? _theme;
    private readonly List<ComponentBuilder> _components = new();

    public SurfaceBuilder(string surfaceId)
    {
        _surfaceId = surfaceId;
    }

    public SurfaceBuilder CatalogId(string catalogId)
    {
        _catalogId = catalogId;
        return this;
    }

    public SurfaceBuilder SendDataModel(bool send = true)
    {
        _sendDataModel = send;
        return this;
    }

    public SurfaceBuilder Theme(object theme)
    {
        _theme = theme;
        return this;
    }

    public SurfaceBuilder AddComponent(string id, string componentType, Action<ComponentBuilder> configure)
    {
        var builder = new ComponentBuilder(id, componentType);
        configure(builder);
        _components.Add(builder);
        return this;
    }

    public SurfaceBuilder AddComponent(ComponentBuilder component)
    {
        _components.Add(component);
        return this;
    }

    public string SurfaceId => _surfaceId;
    public string? CatalogIdValue => _catalogId;
    public bool SendDataModelValue => _sendDataModel;
    public object? ThemeValue => _theme;

    public List<Dictionary<string, object>> BuildComponents()
    {
        return _components.Select(c => c.Build()).ToList();
    }
}
