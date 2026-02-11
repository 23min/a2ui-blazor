using System.Text.Json;
using System.Text.Json.Serialization;

namespace A2UI.Blazor.Protocol;

/// <summary>
/// Base envelope for all A2UI protocol messages.
/// Each line in a JSONL stream deserializes to this type.
/// </summary>
public sealed class A2UIMessage
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("surfaceId")]
    public string? SurfaceId { get; set; }

    [JsonPropertyName("catalogId")]
    public string? CatalogId { get; set; }

    [JsonPropertyName("theme")]
    public JsonElement? Theme { get; set; }

    [JsonPropertyName("sendDataModel")]
    public bool? SendDataModel { get; set; }

    [JsonPropertyName("components")]
    public List<A2UIComponentData>? Components { get; set; }

    [JsonPropertyName("path")]
    public string? Path { get; set; }

    [JsonPropertyName("value")]
    public JsonElement? Value { get; set; }
}

/// <summary>
/// Raw component data from the protocol — a flat bag of properties.
/// </summary>
public sealed class A2UIComponentData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("component")]
    public string Component { get; set; } = string.Empty;

    /// <summary>
    /// All additional properties beyond id/component are captured here.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Properties { get; set; }
}

/// <summary>
/// An action definition on a component (server or local).
/// </summary>
public sealed class A2UIAction
{
    [JsonPropertyName("event")]
    public A2UIEventAction? Event { get; set; }

    [JsonPropertyName("functionCall")]
    public A2UILocalAction? FunctionCall { get; set; }
}

public sealed class A2UIEventAction
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("context")]
    public Dictionary<string, JsonElement>? Context { get; set; }
}

public sealed class A2UILocalAction
{
    [JsonPropertyName("call")]
    public string Call { get; set; } = string.Empty;

    [JsonPropertyName("args")]
    public Dictionary<string, JsonElement>? Args { get; set; }
}

/// <summary>
/// v0.9 client-to-server message envelope.
/// Contains version, action, and optionally the data model (when sendDataModel is true).
/// </summary>
public sealed class A2UIClientMessage
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "v0.9";

    [JsonPropertyName("action")]
    public A2UIUserAction? Action { get; set; }

    [JsonPropertyName("dataModel")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public JsonElement? DataModel { get; set; }
}

/// <summary>
/// A user action sent from client to server (inner payload of A2UIClientMessage).
/// </summary>
public sealed class A2UIUserAction
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("surfaceId")]
    public string SurfaceId { get; set; } = string.Empty;

    [JsonPropertyName("sourceComponentId")]
    public string SourceComponentId { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = DateTimeOffset.UtcNow.ToString("o");

    [JsonPropertyName("context")]
    public Dictionary<string, object?> Context { get; set; } = new();
}

/// <summary>
/// Client capabilities declared to the server via transport metadata.
/// Sent as A2UI-Client-Capabilities HTTP header on every action POST.
/// </summary>
public sealed class A2UIClientCapabilities
{
    [JsonPropertyName("v0.9")]
    public A2UICapabilitiesV09 V09 { get; set; } = new();
}

public sealed class A2UICapabilitiesV09
{
    [JsonPropertyName("supportedCatalogIds")]
    public List<string> SupportedCatalogIds { get; set; } =
    [
        "https://github.com/google/A2UI/blob/main/specification/v0_9/json/standard_catalog.json"
    ];
}
