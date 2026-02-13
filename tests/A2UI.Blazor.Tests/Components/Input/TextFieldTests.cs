using System.Text.Json;
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

        var cut = _ctx.Render<A2UITextField>(p => p
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

        var cut = _ctx.Render<A2UITextField>(p => p
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

        var cut = _ctx.Render<A2UITextField>(p => p
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

        var cut = _ctx.Render<A2UITextField>(p => p
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

        var cut = _ctx.Render<A2UITextField>(p => p
            .Add(c => c.Data, surface.Components["tf"])
            .Add(c => c.Surface, surface));

        Assert.True(cut.Find("input").HasAttribute("disabled"));
    }

    [Fact]
    public void TextField_LabelLinkedToInput_ViaForId()
    {
        var surface = _ctx.SetupSurface("s", [
            SurfaceTestContext.MakeComponent("email", "TextField", new()
            {
                ["label"] = "Email"
            })
        ]);

        var cut = _ctx.Render<A2UITextField>(p => p
            .Add(c => c.Data, surface.Components["email"])
            .Add(c => c.Surface, surface));

        var label = cut.Find("label");
        var input = cut.Find("input");
        Assert.Equal("email", label.GetAttribute("for"));
        Assert.Equal("email", input.GetAttribute("id"));
    }

    [Fact]
    public void TextField_Error_SetsAriaInvalid()
    {
        var surface = _ctx.SetupSurface("s", [
            SurfaceTestContext.MakeComponent("tf", "TextField", new()
            {
                ["label"] = "Name",
                ["error"] = "Required field"
            })
        ]);

        var cut = _ctx.Render<A2UITextField>(p => p
            .Add(c => c.Data, surface.Components["tf"])
            .Add(c => c.Surface, surface));

        var input = cut.Find("input");
        Assert.Equal("true", input.GetAttribute("aria-invalid"));
    }

    [Fact]
    public void TextField_Error_RendersErrorText()
    {
        var surface = _ctx.SetupSurface("s", [
            SurfaceTestContext.MakeComponent("tf", "TextField", new()
            {
                ["label"] = "Name",
                ["error"] = "Required field"
            })
        ]);

        var cut = _ctx.Render<A2UITextField>(p => p
            .Add(c => c.Data, surface.Components["tf"])
            .Add(c => c.Surface, surface));

        var errorSpan = cut.Find(".a2ui-input-error");
        Assert.Equal("Required field", errorSpan.TextContent.Trim());
        Assert.Equal("tf-error", errorSpan.GetAttribute("id"));

        var input = cut.Find("input");
        Assert.Equal("tf-error", input.GetAttribute("aria-describedby"));
    }

    [Fact]
    public void TextField_HelperText_LinkedViaAriaDescribedby()
    {
        var surface = _ctx.SetupSurface("s", [
            SurfaceTestContext.MakeComponent("tf", "TextField", new()
            {
                ["label"] = "Email",
                ["helperText"] = "We won't share your email"
            })
        ]);

        var cut = _ctx.Render<A2UITextField>(p => p
            .Add(c => c.Data, surface.Components["tf"])
            .Add(c => c.Surface, surface));

        var helperSpan = cut.Find(".a2ui-input-helper");
        Assert.Equal("We won't share your email", helperSpan.TextContent.Trim());
        Assert.Equal("tf-helper", helperSpan.GetAttribute("id"));

        var input = cut.Find("input");
        Assert.Equal("tf-helper", input.GetAttribute("aria-describedby"));
    }

    [Fact]
    public void TextField_Error_ClearsOnUserInput()
    {
        var surface = _ctx.SetupSurface("s", [
            SurfaceTestContext.MakeComponent("tf", "TextField", new()
            {
                ["label"] = "Email",
                ["error"] = "Required"
            })
        ]);

        var cut = _ctx.Render<A2UITextField>(p => p
            .Add(c => c.Data, surface.Components["tf"])
            .Add(c => c.Surface, surface));

        // Error visible initially
        Assert.NotNull(cut.Find(".a2ui-input-error"));

        // Type in the input
        cut.Find("input").Input("hello");

        // Error should be gone
        Assert.Throws<Bunit.ElementNotFoundException>(() => cut.Find(".a2ui-input-error"));
        Assert.Null(cut.Find("input").GetAttribute("aria-invalid"));
    }

    [Fact]
    public void Input_OptimisticallyUpdatesDataModel_WhenValueIsBound()
    {
        var surface = _ctx.SetupSurface("s", [
            SurfaceTestContext.MakeComponent("tf", "TextField", new()
            {
                ["value"] = "/name",
                ["action"] = new { @event = new { name = "update" } }
            })
        ], new { name = "Alice" });

        var cut = _ctx.Render<A2UITextField>(p => p
            .Add(c => c.Data, surface.Components["tf"])
            .Add(c => c.Surface, surface)
            .Add(c => c.OnAction, (A2UIUserAction _) => { }));

        cut.Find("input").Input("Bob");

        // Data model should be updated optimistically
        var resolved = _ctx.SurfaceManager.ResolveBinding("s", "/name");
        Assert.Equal("Bob", resolved?.GetString());
    }

    [Fact]
    public void Input_DoesNotOptimisticallyUpdate_WhenValueIsLiteral()
    {
        var surface = _ctx.SetupSurface("s", [
            SurfaceTestContext.MakeComponent("tf", "TextField", new()
            {
                ["value"] = "static text",
                ["action"] = new { @event = new { name = "update" } }
            })
        ], new { name = "Alice" });

        var cut = _ctx.Render<A2UITextField>(p => p
            .Add(c => c.Data, surface.Components["tf"])
            .Add(c => c.Surface, surface)
            .Add(c => c.OnAction, (A2UIUserAction _) => { }));

        cut.Find("input").Input("changed");

        // Data model should NOT be changed
        var resolved = _ctx.SurfaceManager.ResolveBinding("s", "/name");
        Assert.Equal("Alice", resolved?.GetString());
    }

    [Fact]
    public void TextField_ShowsValidationError_WhenBoundPathHasError()
    {
        var surface = _ctx.SetupSurface("s", [
            SurfaceTestContext.MakeComponent("tf", "TextField", new()
            {
                ["label"] = "Email",
                ["value"] = "/email"
            })
        ], new { email = "bad" });

        _ctx.SurfaceManager.SetValidationError("s", "/email", "Invalid email format");

        var cut = _ctx.Render<A2UITextField>(p => p
            .Add(c => c.Data, surface.Components["tf"])
            .Add(c => c.Surface, surface));

        var errorSpan = cut.Find(".a2ui-input-error");
        Assert.Equal("Invalid email format", errorSpan.TextContent.Trim());
    }

    [Fact]
    public void TextField_ComponentError_TakesPrecedenceOverValidationError()
    {
        var surface = _ctx.SetupSurface("s", [
            SurfaceTestContext.MakeComponent("tf", "TextField", new()
            {
                ["label"] = "Email",
                ["value"] = "/email",
                ["error"] = "Component error"
            })
        ], new { email = "bad" });

        _ctx.SurfaceManager.SetValidationError("s", "/email", "Validation error");

        var cut = _ctx.Render<A2UITextField>(p => p
            .Add(c => c.Data, surface.Components["tf"])
            .Add(c => c.Surface, surface));

        var errorSpan = cut.Find(".a2ui-input-error");
        Assert.Equal("Component error", errorSpan.TextContent.Trim());
    }

    public void Dispose() => _ctx.Dispose();
}
