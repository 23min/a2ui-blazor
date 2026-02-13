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
    public void CreateSurface_DoesNotFireEvent()
    {
        int fireCount = 0;
        _manager.OnSurfaceChanged += _ => fireCount++;

        _manager.CreateSurface("s1", null, false);

        Assert.Equal(0, fireCount);
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
    public void UpdateDataModel_BeforeReady_DoesNotFireEvent()
    {
        _manager.CreateSurface("s1", null, true);
        int fireCount = 0;
        _manager.OnSurfaceChanged += _ => fireCount++;

        _manager.UpdateDataModel("s1", "/", Parse("""{"name":"Alice"}"""));

        Assert.Equal(0, fireCount);
    }

    [Fact]
    public void UpdateDataModel_AfterReady_FiresEvent()
    {
        _manager.CreateSurface("s1", null, true);
        _manager.UpdateComponents("s1", [new() { Id = "root", Component = "Column" }]);

        int fireCount = 0;
        _manager.OnSurfaceChanged += _ => fireCount++;

        _manager.UpdateDataModel("s1", "/", Parse("""{"x":1}"""));

        Assert.Equal(1, fireCount);
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

    // ── Render Buffering ───────────────────────────────────────────

    [Fact]
    public void UpdateComponents_WithRoot_SetsReadyAndFiresEvent()
    {
        _manager.CreateSurface("s1", null, false);
        int fireCount = 0;
        _manager.OnSurfaceChanged += _ => fireCount++;

        _manager.UpdateComponents("s1", [new() { Id = "root", Component = "Column" }]);

        Assert.Equal(1, fireCount);
        Assert.True(_manager.GetSurface("s1")!.IsReady);
    }

    [Fact]
    public void UpdateComponents_WithoutRoot_DoesNotFireEvent()
    {
        _manager.CreateSurface("s1", null, false);
        int fireCount = 0;
        _manager.OnSurfaceChanged += _ => fireCount++;

        _manager.UpdateComponents("s1", [new() { Id = "child1", Component = "Text" }]);

        Assert.Equal(0, fireCount);
        Assert.False(_manager.GetSurface("s1")!.IsReady);
    }

    [Fact]
    public void UpdateComponents_AfterReady_AlwaysFiresEvent()
    {
        _manager.CreateSurface("s1", null, false);
        _manager.UpdateComponents("s1", [new() { Id = "root", Component = "Column" }]);

        int fireCount = 0;
        _manager.OnSurfaceChanged += _ => fireCount++;

        _manager.UpdateComponents("s1", [new() { Id = "child1", Component = "Text" }]);

        Assert.Equal(1, fireCount);
    }

    [Fact]
    public void FullSequence_CreateDataComponents_FiresSingleEvent()
    {
        int fireCount = 0;
        _manager.OnSurfaceChanged += _ => fireCount++;

        _manager.CreateSurface("s1", null, true);
        _manager.UpdateDataModel("s1", "/", Parse("""{"items":["a","b"]}"""));
        _manager.UpdateComponents("s1", [
            new() { Id = "root", Component = "Column" },
            new() { Id = "title", Component = "Text" }
        ]);

        Assert.Equal(1, fireCount);
    }

    [Fact]
    public void DeleteSurface_BeforeReady_StillFiresEvent()
    {
        _manager.CreateSurface("s1", null, false);
        int fireCount = 0;
        _manager.OnSurfaceChanged += _ => fireCount++;

        _manager.DeleteSurface("s1");

        Assert.Equal(1, fireCount);
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

    // ── Theme ─────────────────────────────────────────────────────────

    [Fact]
    public void CreateSurface_StoresTheme()
    {
        var theme = Parse("""{"primaryColor":"#2196F3"}""");
        _manager.CreateSurface("s1", null, false, theme);
        var surface = _manager.GetSurface("s1");

        Assert.NotNull(surface!.Theme);
        Assert.Equal("#2196F3", surface.Theme.Value.GetProperty("primaryColor").GetString());
    }

    [Fact]
    public void CreateSurface_NullTheme_StoresNull()
    {
        _manager.CreateSurface("s1", null, false, null);
        var surface = _manager.GetSurface("s1");

        Assert.Null(surface!.Theme);
    }

    // ── OnSurfaceCreated ───────────────────────────────────────────

    [Fact]
    public void CreateSurface_FiresOnSurfaceCreated()
    {
        string? createdId = null;
        _manager.OnSurfaceCreated += id => createdId = id;

        _manager.CreateSurface("s1", null, false);

        Assert.Equal("s1", createdId);
    }

    [Fact]
    public void CreateSurface_FiresOnSurfaceCreated_BeforeOnSurfaceChanged()
    {
        var events = new List<string>();
        _manager.OnSurfaceCreated += _ => events.Add("created");
        _manager.OnSurfaceChanged += _ => events.Add("changed");

        _manager.CreateSurface("s1", null, false);
        // OnSurfaceCreated fires, but OnSurfaceChanged does not fire on create
        Assert.Equal(["created"], events);
    }

    // ── OnSurfaceDeleted ───────────────────────────────────────────

    [Fact]
    public void DeleteSurface_FiresOnSurfaceDeleted()
    {
        _manager.CreateSurface("s1", null, false);

        string? deletedId = null;
        _manager.OnSurfaceDeleted += id => deletedId = id;

        _manager.DeleteSurface("s1");

        Assert.Equal("s1", deletedId);
    }

    [Fact]
    public void DeleteSurface_UnknownSurface_DoesNotFireOnSurfaceDeleted()
    {
        int fireCount = 0;
        _manager.OnSurfaceDeleted += _ => fireCount++;

        _manager.DeleteSurface("nonexistent");

        Assert.Equal(0, fireCount);
    }

    [Fact]
    public void DeleteSurface_FiresOnSurfaceDeleted_BeforeOnSurfaceChanged()
    {
        _manager.CreateSurface("s1", null, false);

        var events = new List<string>();
        _manager.OnSurfaceDeleted += _ => events.Add("deleted");
        _manager.OnSurfaceChanged += _ => events.Add("changed");

        _manager.DeleteSurface("s1");

        Assert.Equal(["deleted", "changed"], events);
    }

    // ── Validation Errors ──────────────────────────────────────────

    [Fact]
    public void SetValidationError_StoresError()
    {
        _manager.CreateSurface("s1", null, false);
        _manager.UpdateComponents("s1", [new() { Id = "root", Component = "Column" }]);

        _manager.SetValidationError("s1", "/email", "Invalid email");

        var surface = _manager.GetSurface("s1");
        Assert.Equal("Invalid email", surface!.ValidationErrors["/email"]);
    }

    [Fact]
    public void SetValidationError_FiresSurfaceChanged()
    {
        _manager.CreateSurface("s1", null, false);
        _manager.UpdateComponents("s1", [new() { Id = "root", Component = "Column" }]);

        int fireCount = 0;
        _manager.OnSurfaceChanged += _ => fireCount++;

        _manager.SetValidationError("s1", "/email", "Invalid");

        Assert.Equal(1, fireCount);
    }

    [Fact]
    public void ClearValidationError_RemovesSpecificError()
    {
        _manager.CreateSurface("s1", null, false);
        _manager.UpdateComponents("s1", [new() { Id = "root", Component = "Column" }]);

        _manager.SetValidationError("s1", "/email", "Invalid");
        _manager.SetValidationError("s1", "/name", "Required");

        _manager.ClearValidationError("s1", "/email");

        var surface = _manager.GetSurface("s1");
        Assert.False(surface!.ValidationErrors.ContainsKey("/email"));
        Assert.Equal("Required", surface.ValidationErrors["/name"]);
    }

    [Fact]
    public void SetValidationError_UnknownSurface_DoesNotThrow()
    {
        var ex = Record.Exception(() => _manager.SetValidationError("missing", "/x", "err"));
        Assert.Null(ex);
    }
}
