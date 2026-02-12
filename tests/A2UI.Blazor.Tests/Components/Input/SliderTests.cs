using A2UI.Blazor.Components.Input;
using A2UI.Blazor.Tests.Helpers;

namespace A2UI.Blazor.Tests.Components.Input;

public class SliderTests : IDisposable
{
    private readonly SurfaceTestContext _ctx = new();

    [Fact]
    public void Slider_LabelLinkedToInput_ViaForId()
    {
        var surface = _ctx.SetupSurface("s", [
            SurfaceTestContext.MakeComponent("vol", "Slider", new()
            {
                ["label"] = "Volume",
                ["min"] = 0,
                ["max"] = 100
            })
        ]);

        var cut = _ctx.Render<A2UISlider>(p => p
            .Add(c => c.Data, surface.Components["vol"])
            .Add(c => c.Surface, surface));

        var label = cut.Find("label");
        var input = cut.Find("input");
        Assert.Equal("vol", label.GetAttribute("for"));
        Assert.Equal("vol", input.GetAttribute("id"));
    }

    [Fact]
    public void Slider_Error_SetsAriaInvalid()
    {
        var surface = _ctx.SetupSurface("s", [
            SurfaceTestContext.MakeComponent("sl", "Slider", new()
            {
                ["label"] = "Rating",
                ["error"] = "Value out of range"
            })
        ]);

        var cut = _ctx.Render<A2UISlider>(p => p
            .Add(c => c.Data, surface.Components["sl"])
            .Add(c => c.Surface, surface));

        var input = cut.Find("input");
        Assert.Equal("true", input.GetAttribute("aria-invalid"));
    }

    [Fact]
    public void Slider_Error_RendersErrorText()
    {
        var surface = _ctx.SetupSurface("s", [
            SurfaceTestContext.MakeComponent("sl", "Slider", new()
            {
                ["label"] = "Rating",
                ["error"] = "Value out of range"
            })
        ]);

        var cut = _ctx.Render<A2UISlider>(p => p
            .Add(c => c.Data, surface.Components["sl"])
            .Add(c => c.Surface, surface));

        var errorSpan = cut.Find(".a2ui-input-error");
        Assert.Equal("Value out of range", errorSpan.TextContent.Trim());
        Assert.Equal("sl-error", errorSpan.GetAttribute("id"));

        var input = cut.Find("input");
        Assert.Equal("sl-error", input.GetAttribute("aria-describedby"));
    }

    [Fact]
    public void Slider_HelperText_LinkedViaAriaDescribedby()
    {
        var surface = _ctx.SetupSurface("s", [
            SurfaceTestContext.MakeComponent("sl", "Slider", new()
            {
                ["label"] = "Brightness",
                ["helperText"] = "Adjust screen brightness"
            })
        ]);

        var cut = _ctx.Render<A2UISlider>(p => p
            .Add(c => c.Data, surface.Components["sl"])
            .Add(c => c.Surface, surface));

        var helperSpan = cut.Find(".a2ui-input-helper");
        Assert.Equal("Adjust screen brightness", helperSpan.TextContent.Trim());
        Assert.Equal("sl-helper", helperSpan.GetAttribute("id"));

        var input = cut.Find("input");
        Assert.Equal("sl-helper", input.GetAttribute("aria-describedby"));
    }

    [Fact]
    public void Slider_Error_ClearsOnUserInput()
    {
        var surface = _ctx.SetupSurface("s", [
            SurfaceTestContext.MakeComponent("sl", "Slider", new()
            {
                ["label"] = "Rating",
                ["error"] = "Value out of range"
            })
        ]);

        var cut = _ctx.Render<A2UISlider>(p => p
            .Add(c => c.Data, surface.Components["sl"])
            .Add(c => c.Surface, surface));

        // Error visible initially
        Assert.NotNull(cut.Find(".a2ui-input-error"));

        // Move the slider
        cut.Find("input").Input("75");

        // Error should be gone
        Assert.Throws<Bunit.ElementNotFoundException>(() => cut.Find(".a2ui-input-error"));
        Assert.Null(cut.Find("input").GetAttribute("aria-invalid"));
    }

    public void Dispose() => _ctx.Dispose();
}
