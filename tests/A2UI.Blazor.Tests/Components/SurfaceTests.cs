using System.Text.Json;
using A2UI.Blazor.Components;
using A2UI.Blazor.Protocol;
using A2UI.Blazor.Tests.Helpers;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace A2UI.Blazor.Tests.Components;

public class SurfaceTests : IDisposable
{
    private readonly SurfaceTestContext _ctx = new();

    private IRenderedComponent<IComponent> RenderSurface(string surfaceId, Action<A2UIUserAction>? onAction = null)
    {
        return _ctx.Render(builder =>
        {
            builder.OpenComponent<A2UISurface>(0);
            builder.AddAttribute(1, "SurfaceId", surfaceId);
            if (onAction is not null)
                builder.AddAttribute(2, "OnAction", EventCallback.Factory.Create(this, onAction));
            builder.CloseComponent();
        });
    }

    [Fact]
    public void Shows_ConnectingMessage_WhenSurfaceNotCreated()
    {
        var cut = RenderSurface("missing-surface");
        Assert.Contains("Connecting...", cut.Markup);
    }

    [Fact]
    public void Renders_RootComponent_WhenSurfaceHasComponents()
    {
        _ctx.SetupSurface("s1", [
            SurfaceTestContext.MakeComponent("root", "Text", new()
            {
                ["text"] = "Hello from surface",
                ["usageHint"] = "h2"
            })
        ]);

        var cut = RenderSurface("s1");

        Assert.Contains("Hello from surface", cut.Markup);
        Assert.DoesNotContain("Connecting...", cut.Markup);
    }

    [Fact]
    public void ReRenders_WhenDataModelUpdates()
    {
        _ctx.SetupSurface("s1",
            [SurfaceTestContext.MakeComponent("root", "Text", new()
            {
                ["text"] = "StaticText",
                ["usageHint"] = "body"
            })],
            new { message = "Initial" });

        var cut = RenderSurface("s1");

        Assert.Contains("StaticText", cut.Markup);

        // Update components to change rendered text
        _ctx.SurfaceManager.UpdateComponents("s1", [
            SurfaceTestContext.MakeComponent("root", "Text", new()
            {
                ["text"] = "UpdatedText",
                ["usageHint"] = "body"
            })
        ]);

        cut.WaitForAssertion(() => Assert.Contains("UpdatedText", cut.Markup));
    }

    [Fact]
    public void Propagates_OnAction_FromChildComponents()
    {
        _ctx.SetupSurface("s1", [
            SurfaceTestContext.MakeComponent("root", "Button", new()
            {
                ["label"] = "Go",
                ["action"] = new { @event = new { name = "click" } }
            })
        ]);

        A2UIUserAction? capturedAction = null;

        var cut = RenderSurface("s1", action => capturedAction = action);

        cut.Find("button").Click();

        Assert.NotNull(capturedAction);
        Assert.Equal("click", capturedAction.Name);
    }

    public void Dispose() => _ctx.Dispose();
}
