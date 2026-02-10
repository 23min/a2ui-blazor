using A2UI.Blazor.Components.Layout;
using A2UI.Blazor.Tests.Helpers;

namespace A2UI.Blazor.Tests.Components.Layout;

public class RowTests : IDisposable
{
    private readonly SurfaceTestContext _ctx = new();

    [Fact]
    public void Renders_ChildrenInFlexRow()
    {
        var surface = _ctx.SetupSurface("s", [
            SurfaceTestContext.MakeComponent("row", "Row", new()
            {
                ["children"] = new[] { "t1", "t2" }
            }),
            SurfaceTestContext.MakeComponent("t1", "Text", new() { ["text"] = "A" }),
            SurfaceTestContext.MakeComponent("t2", "Text", new() { ["text"] = "B" })
        ]);

        var cut = _ctx.Render<A2UIRow>(p => p
            .Add(c => c.Data, surface.Components["row"])
            .Add(c => c.Surface, surface));

        var div = cut.Find(".a2ui-row");
        Assert.NotNull(div);
        Assert.Contains("A", cut.Markup);
        Assert.Contains("B", cut.Markup);
    }

    [Fact]
    public void Applies_DistributionAlignmentGap_Styles()
    {
        var surface = _ctx.SetupSurface("s", [
            SurfaceTestContext.MakeComponent("row", "Row", new()
            {
                ["distribution"] = "spaceBetween",
                ["alignment"] = "end",
                ["gap"] = "16",
                ["children"] = new[] { "t1" }
            }),
            SurfaceTestContext.MakeComponent("t1", "Text", new() { ["text"] = "X" })
        ]);

        var cut = _ctx.Render<A2UIRow>(p => p
            .Add(c => c.Data, surface.Components["row"])
            .Add(c => c.Surface, surface));

        var style = cut.Find(".a2ui-row").GetAttribute("style") ?? "";
        Assert.Contains("space-between", style);
        Assert.Contains("flex-end", style);
        Assert.Contains("16px", style);
    }

    [Fact]
    public void Skips_NonExistentChildIds()
    {
        var surface = _ctx.SetupSurface("s", [
            SurfaceTestContext.MakeComponent("row", "Row", new()
            {
                ["children"] = new[] { "exists", "missing" }
            }),
            SurfaceTestContext.MakeComponent("exists", "Text", new() { ["text"] = "Here" })
        ]);

        var cut = _ctx.Render<A2UIRow>(p => p
            .Add(c => c.Data, surface.Components["row"])
            .Add(c => c.Surface, surface));

        Assert.Contains("Here", cut.Markup);
        // Should not throw, just skip the missing child
    }

    public void Dispose() => _ctx.Dispose();
}
