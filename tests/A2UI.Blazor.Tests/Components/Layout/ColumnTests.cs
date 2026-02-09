using A2UI.Blazor.Components.Layout;
using A2UI.Blazor.Tests.Helpers;

namespace A2UI.Blazor.Tests.Components.Layout;

public class ColumnTests : IDisposable
{
    private readonly SurfaceTestContext _ctx = new();

    [Fact]
    public void Renders_ChildrenInFlexColumn()
    {
        var surface = _ctx.SetupSurface("s", [
            SurfaceTestContext.MakeComponent("col", "Column", new()
            {
                ["children"] = new[] { "t1", "t2" }
            }),
            SurfaceTestContext.MakeComponent("t1", "Text", new() { ["text"] = "First" }),
            SurfaceTestContext.MakeComponent("t2", "Text", new() { ["text"] = "Second" })
        ]);

        var cut = _ctx.RenderComponent<A2UIColumn>(p => p
            .Add(c => c.Data, surface.Components["col"])
            .Add(c => c.Surface, surface));

        var div = cut.Find(".a2ui-column");
        Assert.NotNull(div);
        Assert.Contains("First", cut.Markup);
        Assert.Contains("Second", cut.Markup);
    }

    [Fact]
    public void Applies_AlignmentAndGap_Styles()
    {
        var surface = _ctx.SetupSurface("s", [
            SurfaceTestContext.MakeComponent("col", "Column", new()
            {
                ["alignment"] = "center",
                ["gap"] = "24",
                ["children"] = new[] { "t1" }
            }),
            SurfaceTestContext.MakeComponent("t1", "Text", new() { ["text"] = "X" })
        ]);

        var cut = _ctx.RenderComponent<A2UIColumn>(p => p
            .Add(c => c.Data, surface.Components["col"])
            .Add(c => c.Surface, surface));

        var style = cut.Find(".a2ui-column").GetAttribute("style") ?? "";
        Assert.Contains("center", style);
        Assert.Contains("24px", style);
    }

    public void Dispose() => _ctx.Dispose();
}
