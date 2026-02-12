using A2UI.Blazor.Components.Input;
using A2UI.Blazor.Tests.Helpers;

namespace A2UI.Blazor.Tests.Components.Input;

public class CheckBoxTests : IDisposable
{
    private readonly SurfaceTestContext _ctx = new();

    [Fact]
    public void CheckBox_Error_SetsAriaInvalid()
    {
        var surface = _ctx.SetupSurface("s", [
            SurfaceTestContext.MakeComponent("cb", "CheckBox", new()
            {
                ["label"] = "I agree",
                ["error"] = "You must accept the terms"
            })
        ]);

        var cut = _ctx.Render<A2UICheckBox>(p => p
            .Add(c => c.Data, surface.Components["cb"])
            .Add(c => c.Surface, surface));

        var input = cut.Find("input");
        Assert.Equal("true", input.GetAttribute("aria-invalid"));
    }

    [Fact]
    public void CheckBox_Error_RendersErrorText()
    {
        var surface = _ctx.SetupSurface("s", [
            SurfaceTestContext.MakeComponent("cb", "CheckBox", new()
            {
                ["label"] = "I agree",
                ["error"] = "You must accept the terms"
            })
        ]);

        var cut = _ctx.Render<A2UICheckBox>(p => p
            .Add(c => c.Data, surface.Components["cb"])
            .Add(c => c.Surface, surface));

        var errorSpan = cut.Find(".a2ui-input-error");
        Assert.Equal("You must accept the terms", errorSpan.TextContent.Trim());
        Assert.Equal("cb-error", errorSpan.GetAttribute("id"));

        var input = cut.Find("input");
        Assert.Equal("cb-error", input.GetAttribute("aria-describedby"));
    }

    [Fact]
    public void CheckBox_HelperText_LinkedViaAriaDescribedby()
    {
        var surface = _ctx.SetupSurface("s", [
            SurfaceTestContext.MakeComponent("cb", "CheckBox", new()
            {
                ["label"] = "Newsletter",
                ["helperText"] = "Receive weekly updates"
            })
        ]);

        var cut = _ctx.Render<A2UICheckBox>(p => p
            .Add(c => c.Data, surface.Components["cb"])
            .Add(c => c.Surface, surface));

        var helperSpan = cut.Find(".a2ui-input-helper");
        Assert.Equal("Receive weekly updates", helperSpan.TextContent.Trim());
        Assert.Equal("cb-helper", helperSpan.GetAttribute("id"));

        var input = cut.Find("input");
        Assert.Equal("cb-helper", input.GetAttribute("aria-describedby"));
    }

    [Fact]
    public void CheckBox_Error_ClearsOnUserInteraction()
    {
        var surface = _ctx.SetupSurface("s", [
            SurfaceTestContext.MakeComponent("cb", "CheckBox", new()
            {
                ["label"] = "I agree",
                ["error"] = "You must accept the terms"
            })
        ]);

        var cut = _ctx.Render<A2UICheckBox>(p => p
            .Add(c => c.Data, surface.Components["cb"])
            .Add(c => c.Surface, surface));

        // Error visible initially
        Assert.NotNull(cut.Find(".a2ui-input-error"));

        // Toggle the checkbox
        cut.Find("input").Change(true);

        // Error should be gone
        Assert.Throws<Bunit.ElementNotFoundException>(() => cut.Find(".a2ui-input-error"));
        Assert.Null(cut.Find("input").GetAttribute("aria-invalid"));
    }

    public void Dispose() => _ctx.Dispose();
}
