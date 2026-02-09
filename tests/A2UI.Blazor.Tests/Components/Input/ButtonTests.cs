using A2UI.Blazor.Components.Input;
using A2UI.Blazor.Protocol;
using A2UI.Blazor.Tests.Helpers;

namespace A2UI.Blazor.Tests.Components.Input;

public class ButtonTests : IDisposable
{
    private readonly SurfaceTestContext _ctx = new();

    [Fact]
    public void Renders_ButtonWithLabel()
    {
        var surface = _ctx.SetupSurface("s", [
            SurfaceTestContext.MakeComponent("btn", "Button", new()
            {
                ["label"] = "Click Me"
            })
        ]);

        var cut = _ctx.RenderComponent<A2UIButton>(p => p
            .Add(c => c.Data, surface.Components["btn"])
            .Add(c => c.Surface, surface));

        var button = cut.Find("button");
        Assert.Equal("Click Me", button.TextContent.Trim());
    }

    [Fact]
    public void Applies_VariantCssClass()
    {
        var surface = _ctx.SetupSurface("s", [
            SurfaceTestContext.MakeComponent("btn", "Button", new()
            {
                ["label"] = "Go",
                ["variant"] = "secondary"
            })
        ]);

        var cut = _ctx.RenderComponent<A2UIButton>(p => p
            .Add(c => c.Data, surface.Components["btn"])
            .Add(c => c.Surface, surface));

        var button = cut.Find("button");
        Assert.Contains("a2ui-button-secondary", button.ClassName);
    }

    [Fact]
    public void DefaultVariant_IsPrimary()
    {
        var surface = _ctx.SetupSurface("s", [
            SurfaceTestContext.MakeComponent("btn", "Button", new()
            {
                ["label"] = "Go"
            })
        ]);

        var cut = _ctx.RenderComponent<A2UIButton>(p => p
            .Add(c => c.Data, surface.Components["btn"])
            .Add(c => c.Surface, surface));

        var button = cut.Find("button");
        Assert.Contains("a2ui-button-primary", button.ClassName);
    }

    [Fact]
    public void Click_FiresOnAction_WithCorrectEventName()
    {
        var surface = _ctx.SetupSurface("s", [
            SurfaceTestContext.MakeComponent("btn", "Button", new()
            {
                ["label"] = "Search",
                ["action"] = new { @event = new { name = "search" } }
            })
        ]);

        A2UIUserAction? capturedAction = null;

        var cut = _ctx.RenderComponent<A2UIButton>(p => p
            .Add(c => c.Data, surface.Components["btn"])
            .Add(c => c.Surface, surface)
            .Add(c => c.OnAction, action => capturedAction = action));

        cut.Find("button").Click();

        Assert.NotNull(capturedAction);
        Assert.Equal("search", capturedAction.Name);
        Assert.Equal("s", capturedAction.SurfaceId);
        Assert.Equal("btn", capturedAction.SourceComponentId);
    }

    [Fact]
    public void Disabled_Button_HasDisabledAttribute()
    {
        var surface = _ctx.SetupSurface("s", [
            SurfaceTestContext.MakeComponent("btn", "Button", new()
            {
                ["label"] = "No",
                ["disabled"] = true
            })
        ]);

        var cut = _ctx.RenderComponent<A2UIButton>(p => p
            .Add(c => c.Data, surface.Components["btn"])
            .Add(c => c.Surface, surface));

        Assert.True(cut.Find("button").HasAttribute("disabled"));
    }

    public void Dispose() => _ctx.Dispose();
}
