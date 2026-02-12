using A2UI.Blazor.Components.Input;
using A2UI.Blazor.Tests.Helpers;

namespace A2UI.Blazor.Tests.Components.Input;

public class DateTimeInputTests : IDisposable
{
    private readonly SurfaceTestContext _ctx = new();

    [Fact]
    public void DateTimeInput_LabelLinkedToInput_ViaForId()
    {
        var surface = _ctx.SetupSurface("s", [
            SurfaceTestContext.MakeComponent("dob", "DateTimeInput", new()
            {
                ["label"] = "Date of Birth",
                ["inputType"] = "date"
            })
        ]);

        var cut = _ctx.Render<A2UIDateTimeInput>(p => p
            .Add(c => c.Data, surface.Components["dob"])
            .Add(c => c.Surface, surface));

        var label = cut.Find("label");
        var input = cut.Find("input");
        Assert.Equal("dob", label.GetAttribute("for"));
        Assert.Equal("dob", input.GetAttribute("id"));
    }

    [Fact]
    public void DateTimeInput_Error_SetsAriaInvalid()
    {
        var surface = _ctx.SetupSurface("s", [
            SurfaceTestContext.MakeComponent("dt", "DateTimeInput", new()
            {
                ["label"] = "Start Date",
                ["error"] = "Date is required"
            })
        ]);

        var cut = _ctx.Render<A2UIDateTimeInput>(p => p
            .Add(c => c.Data, surface.Components["dt"])
            .Add(c => c.Surface, surface));

        var input = cut.Find("input");
        Assert.Equal("true", input.GetAttribute("aria-invalid"));
    }

    [Fact]
    public void DateTimeInput_Error_RendersErrorText()
    {
        var surface = _ctx.SetupSurface("s", [
            SurfaceTestContext.MakeComponent("dt", "DateTimeInput", new()
            {
                ["label"] = "Start Date",
                ["error"] = "Date is required"
            })
        ]);

        var cut = _ctx.Render<A2UIDateTimeInput>(p => p
            .Add(c => c.Data, surface.Components["dt"])
            .Add(c => c.Surface, surface));

        var errorSpan = cut.Find(".a2ui-input-error");
        Assert.Equal("Date is required", errorSpan.TextContent.Trim());
        Assert.Equal("dt-error", errorSpan.GetAttribute("id"));

        var input = cut.Find("input");
        Assert.Equal("dt-error", input.GetAttribute("aria-describedby"));
    }

    [Fact]
    public void DateTimeInput_HelperText_LinkedViaAriaDescribedby()
    {
        var surface = _ctx.SetupSurface("s", [
            SurfaceTestContext.MakeComponent("dt", "DateTimeInput", new()
            {
                ["label"] = "Appointment",
                ["helperText"] = "Select a weekday"
            })
        ]);

        var cut = _ctx.Render<A2UIDateTimeInput>(p => p
            .Add(c => c.Data, surface.Components["dt"])
            .Add(c => c.Surface, surface));

        var helperSpan = cut.Find(".a2ui-input-helper");
        Assert.Equal("Select a weekday", helperSpan.TextContent.Trim());
        Assert.Equal("dt-helper", helperSpan.GetAttribute("id"));

        var input = cut.Find("input");
        Assert.Equal("dt-helper", input.GetAttribute("aria-describedby"));
    }

    [Fact]
    public void DateTimeInput_Error_ClearsOnUserInput()
    {
        var surface = _ctx.SetupSurface("s", [
            SurfaceTestContext.MakeComponent("dt", "DateTimeInput", new()
            {
                ["label"] = "Start Date",
                ["error"] = "Date is required"
            })
        ]);

        var cut = _ctx.Render<A2UIDateTimeInput>(p => p
            .Add(c => c.Data, surface.Components["dt"])
            .Add(c => c.Surface, surface));

        // Error visible initially
        Assert.NotNull(cut.Find(".a2ui-input-error"));

        // Change the date
        cut.Find("input").Change("2024-01-15");

        // Error should be gone
        Assert.Throws<Bunit.ElementNotFoundException>(() => cut.Find(".a2ui-input-error"));
        Assert.Null(cut.Find("input").GetAttribute("aria-invalid"));
    }

    public void Dispose() => _ctx.Dispose();
}
