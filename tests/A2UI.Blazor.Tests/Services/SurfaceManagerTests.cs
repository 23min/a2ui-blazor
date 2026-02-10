using System.Text.Json;
using A2UI.Blazor.Protocol;
using A2UI.Blazor.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace A2UI.Blazor.Tests.Services;

public class SurfaceManagerTests
{
    private readonly SurfaceManager _manager = new(NullLogger<SurfaceManager>.Instance);

    private static JsonElement Parse(string json) =>
        JsonDocument.Parse(json).RootElement;

    // ── CreateSurface ───────────────────────────────────────────────

    [Fact]
    public void CreateSurface_StoresSurface()
    {
        _manager.CreateSurface("s1", null, false);
        var surface = _manager.GetSurface("s1");

        Assert.NotNull(surface);
        Assert.Equal("s1", surface.SurfaceId);
    }

    [Fact]
    public void CreateSurface_FiresOnSurfaceChanged()
    {
        string? firedId = null;
        _manager.OnSurfaceChanged += id => firedId = id;

        _manager.CreateSurface("s1", null, false);

        Assert.Equal("s1", firedId);
    }

    [Fact]
    public void CreateSurface_SetsCatalogAndSendDataModel()
    {
        _manager.CreateSurface("s1", "catalog-1", true);
        var surface = _manager.GetSurface("s1");

        Assert.Equal("catalog-1", surface!.CatalogId);
        Assert.True(surface.SendDataModel);
    }

    // ── GetSurface ──────────────────────────────────────────────────

    [Fact]
    public void GetSurface_UnknownId_ReturnsNull()
    {
        Assert.Null(_manager.GetSurface("nonexistent"));
    }

    // ── UpdateComponents ────────────────────────────────────────────

    [Fact]
    public void UpdateComponents_AddsComponentsToSurface()
    {
        _manager.CreateSurface("s1", null, false);
        var components = new List<A2UIComponentData>
        {
            new() { Id = "root", Component = "Column" },
            new() { Id = "text1", Component = "Text" }
        };

        _manager.UpdateComponents("s1", components);
        var surface = _manager.GetSurface("s1");

        Assert.Equal(2, surface!.Components.Count);
        Assert.Equal("Column", surface.Components["root"].Component);
    }

    [Fact]
    public void UpdateComponents_UnknownSurface_DoesNotThrow()
    {
        var components = new List<A2UIComponentData>
        {
            new() { Id = "root", Component = "Column" }
        };

        var ex = Record.Exception(() => _manager.UpdateComponents("missing", components));
        Assert.Null(ex);
    }

    [Fact]
    public void UpdateComponents_FiresEvent()
    {
        _manager.CreateSurface("s1", null, false);
        string? firedId = null;
        _manager.OnSurfaceChanged += id => firedId = id;

        _manager.UpdateComponents("s1", [new() { Id = "root", Component = "Column" }]);

        Assert.Equal("s1", firedId);
    }

    // ── UpdateDataModel ─────────────────────────────────────────────

    [Fact]
    public void UpdateDataModel_RootPath_ReplacesEntireModel()
    {
        _manager.CreateSurface("s1", null, true);
        var data = Parse("""{"name":"Alice"}""");

        _manager.UpdateDataModel("s1", "/", data);

        var resolved = _manager.ResolveBinding("s1", "/name");
        Assert.Equal("Alice", resolved?.GetString());
    }

    [Fact]
    public void UpdateDataModel_NullPath_ReplacesEntireModel()
    {
        _manager.CreateSurface("s1", null, true);
        var data = Parse("""{"x":42}""");

        _manager.UpdateDataModel("s1", null, data);

        var resolved = _manager.ResolveBinding("s1", "/x");
        Assert.Equal(42, resolved?.GetInt32());
    }

    [Fact]
    public void UpdateDataModel_EmptyStringPath_ReplacesEntireModel()
    {
        _manager.CreateSurface("s1", null, true);
        var data = Parse("""{"y":"yes"}""");

        _manager.UpdateDataModel("s1", "", data);

        var resolved = _manager.ResolveBinding("s1", "/y");
        Assert.Equal("yes", resolved?.GetString());
    }

    [Fact]
    public void UpdateDataModel_SpecificPath_PatchesModel()
    {
        _manager.CreateSurface("s1", null, true);
        _manager.UpdateDataModel("s1", "/", Parse("""{"a":1,"b":2}"""));

        _manager.UpdateDataModel("s1", "/a", Parse("99"));

        Assert.Equal(99, _manager.ResolveBinding("s1", "/a")?.GetInt32());
        Assert.Equal(2, _manager.ResolveBinding("s1", "/b")?.GetInt32());
    }

    [Fact]
    public void UpdateDataModel_FiresEvent()
    {
        _manager.CreateSurface("s1", null, true);
        string? firedId = null;
        _manager.OnSurfaceChanged += id => firedId = id;

        _manager.UpdateDataModel("s1", "/", Parse("""{}"""));

        Assert.Equal("s1", firedId);
    }

    [Fact]
    public void UpdateDataModel_UnknownSurface_DoesNotThrow()
    {
        var ex = Record.Exception(() => _manager.UpdateDataModel("missing", "/", Parse("{}")));
        Assert.Null(ex);
    }

    // ── DeleteSurface ───────────────────────────────────────────────

    [Fact]
    public void DeleteSurface_RemovesSurface()
    {
        _manager.CreateSurface("s1", null, false);
        _manager.DeleteSurface("s1");

        Assert.Null(_manager.GetSurface("s1"));
    }

    [Fact]
    public void DeleteSurface_FiresEvent()
    {
        _manager.CreateSurface("s1", null, false);
        string? firedId = null;
        _manager.OnSurfaceChanged += id => firedId = id;

        _manager.DeleteSurface("s1");

        Assert.Equal("s1", firedId);
    }

    // ── ResolveBinding ──────────────────────────────────────────────

    [Fact]
    public void ResolveBinding_MissingSurface_ReturnsNull()
    {
        Assert.Null(_manager.ResolveBinding("missing", "/x"));
    }

    [Fact]
    public void ResolveBinding_NoDataModel_ReturnsNull()
    {
        _manager.CreateSurface("s1", null, false);
        Assert.Null(_manager.ResolveBinding("s1", "/x"));
    }
}
