using A2UI.Blazor.Components;
using A2UI.Blazor.Protocol;
using A2UI.Blazor.Services;
using A2UI.Blazor.Tests.Helpers;
using Bunit;

namespace A2UI.Blazor.Tests.Components;

/// <summary>
/// Tests for ErrorBoundary behavior in A2UI components.
/// </summary>
public class ErrorBoundaryTests : SurfaceTestContext
{
    [Fact]
    public void ComponentRenderer_UnknownComponent_ShowsUnknownUI()
    {
        // Setup surface with unknown component type
        var components = new List<A2UIComponentData>
        {
            new() { Id = "root", Component = "NonExistentComponent" }
        };

        var surface = SetupSurface("test", components);

        // Render the component
        var cut = Render<A2UIComponentRenderer>(parameters => parameters
            .Add(p => p.Data, surface.GetRoot()!)
            .Add(p => p.Surface, surface));

        // Should show .a2ui-component-unknown div
        var unknownDiv = cut.Find(".a2ui-component-unknown");
        Assert.NotNull(unknownDiv);
        Assert.Contains("Unknown component: NonExistentComponent", unknownDiv.TextContent);
    }

    [Fact]
    public void ComponentRenderer_KnownComponent_RendersSuccessfully()
    {
        // Setup surface with known component
        var components = new List<A2UIComponentData>
        {
            SurfaceTestContext.MakeComponent("root", "Text", new Dictionary<string, object>
            {
                ["text"] = "Hello World",
                ["variant"] = "body"
            })
        };

        var surface = SetupSurface("test", components);

        // Render the component
        var cut = Render<A2UIComponentRenderer>(parameters => parameters
            .Add(p => p.Data, surface.GetRoot()!)
            .Add(p => p.Surface, surface));

        // Should render the text component successfully
        var textDiv = cut.Find(".a2ui-text");
        Assert.NotNull(textDiv);
        Assert.Contains("Hello World", textDiv.TextContent);

        // Should NOT show error UI
        Assert.Throws<Bunit.ElementNotFoundException>(() => cut.Find(".a2ui-component-unknown"));
        Assert.Throws<Bunit.ElementNotFoundException>(() => cut.Find(".a2ui-component-error"));
    }

    [Fact]
    public void Surface_NoSurface_ShowsLoadingState()
    {
        // Create a surface component without creating the surface first
        var cut = Render<A2UISurface>(parameters => parameters
            .Add(p => p.SurfaceId, "nonexistent"));

        // Should show loading state
        var loadingDiv = cut.Find(".a2ui-surface-loading");
        Assert.NotNull(loadingDiv);
        Assert.Contains("Connecting...", loadingDiv.TextContent);
    }

    [Fact]
    public void Surface_WithValidSurface_RendersContent()
    {
        // Setup surface
        var components = new List<A2UIComponentData>
        {
            SurfaceTestContext.MakeComponent("root", "Text", new Dictionary<string, object>
            {
                ["text"] = "Test Surface",
                ["variant"] = "body"
            })
        };

        SetupSurface("test", components);

        // Render the surface
        var cut = Render<A2UISurface>(parameters => parameters
            .Add(p => p.SurfaceId, "test"));

        // Should render the content
        var textDiv = cut.Find(".a2ui-text");
        Assert.NotNull(textDiv);
        Assert.Contains("Test Surface", textDiv.TextContent);

        // Should NOT show loading or error states
        Assert.Throws<Bunit.ElementNotFoundException>(() => cut.Find(".a2ui-surface-loading"));
        Assert.Throws<Bunit.ElementNotFoundException>(() => cut.Find(".a2ui-surface-error"));
    }

    [Fact]
    public void Surface_Reconnecting_ShowsOverlay()
    {
        // Setup surface
        var components = new List<A2UIComponentData>
        {
            SurfaceTestContext.MakeComponent("root", "Text", new Dictionary<string, object>
            {
                ["text"] = "Content",
                ["variant"] = "body"
            })
        };

        SetupSurface("test", components);

        // Render with Reconnecting state
        var cut = Render<A2UISurface>(parameters => parameters
            .Add(p => p.SurfaceId, "test")
            .Add(p => p.ConnectionState, StreamConnectionState.Reconnecting));

        // Should show reconnecting overlay
        var overlay = cut.Find(".a2ui-surface-reconnecting");
        Assert.NotNull(overlay);
        Assert.Contains("Reconnecting...", overlay.TextContent);

        // Content should still be visible underneath
        var textDiv = cut.Find(".a2ui-text");
        Assert.NotNull(textDiv);
    }

    [Fact]
    public void Surface_Connected_NoOverlay()
    {
        // Setup surface
        var components = new List<A2UIComponentData>
        {
            SurfaceTestContext.MakeComponent("root", "Text", new Dictionary<string, object>
            {
                ["text"] = "Content",
                ["variant"] = "body"
            })
        };

        SetupSurface("test", components);

        // Render with Connected state
        var cut = Render<A2UISurface>(parameters => parameters
            .Add(p => p.SurfaceId, "test")
            .Add(p => p.ConnectionState, StreamConnectionState.Connected));

        // Should NOT show reconnecting overlay
        Assert.Throws<Bunit.ElementNotFoundException>(() => cut.Find(".a2ui-surface-reconnecting"));

        // Content should be visible
        var textDiv = cut.Find(".a2ui-text");
        Assert.NotNull(textDiv);
    }

    [Fact]
    public void ComponentRenderer_UnknownComponent_DoesNotCrash()
    {
        // Setup surface with unknown component
        var components = new List<A2UIComponentData>
        {
            new() { Id = "root", Component = "TotallyUnknownType" }
        };

        var surface = SetupSurface("test", components);

        // Should not throw when rendering unknown component
        var ex = Record.Exception(() =>
        {
            var cut = Render<A2UISurface>(parameters => parameters
                .Add(p => p.SurfaceId, "test"));
        });

        Assert.Null(ex);
    }

    [Fact]
    public void ComponentRegistry_ReturnsNull_ForUnknownTypes()
    {
        // Verify that ComponentRegistry returns null for unknown types
        // This is what triggers the unknown component UI in the renderer
        var unknownType = Registry.Resolve("CompletelyMadeUpComponentName");

        Assert.Null(unknownType);
    }

    [Fact]
    public void Surface_EmptyComponents_RendersEmptyColumn()
    {
        // Setup surface with just a root Column, no children
        var components = new List<A2UIComponentData>
        {
            new() { Id = "root", Component = "Column" }
        };

        var surface = SetupSurface("test", components);

        // Render the surface
        var cut = Render<A2UISurface>(parameters => parameters
            .Add(p => p.SurfaceId, "test"));

        // Should render the column (even if empty)
        var column = cut.Find(".a2ui-column");
        Assert.NotNull(column);

        // Should not show any error states
        Assert.Throws<Bunit.ElementNotFoundException>(() => cut.Find(".a2ui-component-error"));
        Assert.Throws<Bunit.ElementNotFoundException>(() => cut.Find(".a2ui-surface-error"));
    }
}

