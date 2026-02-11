using System.Text.Json;
using A2UI.Blazor.Protocol;

namespace A2UI.Blazor.Tests.Services;

public class A2UIClientMessageTests
{
    [Fact]
    public void Envelope_Serializes_WithVersionAndAction()
    {
        var action = new A2UIUserAction
        {
            Name = "search",
            SurfaceId = "contacts",
            SourceComponentId = "search-btn"
        };
        var envelope = new A2UIClientMessage { Action = action };

        var json = JsonSerializer.Serialize(envelope);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal("v0.9", root.GetProperty("version").GetString());
        Assert.True(root.TryGetProperty("action", out var actionEl));
        Assert.Equal("search", actionEl.GetProperty("name").GetString());
        Assert.Equal("contacts", actionEl.GetProperty("surfaceId").GetString());
        Assert.Equal("search-btn", actionEl.GetProperty("sourceComponentId").GetString());
    }

    [Fact]
    public void Envelope_DoesNotInclude_TypeField()
    {
        var action = new A2UIUserAction { Name = "click", SurfaceId = "s1", SourceComponentId = "btn1" };
        var envelope = new A2UIClientMessage { Action = action };

        var json = JsonSerializer.Serialize(envelope);
        var doc = JsonDocument.Parse(json);
        var actionEl = doc.RootElement.GetProperty("action");

        Assert.False(actionEl.TryGetProperty("type", out _));
    }

    [Fact]
    public void Action_Timestamp_IsIso8601String()
    {
        var action = new A2UIUserAction
        {
            Name = "test",
            SurfaceId = "s1",
            SourceComponentId = "c1"
        };

        var json = JsonSerializer.Serialize(action);
        var doc = JsonDocument.Parse(json);
        var timestamp = doc.RootElement.GetProperty("timestamp").GetString();

        Assert.NotNull(timestamp);
        Assert.True(DateTimeOffset.TryParse(timestamp, out _), $"Timestamp '{timestamp}' is not valid ISO 8601");
    }

    [Fact]
    public void Action_Context_DefaultsToEmptyObject()
    {
        var action = new A2UIUserAction
        {
            Name = "test",
            SurfaceId = "s1",
            SourceComponentId = "c1"
        };

        var json = JsonSerializer.Serialize(action);
        var doc = JsonDocument.Parse(json);
        var context = doc.RootElement.GetProperty("context");

        Assert.Equal(JsonValueKind.Object, context.ValueKind);
        Assert.Empty(context.EnumerateObject());
    }

    [Fact]
    public void Capabilities_Serializes_WithV09Key()
    {
        var capabilities = new A2UIClientCapabilities();

        var json = JsonSerializer.Serialize(capabilities);
        var doc = JsonDocument.Parse(json);
        var v09 = doc.RootElement.GetProperty("v0.9");

        Assert.True(v09.TryGetProperty("supportedCatalogIds", out var catalogIds));
        Assert.True(catalogIds.GetArrayLength() > 0);
    }

    [Fact]
    public void Envelope_HasExactlyTwoTopLevelProperties()
    {
        var action = new A2UIUserAction { Name = "test", SurfaceId = "s1", SourceComponentId = "c1" };
        var envelope = new A2UIClientMessage { Action = action };

        var json = JsonSerializer.Serialize(envelope);
        var doc = JsonDocument.Parse(json);

        Assert.Equal(2, doc.RootElement.EnumerateObject().Count());
    }
}
