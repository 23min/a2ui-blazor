using System.Text.Json;

namespace A2UI.Blazor.Server.Builders;

/// <summary>
/// Fluent builder for constructing A2UI component objects.
/// </summary>
public sealed class ComponentBuilder
{
    private readonly string _id;
    private readonly string _component;
    private readonly Dictionary<string, object> _properties = new();

    public ComponentBuilder(string id, string component)
    {
        _id = id;
        _component = component;
    }

    public ComponentBuilder Set(string property, object value)
    {
        _properties[property] = value;
        return this;
    }

    public ComponentBuilder Text(string text) => Set("text", text);
    public ComponentBuilder Label(string label) => Set("label", label);
    public ComponentBuilder UsageHint(string hint) => Set("usageHint", hint);
    public ComponentBuilder Src(string src) => Set("src", src);
    public ComponentBuilder Alt(string alt) => Set("alt", alt);
    public ComponentBuilder Placeholder(string placeholder) => Set("placeholder", placeholder);
    public ComponentBuilder Variant(string variant) => Set("variant", variant);
    public ComponentBuilder Disabled(bool disabled = true) => Set("disabled", disabled);
    public ComponentBuilder Value(object value) => Set("value", value);
    public ComponentBuilder Children(params string[] childIds) => Set("children", childIds);
    public ComponentBuilder Options(params string[] options) => Set("options", options);
    public ComponentBuilder Title(string title) => Set("title", title);

    public ComponentBuilder Action(string eventName, Dictionary<string, object>? context = null)
    {
        var eventObj = new Dictionary<string, object> { ["name"] = eventName };
        if (context is not null) eventObj["context"] = context;
        var action = new Dictionary<string, object> { ["event"] = eventObj };
        return Set("action", action);
    }

    public ComponentBuilder Distribution(string distribution) => Set("distribution", distribution);
    public ComponentBuilder Alignment(string alignment) => Set("alignment", alignment);
    public ComponentBuilder Gap(string gap) => Set("gap", gap);

    public ComponentBuilder Min(double min) => Set("min", min);
    public ComponentBuilder Max(double max) => Set("max", max);
    public ComponentBuilder Step(double step) => Set("step", step);

    public ComponentBuilder Data(string dataPath) => Set("data", dataPath);
    public ComponentBuilder Template(string componentId) =>
        Set("template", new Dictionary<string, object> { ["componentId"] = componentId });

    public ComponentBuilder Tabs(params (string Label, string ContentId)[] tabs) =>
        Set("tabs", tabs.Select(t => new Dictionary<string, object>
        {
            ["label"] = t.Label,
            ["contentId"] = t.ContentId
        }).ToArray());

    public Dictionary<string, object> Build()
    {
        var result = new Dictionary<string, object>
        {
            ["id"] = _id,
            ["component"] = _component
        };

        foreach (var kvp in _properties)
            result[kvp.Key] = kvp.Value;

        return result;
    }
}
