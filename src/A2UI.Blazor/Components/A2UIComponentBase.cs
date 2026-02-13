using System.Text.Json;
using A2UI.Blazor.Protocol;
using A2UI.Blazor.Services;
using Microsoft.AspNetCore.Components;

namespace A2UI.Blazor.Components;

/// <summary>
/// Base class for all A2UI Blazor components. Provides access to
/// component data, the surface state, and data binding resolution.
/// </summary>
public abstract class A2UIComponentBase : ComponentBase
{
    [Parameter, EditorRequired]
    public A2UIComponentData Data { get; set; } = default!;

    [Parameter, EditorRequired]
    public A2UISurfaceState Surface { get; set; } = default!;

    [Inject]
    protected SurfaceManager? SurfaceManager { get; set; }

    [CascadingParameter(Name = "ScopeElement")]
    public JsonElement? ScopeElement { get; set; }

    [Inject]
    protected LocalActionRegistry? LocalActionRegistry { get; set; }

    /// <summary>
    /// Get a validation error for a bound property.
    /// Checks the surface's ValidationErrors dictionary for the property's binding path.
    /// </summary>
    protected string? GetValidationError(string propertyName)
    {
        if (Data.Properties is null) return null;
        if (!Data.Properties.TryGetValue(propertyName, out var element)) return null;
        if (element.ValueKind != JsonValueKind.String) return null;

        var path = element.GetString();
        if (path is null || !path.StartsWith('/')) return null;

        return Surface.ValidationErrors.GetValueOrDefault(path);
    }

    /// <summary>
    /// Get a string property from the component data.
    /// If the value starts with '/' it's treated as a data binding path.
    /// </summary>
    protected string? GetString(string propertyName)
    {
        if (Data.Properties is null) return null;
        if (!Data.Properties.TryGetValue(propertyName, out var element)) return null;

        if (element.ValueKind == JsonValueKind.String)
        {
            var val = element.GetString();
            return ResolveStringBinding(val);
        }

        if (element.ValueKind == JsonValueKind.Object)
            return ResolveFunctionCall(element);

        return element.ToString();
    }

    /// <summary>
    /// Get a boolean property from the component data.
    /// </summary>
    protected bool GetBool(string propertyName, bool defaultValue = false)
    {
        if (Data.Properties is null) return defaultValue;
        if (!Data.Properties.TryGetValue(propertyName, out var element)) return defaultValue;

        if (element.ValueKind == JsonValueKind.True) return true;
        if (element.ValueKind == JsonValueKind.False) return false;

        // Check for data binding
        if (element.ValueKind == JsonValueKind.String)
        {
            var path = element.GetString();
            if (path is not null && path.StartsWith('/'))
            {
                var resolved = SurfaceManager?.ResolveBinding(Surface.SurfaceId, path);
                if (resolved?.ValueKind == JsonValueKind.True) return true;
                if (resolved?.ValueKind == JsonValueKind.False) return false;
            }
        }

        return defaultValue;
    }

    /// <summary>
    /// Get a numeric property from the component data.
    /// </summary>
    protected double? GetNumber(string propertyName)
    {
        if (Data.Properties is null) return null;
        if (!Data.Properties.TryGetValue(propertyName, out var element)) return null;

        if (element.ValueKind == JsonValueKind.Number)
            return element.GetDouble();

        if (element.ValueKind == JsonValueKind.String)
        {
            var path = element.GetString();
            if (path is not null && path.StartsWith('/'))
            {
                var resolved = SurfaceManager?.ResolveBinding(Surface.SurfaceId, path);
                if (resolved?.ValueKind == JsonValueKind.Number)
                    return resolved.Value.GetDouble();
            }
        }

        return null;
    }

    /// <summary>
    /// Get a list of string values from the component data.
    /// </summary>
    protected List<string> GetStringList(string propertyName)
    {
        if (Data.Properties is null) return [];
        if (!Data.Properties.TryGetValue(propertyName, out var element)) return [];

        if (element.ValueKind == JsonValueKind.Array)
        {
            var result = new List<string>();
            foreach (var item in element.EnumerateArray())
            {
                var s = item.GetString();
                if (s is not null) result.Add(s);
            }
            return result;
        }

        // Could be a binding path
        if (element.ValueKind == JsonValueKind.String)
        {
            var path = element.GetString();
            if (path is not null && path.StartsWith('/'))
            {
                var resolved = SurfaceManager?.ResolveBinding(Surface.SurfaceId, path);
                if (resolved?.ValueKind == JsonValueKind.Array)
                {
                    var result = new List<string>();
                    foreach (var item in resolved.Value.EnumerateArray())
                    {
                        var s = item.GetString();
                        if (s is not null) result.Add(s);
                    }
                    return result;
                }
            }
        }

        return [];
    }

