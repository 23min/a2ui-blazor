using System.Text.Json;
using A2UI.Blazor.Protocol;
using A2UI.Blazor.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace A2UI.Blazor.Tests.Services;

public class MessageDispatcherTests
{
    private readonly SurfaceManager _manager = new(NullLogger<SurfaceManager>.Instance);
    private readonly MessageDispatcher _dispatcher;

    public MessageDispatcherTests()
    {
        _dispatcher = new MessageDispatcher(_manager, NullLogger<MessageDispatcher>.Instance);
    }

    [Fact]
    public void Dispatch_CreateSurface_CreatesSurface()
    {
        _dispatcher.Dispatch(new A2UIMessage
        {
            Type = "createSurface",
            SurfaceId = "s1",
            SendDataModel = true
        });

        var surface = _manager.GetSurface("s1");
        Assert.NotNull(surface);
        Assert.True(surface.SendDataModel);
    }

    [Fact]
    public void Dispatch_UpdateComponents_AddsComponents()
    {
        _dispatcher.Dispatch(new A2UIMessage { Type = "createSurface", SurfaceId = "s1" });
        _dispatcher.Dispatch(new A2UIMessage
        {
            Type = "updateComponents",
            SurfaceId = "s1",
            Components = [new() { Id = "root", Component = "Column" }]
        });

        var surface = _manager.GetSurface("s1");
        Assert.Single(surface!.Components);
        Assert.Equal("Column", surface.Components["root"].Component);
    }

    [Fact]
    public void Dispatch_UpdateDataModel_SetsData()
    {
        _dispatcher.Dispatch(new A2UIMessage { Type = "createSurface", SurfaceId = "s1" });
        _dispatcher.Dispatch(new A2UIMessage
        {
            Type = "updateDataModel",
            SurfaceId = "s1",
            Path = "/",
            Value = JsonDocument.Parse("""{"count":5}""").RootElement
        });

        var val = _manager.ResolveBinding("s1", "/count");
        Assert.Equal(5, val?.GetInt32());
    }

    [Fact]
    public void Dispatch_DeleteSurface_RemovesSurface()
    {
        _dispatcher.Dispatch(new A2UIMessage { Type = "createSurface", SurfaceId = "s1" });
        _dispatcher.Dispatch(new A2UIMessage { Type = "deleteSurface", SurfaceId = "s1" });

        Assert.Null(_manager.GetSurface("s1"));
    }

    [Fact]
    public void Dispatch_UnknownType_DoesNotThrow()
    {
        var ex = Record.Exception(() =>
            _dispatcher.Dispatch(new A2UIMessage { Type = "unknownType", SurfaceId = "s1" }));
        Assert.Null(ex);
    }

    [Fact]
    public void Dispatch_NullSurfaceId_DoesNotThrow()
    {
        var ex = Record.Exception(() =>
            _dispatcher.Dispatch(new A2UIMessage { Type = "createSurface", SurfaceId = null }));
        Assert.Null(ex);
    }

    [Fact]
    public void Dispatch_UpdateComponents_NullComponents_DoesNotThrow()
    {
        _dispatcher.Dispatch(new A2UIMessage { Type = "createSurface", SurfaceId = "s1" });

        var ex = Record.Exception(() =>
            _dispatcher.Dispatch(new A2UIMessage { Type = "updateComponents", SurfaceId = "s1", Components = null }));
        Assert.Null(ex);
    }

    [Fact]
    public void Dispatch_CreateSurface_PassesTheme()
    {
        var theme = JsonDocument.Parse("""{"primaryColor":"#FF0000"}""").RootElement;
        _dispatcher.Dispatch(new A2UIMessage
        {
            Type = "createSurface",
            SurfaceId = "s1",
            Theme = theme
        });

        var surface = _manager.GetSurface("s1");
        Assert.NotNull(surface!.Theme);
        Assert.Equal("#FF0000", surface.Theme.Value.GetProperty("primaryColor").GetString());
    }

    [Fact]
    public void Dispatch_FullSequence_CreatesRenderableSurface()
    {
        // Simulate the exact message sequence the Python server sends
        _dispatcher.Dispatch(new A2UIMessage
        {
            Type = "createSurface",
            SurfaceId = "test",
            SendDataModel = true
        });

        _dispatcher.Dispatch(new A2UIMessage
        {
            Type = "updateDataModel",
            SurfaceId = "test",
            Path = "/",
            Value = JsonDocument.Parse("""{"items":["a","b"]}""").RootElement
        });

        _dispatcher.Dispatch(new A2UIMessage
        {
            Type = "updateComponents",
            SurfaceId = "test",
            Components =
            [
                new() { Id = "root", Component = "Column" },
                new() { Id = "title", Component = "Text" }
            ]
        });

        var surface = _manager.GetSurface("test");
        Assert.NotNull(surface);
        Assert.NotNull(surface.GetRoot());
        Assert.Equal(2, surface.Components.Count);
        Assert.NotNull(surface.DataModel);

        var items = _manager.ResolveBinding("test", "/items");
        Assert.Equal(JsonValueKind.Array, items?.ValueKind);
    }
}
