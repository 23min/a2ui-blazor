using A2UI.Blazor.Diagnostics;
using Microsoft.Extensions.Logging;

namespace A2UI.Blazor.Services;

/// <summary>
/// Maps A2UI component type strings (e.g. "Text", "Button") to Blazor component Types.
/// Pre-populated with the standard catalog; extensible for custom components.
/// </summary>
public sealed class ComponentRegistry
{
    private readonly Dictionary<string, Type> _registry = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _warnedTypes = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<ComponentRegistry> _logger;

    public ComponentRegistry(ILogger<ComponentRegistry> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Register a Blazor component type for a given A2UI component type string.
    /// </summary>
    public void Register(string a2uiType, Type blazorComponentType)
    {
        _registry[a2uiType] = blazorComponentType;
    }

    /// <summary>
    /// Look up the Blazor component type for a given A2UI component type string.
    /// </summary>
    public Type? Resolve(string a2uiType)
    {
        var type = _registry.GetValueOrDefault(a2uiType);
        if (type is null && _warnedTypes.Add(a2uiType))
        {
            _logger.LogWarning(LogEvents.ComponentNotFound, "Unknown component type: {ComponentType}", a2uiType);
        }
        return type;
    }

    /// <summary>
    /// Register all standard A2UI components from the built-in catalog.
    /// </summary>
    internal void RegisterStandardComponents()
    {
        // Display
        Register("Text", typeof(Components.Display.A2UIText));
        Register("Image", typeof(Components.Display.A2UIImage));
        Register("Icon", typeof(Components.Display.A2UIIcon));
        Register("Divider", typeof(Components.Display.A2UIDivider));

        // Layout
        Register("Row", typeof(Components.Layout.A2UIRow));
        Register("Column", typeof(Components.Layout.A2UIColumn));
        Register("Card", typeof(Components.Layout.A2UICard));
        Register("List", typeof(Components.Layout.A2UIList));
        Register("Tabs", typeof(Components.Layout.A2UITabs));

        // Input
        Register("Button", typeof(Components.Input.A2UIButton));
        Register("TextField", typeof(Components.Input.A2UITextField));
        Register("CheckBox", typeof(Components.Input.A2UICheckBox));
        Register("ChoicePicker", typeof(Components.Input.A2UIChoicePicker));
        Register("DateTimeInput", typeof(Components.Input.A2UIDateTimeInput));
        Register("Slider", typeof(Components.Input.A2UISlider));

        // Media
        Register("Video", typeof(Components.Media.A2UIVideo));
        Register("AudioPlayer", typeof(Components.Media.A2UIAudioPlayer));

        // Visualization
        Register("StateMachine", typeof(Components.Visualization.A2UIStateMachine));
    }
}
