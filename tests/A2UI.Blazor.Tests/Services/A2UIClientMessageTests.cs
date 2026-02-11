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

    [Fact]
    public void ErrorEnvelope_Serializes_WithVersionAndError()
    {
        var error = new A2UIClientError
        {
            Code = "VALIDATION_FAILED",
            SurfaceId = "s1",
            Message = "Expected string, got number"
        };
        var envelope = new A2UIClientMessage { Error = error };

        var json = JsonSerializer.Serialize(envelope);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal("v0.9", root.GetProperty("version").GetString());
        Assert.True(root.TryGetProperty("error", out var errorEl));
        Assert.Equal("VALIDATION_FAILED", errorEl.GetProperty("code").GetString());
        Assert.Equal("s1", errorEl.GetProperty("surfaceId").GetString());
        Assert.Equal("Expected string, got number", errorEl.GetProperty("message").GetString());
    }

    [Fact]
    public void ErrorEnvelope_OmitsAction_WhenOnlyErrorSet()
    {
        var error = new A2UIClientError { Code = "TEST", SurfaceId = "s1", Message = "test" };
        var envelope = new A2UIClientMessage { Error = error };

        var json = JsonSerializer.Serialize(envelope);
        var doc = JsonDocument.Parse(json);

        Assert.False(doc.RootElement.TryGetProperty("action", out _));
    }

    [Fact]
    public void ErrorEnvelope_OmitsError_WhenOnlyActionSet()
    {
        var action = new A2UIUserAction { Name = "click", SurfaceId = "s1", SourceComponentId = "btn1" };
        var envelope = new A2UIClientMessage { Action = action };

        var json = JsonSerializer.Serialize(envelope);
        var doc = JsonDocument.Parse(json);

        Assert.False(doc.RootElement.TryGetProperty("error", out _));
    }

    [Fact]
    public void ErrorEnvelope_IncludesPath_WhenProvided()
    {
        var error = new A2UIClientError
        {
            Code = "VALIDATION_FAILED",
            SurfaceId = "s1",
            Message = "bad value",
            Path = "/components/0/text"
        };
        var envelope = new A2UIClientMessage { Error = error };

        var json = JsonSerializer.Serialize(envelope);
        var doc = JsonDocument.Parse(json);
        var errorEl = doc.RootElement.GetProperty("error");

        Assert.Equal("/components/0/text", errorEl.GetProperty("path").GetString());
    }

    [Fact]
    public void ErrorEnvelope_OmitsPath_WhenNull()
    {
        var error = new A2UIClientError { Code = "GENERIC", SurfaceId = "s1", Message = "oops" };
        var envelope = new A2UIClientMessage { Error = error };

        var json = JsonSerializer.Serialize(envelope);
        var doc = JsonDocument.Parse(json);
        var errorEl = doc.RootElement.GetProperty("error");

        Assert.False(errorEl.TryGetProperty("path", out _));
    }

    [Fact]
    public void ErrorEnvelope_HasExactlyTwoTopLevelProperties()
    {
        var error = new A2UIClientError { Code = "TEST", SurfaceId = "s1", Message = "test" };
        var envelope = new A2UIClientMessage { Error = error };

        var json = JsonSerializer.Serialize(envelope);
        var doc = JsonDocument.Parse(json);

        Assert.Equal(2, doc.RootElement.EnumerateObject().Count());
    }
}