    /// <summary>
    /// Get the raw JsonElement for a property.
    /// </summary>
    protected JsonElement? GetElement(string propertyName)
    {
        if (Data.Properties is null) return null;
        if (!Data.Properties.TryGetValue(propertyName, out var element)) return null;
        return element;
    }

    /// <summary>
    /// Get the A2UI action definition for this component.
    /// </summary>
    protected A2UIAction? GetAction()
    {
        var element = GetElement("action");
        if (element is null) return null;
        return JsonSerializer.Deserialize<A2UIAction>(element.Value.GetRawText());
    }

    /// <summary>
    /// Apply an optimistic update to the data model for a bound property.
    /// If the property value is a data binding path (starts with '/'),
    /// the data model is updated locally before the server responds.
    /// </summary>
    protected void ApplyOptimisticUpdate(string propertyName, object? value)
    {
        if (SurfaceManager is null || Data.Properties is null) return;
        if (!Data.Properties.TryGetValue(propertyName, out var element)) return;
        if (element.ValueKind != JsonValueKind.String) return;

        var path = element.GetString();
        if (path is null || !path.StartsWith('/')) return;

        var jsonValue = JsonSerializer.SerializeToElement(value);
        SurfaceManager.UpdateDataModel(Surface.SurfaceId, path, jsonValue);
    }

    /// <summary>
    /// Execute a local action (functionCall) if one is registered.
    /// Returns true if a local action was found and executed.
    /// </summary>
    protected bool ExecuteLocalAction(A2UILocalAction localAction)
    {
        if (LocalActionRegistry is null || !LocalActionRegistry.IsRegistered(localAction.Call))
            return false;

        LocalActionRegistry.Execute(localAction.Call, localAction.Args);
        return true;
    }

    /// <summary>
    /// Get child component IDs.
    /// </summary>
    protected List<string> GetChildIds()
    {
        if (Data.Properties is null) return [];
        if (!Data.Properties.TryGetValue("children", out var element)) return [];

        if (element.ValueKind != JsonValueKind.Array) return [];

        var result = new List<string>();
        foreach (var item in element.EnumerateArray())
        {
            var id = item.GetString();
            if (id is not null) result.Add(id);
        }
        return result;
    }

    private string? ResolveFunctionCall(JsonElement element)
    {
        if (!element.TryGetProperty("call", out var callEl) ||
            callEl.GetString() != "formatString")
            return element.ToString();

        if (!element.TryGetProperty("args", out var argsEl) ||
            argsEl.ValueKind != JsonValueKind.Object)
            return element.ToString();

        if (!argsEl.TryGetProperty("value", out var valueEl) ||
            valueEl.ValueKind != JsonValueKind.String)
            return element.ToString();

        var template = valueEl.GetString();
        var resolver = new FormatStringResolver();

        JsonElement? dataModelRoot = null;
        if (SurfaceManager is not null)
        {
            var surface = SurfaceManager.GetSurface(Surface.SurfaceId);
            dataModelRoot = surface?.DataModel?.RootElement;
        }

        return resolver.Resolve(template, dataModelRoot, ScopeElement);
    }

    private string? ResolveStringBinding(string? value)
    {
        if (value is null) return null;

        // DynamicString pattern: path starting with /
        if (value.StartsWith('/') && SurfaceManager is not null)
        {
            var resolved = SurfaceManager.ResolveBinding(Surface.SurfaceId, value);
            if (resolved.HasValue)
                return resolved.Value.ValueKind == JsonValueKind.String
                    ? resolved.Value.GetString()
                    : resolved.Value.ToString();
        }

        // Relative binding against scope
        if (ScopeElement.HasValue && !value.Contains('/'))
        {
            var resolver = new DataBindingResolver();
            var resolved = resolver.ResolveRelative(ScopeElement.Value, value);
            if (resolved.HasValue)
                return resolved.Value.ValueKind == JsonValueKind.String
                    ? resolved.Value.GetString()
                    : resolved.Value.ToString();
        }

        return value;
    }
}
