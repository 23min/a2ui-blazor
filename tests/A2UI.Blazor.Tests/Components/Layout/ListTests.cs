using A2UI.Blazor.Components.Layout;
using A2UI.Blazor.Services;
using A2UI.Blazor.Tests.Helpers;
using Microsoft.AspNetCore.Components;

namespace A2UI.Blazor.Tests.Components.Layout;

public class ListTests : IDisposable
{
    private readonly SurfaceTestContext _ctx = new();

    /// <summary>
    /// Render A2UIList wrapped in CascadingValue for SurfaceManager (needed for data binding in template mode).
    /// </summary>
    private IRenderedFragment RenderList(A2UI.Blazor.Protocol.A2UISurfaceState surface, string componentId)
    {
        return _ctx.Render(builder =>
        {
            builder.OpenComponent<CascadingValue<SurfaceManager>>(0);
            builder.AddAttribute(1, "Value", _ctx.SurfaceManager);
            builder.AddAttribute(2, "ChildContent",
                (RenderFragment)(b2 =>
                {
                    b2.OpenComponent<A2UIList>(0);
                    b2.AddAttribute(1, "Data", surface.Components[componentId]);
                    b2.AddAttribute(2, "Surface", surface);
                    b2.CloseComponent();
                }));
            builder.CloseComponent();
        });
    }

    [Fact]
    public void TemplateMode_RendersItemsFromDataModel()
    {
        var surface = _ctx.SetupSurface("s",
            [
                SurfaceTestContext.MakeComponent("list", "List", new()
                {
                    ["data"] = "/items",
                    ["template"] = new { componentId = "item-tmpl" }
                }),
                SurfaceTestContext.MakeComponent("item-tmpl", "Text", new()
                {
                    ["text"] = "name"
                })
            ],
            new { items = new[] { new { name = "Alice" }, new { name = "Bob" } } });

        var cut = RenderList(surface, "list");

        var listItems = cut.FindAll(".a2ui-list-item");
        Assert.Equal(2, listItems.Count);
        Assert.Contains("Alice", cut.Markup);
        Assert.Contains("Bob", cut.Markup);
    }

    [Fact]
    public void TemplateMode_EmptyArray_RendersNoItems()
    {
        var surface = _ctx.SetupSurface("s",
            [
                SurfaceTestContext.MakeComponent("list", "List", new()
                {
                    ["data"] = "/items",
                    ["template"] = new { componentId = "item-tmpl" }
                }),
                SurfaceTestContext.MakeComponent("item-tmpl", "Text", new()
                {
                    ["text"] = "name"
                })
            ],
            new { items = Array.Empty<object>() });

        var cut = RenderList(surface, "list");

        var listItems = cut.FindAll(".a2ui-list-item");
        Assert.Empty(listItems);
    }

    [Fact]
    public void ChildrenMode_RendersDirectChildren()
    {
        var surface = _ctx.SetupSurface("s", [
            SurfaceTestContext.MakeComponent("list", "List", new()
            {
                ["children"] = new[] { "c1", "c2" }
            }),
            SurfaceTestContext.MakeComponent("c1", "Text", new() { ["text"] = "One" }),
            SurfaceTestContext.MakeComponent("c2", "Text", new() { ["text"] = "Two" })
        ]);

        var cut = _ctx.RenderComponent<A2UIList>(p => p
            .Add(c => c.Data, surface.Components["list"])
            .Add(c => c.Surface, surface));

        var items = cut.FindAll(".a2ui-list-item");
        Assert.Equal(2, items.Count);
        Assert.Contains("One", cut.Markup);
        Assert.Contains("Two", cut.Markup);
    }

    [Fact]
    public void TemplateMode_MissingTemplateComponent_RendersNothing()
    {
        var surface = _ctx.SetupSurface("s",
            [
                SurfaceTestContext.MakeComponent("list", "List", new()
                {
                    ["data"] = "/items",
                    ["template"] = new { componentId = "nonexistent" }
                })
            ],
            new { items = new[] { new { name = "Alice" } } });

        var cut = RenderList(surface, "list");

        var listItems = cut.FindAll(".a2ui-list-item");
        Assert.Empty(listItems);
    }

    [Fact]
    public void TemplateMode_NoData_FallsBackToChildren()
    {
        var surface = _ctx.SetupSurface("s", [
            SurfaceTestContext.MakeComponent("list", "List", new()
            {
                ["children"] = new[] { "c1" }
            }),
            SurfaceTestContext.MakeComponent("c1", "Text", new() { ["text"] = "Fallback" })
        ]);

        var cut = _ctx.RenderComponent<A2UIList>(p => p
            .Add(c => c.Data, surface.Components["list"])
            .Add(c => c.Surface, surface));

        Assert.Contains("Fallback", cut.Markup);
    }

    public void Dispose() => _ctx.Dispose();
}
