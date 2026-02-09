using A2UI.Blazor.Components.Input;
using A2UI.Blazor.Protocol;
using A2UI.Blazor.Tests.Helpers;
using AngleSharp.Dom;

namespace A2UI.Blazor.Tests.Components.Input;

public class TextFieldTests : IDisposable
{
    private readonly SurfaceTestContext _ctx = new();

    [Fact]
    public void Renders_InputWithPlaceholder()
    {
        var surface = _ctx.SetupSurface("s", [
            SurfaceTestContext.MakeComponent("tf", "TextField", new()
            {
                ["placeholder"] = "Type here..."
            })
        ]);

        var cut = _ctx.RenderComponent<A2UITextField>(p => p
            .Add(c => c.Data, surface.Components["tf"])
            .Add(c => c.Surface, surface));

        var input = cut.Find("input");
        Assert.Equal("Type here...", input.GetAttribute("placeholder"));
    }

    [Fact]
    public void Renders_LabelWhenProvided()
    {
        var surface = _ctx.SetupSurface("s", [
            SurfaceTestContext.MakeComponent("tf", "TextField", new()
            {
                ["label"] = "Name"
            })
        ]);

        var cut = _ctx.RenderComponent<A2UITextField>(p => p
            .Add(c => c.Data, surface.Components["tf"])
            .Add(c => c.Surface, surface));

        var label = cut.Find("label");
        Assert.Equal("Name", label.TextContent.Trim());
    }

    [Fact]
    public void Input_FiresOnAction_WithTypedValue()
    {
        var surface = _ctx.SetupSurface("s", [
            SurfaceTestContext.MakeComponent("tf", "TextField", new()
            {
                ["action"] = new { @event = new { name = "search" } }
            })
        ]);

        A2UIUserAction? capturedAction = null;

        var cut = _ctx.RenderComponent<A2UITextField>(p => p
            .Add(c => c.Data, surface.Components["tf"])
            .Add(c => c.Surface, surface)
            .Add(c => c.OnAction, action => capturedAction = action));

        cut.Find("input").Input("hello");

        Assert.NotNull(capturedAction);
        Assert.Equal("search", capturedAction.Name);
        Assert.Equal("hello", capturedAction.Context?["value"]?.ToString());
    }

    [Fact]
    public void Renders_TextareaWhenMultiline()
    {
        var surface = _ctx.SetupSurface("s", [
            SurfaceTestContext.MakeComponent("tf", "TextField", new()
            {
                ["multiline"] = true,
                ["placeholder"] = "Multi..."
            })
        ]);

        var cut = _ctx.RenderComponent<A2UITextField>(p => p
            .Add(c => c.Data, surface.Components["tf"])
            .Add(c => c.Surface, surface));

        Assert.Throws<Bunit.ElementNotFoundException>(() => cut.Find("input"));
        var textarea = cut.Find("textarea");
        Assert.Equal("Multi...", textarea.GetAttribute("placeholder"));
    }

    [Fact]
    public void Disabled_Input_HasDisabledAttribute()
    {
        var surface = _ctx.SetupSurface("s", [
            SurfaceTestContext.MakeComponent("tf", "TextField", new()
            {
                ["disabled"] = true
            })
        ]);

        var cut = _ctx.RenderComponent<A2UITextField>(p => p
            .Add(c => c.Data, surface.Components["tf"])
            .Add(c => c.Surface, surface));

        Assert.True(cut.Find("input").HasAttribute("disabled"));
    }

    public void Dispose() => _ctx.Dispose();
}
