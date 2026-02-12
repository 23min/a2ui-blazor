using A2UI.Blazor.Components.Input;
using A2UI.Blazor.Tests.Helpers;

namespace A2UI.Blazor.Tests.Components.Input;

public class ChoicePickerTests : IDisposable
{
    private readonly SurfaceTestContext _ctx = new();

    [Fact]
    public void ChoicePicker_LabelLinkedToSelect_ViaForId()
    {
        var surface = _ctx.SetupSurface("s", [
            SurfaceTestContext.MakeComponent("color", "ChoicePicker", new()
            {
                ["label"] = "Color",
                ["options"] = new[] { "Red", "Blue" }
            })
        ]);

        var cut = _ctx.Render<A2UIChoicePicker>(p => p
            .Add(c => c.Data, surface.Components["color"])
            .Add(c => c.Surface, surface));

        var label = cut.Find("label");
        var select = cut.Find("select");
        Assert.Equal("color", label.GetAttribute("for"));
        Assert.Equal("color", select.GetAttribute("id"));
    }

    [Fact]
    public void ChoicePicker_Error_SetsAriaInvalid()
    {
        var surface = _ctx.SetupSurface("s", [
            SurfaceTestContext.MakeComponent("cp", "ChoicePicker", new()
            {
                ["label"] = "Size",
                ["options"] = new[] { "S", "M", "L" },
                ["error"] = "Please select a size"
            })
        ]);

        var cut = _ctx.Render<A2UIChoicePicker>(p => p
            .Add(c => c.Data, surface.Components["cp"])
            .Add(c => c.Surface, surface));

        var select = cut.Find("select");
        Assert.Equal("true", select.GetAttribute("aria-invalid"));
    }

    [Fact]
    public void ChoicePicker_Error_RendersErrorText()
    {
        var surface = _ctx.SetupSurface("s", [
            SurfaceTestContext.MakeComponent("cp", "ChoicePicker", new()
            {
                ["label"] = "Size",
                ["options"] = new[] { "S", "M", "L" },
                ["error"] = "Please select a size"
            })
        ]);

        var cut = _ctx.Render<A2UIChoicePicker>(p => p
            .Add(c => c.Data, surface.Components["cp"])
            .Add(c => c.Surface, surface));

        var errorSpan = cut.Find(".a2ui-input-error");
        Assert.Equal("Please select a size", errorSpan.TextContent.Trim());
        Assert.Equal("cp-error", errorSpan.GetAttribute("id"));

        var select = cut.Find("select");
        Assert.Equal("cp-error", select.GetAttribute("aria-describedby"));
    }

    [Fact]
    public void ChoicePicker_HelperText_LinkedViaAriaDescribedby()
    {
        var surface = _ctx.SetupSurface("s", [
            SurfaceTestContext.MakeComponent("cp", "ChoicePicker", new()
            {
                ["label"] = "Theme",
                ["options"] = new[] { "Light", "Dark" },
                ["helperText"] = "Choose your preferred theme"
            })
        ]);

        var cut = _ctx.Render<A2UIChoicePicker>(p => p
            .Add(c => c.Data, surface.Components["cp"])
            .Add(c => c.Surface, surface));

        var helperSpan = cut.Find(".a2ui-input-helper");
        Assert.Equal("Choose your preferred theme", helperSpan.TextContent.Trim());
        Assert.Equal("cp-helper", helperSpan.GetAttribute("id"));

        var select = cut.Find("select");
        Assert.Equal("cp-helper", select.GetAttribute("aria-describedby"));
    }

    [Fact]
    public void ChoicePicker_Error_ClearsOnUserSelection()
    {
        var surface = _ctx.SetupSurface("s", [
            SurfaceTestContext.MakeComponent("cp", "ChoicePicker", new()
            {
                ["label"] = "Size",
                ["options"] = new[] { "S", "M", "L" },
                ["error"] = "Please select a size"
            })
        ]);

        var cut = _ctx.Render<A2UIChoicePicker>(p => p
            .Add(c => c.Data, surface.Components["cp"])
            .Add(c => c.Surface, surface));

        // Error visible initially
        Assert.NotNull(cut.Find(".a2ui-input-error"));

        // Select an option
        cut.Find("select").Change("M");

        // Error should be gone
        Assert.Throws<Bunit.ElementNotFoundException>(() => cut.Find(".a2ui-input-error"));
        Assert.Null(cut.Find("select").GetAttribute("aria-invalid"));
    }

    public void Dispose() => _ctx.Dispose();
}
