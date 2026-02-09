using A2UI.Blazor.Components.Visualization;
using A2UI.Blazor.Services;
using A2UI.Blazor.Tests.Helpers;
using Microsoft.AspNetCore.Components;

namespace A2UI.Blazor.Tests.Components.Visualization;

public class StateMachineTests : IDisposable
{
    private readonly SurfaceTestContext _ctx = new();

    /// <summary>
    /// Render A2UIStateMachine with CascadingValue for SurfaceManager (required for data binding).
    /// </summary>
    private IRenderedFragment RenderStateMachine(A2UI.Blazor.Protocol.A2UISurfaceState surface, string componentId)
    {
        return _ctx.Render(builder =>
        {
            builder.OpenComponent<CascadingValue<SurfaceManager>>(0);
            builder.AddAttribute(1, "Value", _ctx.SurfaceManager);
            builder.AddAttribute(2, "ChildContent",
                (RenderFragment)(b2 =>
                {
                    b2.OpenComponent<A2UIStateMachine>(0);
                    b2.AddAttribute(1, "Data", surface.Components[componentId]);
                    b2.AddAttribute(2, "Surface", surface);
                    b2.CloseComponent();
                }));
            builder.CloseComponent();
        });
    }

    [Fact]
    public void RendersCorrectNumberOfNodes()
    {
        var surface = _ctx.SetupSurface("s",
            [
                SurfaceTestContext.MakeComponent("sm", "StateMachine", new()
                {
                    ["data"] = "/pipeline"
                })
            ],
            new
            {
                pipeline = new
                {
                    title = "Test Pipeline",
                    states = new[]
                    {
                        new { id = "s1", label = "Step 1", status = "completed" },
                        new { id = "s2", label = "Step 2", status = "active" },
                        new { id = "s3", label = "Step 3", status = "pending" },
                    }
                }
            });

        var cut = RenderStateMachine(surface, "sm");

        // 3 node groups
        var nodes = cut.FindAll(".a2ui-sm-node");
        Assert.Equal(3, nodes.Count);

        // 3 circles for nodes + 1 pulse circle for active = 4 total circles
        var circles = cut.FindAll("circle");
        Assert.Equal(4, circles.Count);
    }

    [Fact]
    public void AppliesActiveClassToActiveState()
    {
        var surface = _ctx.SetupSurface("s",
            [
                SurfaceTestContext.MakeComponent("sm", "StateMachine", new()
                {
                    ["data"] = "/pipeline"
                })
            ],
            new
            {
                pipeline = new
                {
                    states = new[]
                    {
                        new { id = "s1", label = "Step 1", status = "pending" },
                        new { id = "s2", label = "Step 2", status = "active" },
                    }
                }
            });

        var cut = RenderStateMachine(surface, "sm");

        var activeNodes = cut.FindAll(".a2ui-sm-node-active");
        Assert.Single(activeNodes);

        // Active node has a pulse circle
        var pulseCircles = cut.FindAll(".a2ui-sm-pulse");
        Assert.Single(pulseCircles);
    }

    [Fact]
    public void AppliesCompletedClassWithCheckmark()
    {
        var surface = _ctx.SetupSurface("s",
            [
                SurfaceTestContext.MakeComponent("sm", "StateMachine", new()
                {
                    ["data"] = "/pipeline"
                })
            ],
            new
            {
                pipeline = new
                {
                    states = new[]
                    {
                        new { id = "s1", label = "Step 1", status = "completed" },
                        new { id = "s2", label = "Step 2", status = "pending" },
                    }
                }
            });

        var cut = RenderStateMachine(surface, "sm");

        var completedNodes = cut.FindAll(".a2ui-sm-node-completed");
        Assert.Single(completedNodes);

        // Checkmark is rendered
        var checkmarks = cut.FindAll(".a2ui-sm-check");
        Assert.Single(checkmarks);
    }

    [Fact]
    public void RendersTitleWhenProvided()
    {
        var surface = _ctx.SetupSurface("s",
            [
                SurfaceTestContext.MakeComponent("sm", "StateMachine", new()
                {
                    ["data"] = "/pipeline",
                    ["title"] = "/pipeline/title"
                })
            ],
            new
            {
                pipeline = new
                {
                    title = "My Pipeline",
                    states = new[]
                    {
                        new { id = "s1", label = "Step 1", status = "pending" },
                    }
                }
            });

        var cut = RenderStateMachine(surface, "sm");

        var titleEl = cut.Find(".a2ui-statemachine-title");
        Assert.Equal("My Pipeline", titleEl.TextContent);
    }

    [Fact]
    public void HandlesEmptyStatesGracefully()
    {
        var surface = _ctx.SetupSurface("s",
            [
                SurfaceTestContext.MakeComponent("sm", "StateMachine", new()
                {
                    ["data"] = "/pipeline"
                })
            ],
            new
            {
                pipeline = new
                {
                    states = Array.Empty<object>()
                }
            });

        var cut = RenderStateMachine(surface, "sm");

        // Should render the container div but no SVG nodes
        Assert.NotNull(cut.Find(".a2ui-statemachine"));
        var nodes = cut.FindAll(".a2ui-sm-node");
        Assert.Empty(nodes);
    }

    [Fact]
    public void RendersEdgesBetweenNodes()
    {
        var surface = _ctx.SetupSurface("s",
            [
                SurfaceTestContext.MakeComponent("sm", "StateMachine", new()
                {
                    ["data"] = "/pipeline"
                })
            ],
            new
            {
                pipeline = new
                {
                    states = new[]
                    {
                        new { id = "s1", label = "Step 1", status = "completed" },
                        new { id = "s2", label = "Step 2", status = "active" },
                        new { id = "s3", label = "Step 3", status = "pending" },
                    }
                }
            });

        var cut = RenderStateMachine(surface, "sm");

        // 2 edges for 3 nodes
        var completedEdges = cut.FindAll(".a2ui-sm-edge-completed");
        Assert.Single(completedEdges); // edge from completed s1

        var pendingEdges = cut.FindAll(".a2ui-sm-edge-pending");
        Assert.Single(pendingEdges); // edge from active s2
    }

    [Fact]
    public void NoTitleRendered_WhenNotProvided()
    {
        var surface = _ctx.SetupSurface("s",
            [
                SurfaceTestContext.MakeComponent("sm", "StateMachine", new()
                {
                    ["data"] = "/pipeline"
                })
            ],
            new
            {
                pipeline = new
                {
                    states = new[]
                    {
                        new { id = "s1", label = "Step 1", status = "pending" },
                    }
                }
            });

        var cut = RenderStateMachine(surface, "sm");

        var titles = cut.FindAll(".a2ui-statemachine-title");
        Assert.Empty(titles);
    }

    public void Dispose() => _ctx.Dispose();
}
