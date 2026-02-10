using A2UI.Blazor.Components.Layout;
using A2UI.Blazor.Tests.Helpers;

namespace A2UI.Blazor.Tests.Components.Layout;

public class CardTests : IDisposable
{
    private readonly SurfaceTestContext _ctx = new();

    [Fact]
    public void Renders_TitleInHeader()
    {
        var surface = _ctx.SetupSurface("s", [
            SurfaceTestContext.MakeComponent("card", "Card", new()
            {
                ["title"] = "My Card",
                ["children"] = new[] { "body" }
            }),
            SurfaceTestContext.MakeComponent("body", "Text", new() { ["text"] = "Content" })
        ]);

        var cut = _ctx.Render<A2UICard>(p => p
            .Add(c => c.Data, surface.Components["card"])
            .Add(c => c.Surface, surface));

        var title = cut.Find(".a2ui-card-title");
        Assert.Equal("My Card", title.TextContent.Trim());
    }

    [Fact]
    public void Renders_ChildrenInBody()
    {
        var surface = _ctx.SetupSurface("s", [
            SurfaceTestContext.MakeComponent("card", "Card", new()
            {
                ["title"] = "T",
                ["children"] = new[] { "c1" }
            }),
            SurfaceTestContext.MakeComponent("c1", "Text", new() { ["text"] = "Inside card" })
        ]);

        var cut = _ctx.Render<A2UICard>(p => p
            .Add(c => c.Data, surface.Components["card"])
            .Add(c => c.Surface, surface));

        var body = cut.Find(".a2ui-card-body");
        Assert.Contains("Inside card", body.TextContent);
    }

    [Fact]
    public void NoHeader_WhenTitleIsNull()
    {
        var surface = _ctx.SetupSurface("s", [
            SurfaceTestContext.MakeComponent("card", "Card", new()
            {
                ["children"] = new[] { "c1" }
            }),
            SurfaceTestContext.MakeComponent("c1", "Text", new() { ["text"] = "Content" })
        ]);

        var cut = _ctx.Render<A2UICard>(p => p
            .Add(c => c.Data, surface.Components["card"])
            .Add(c => c.Surface, surface));

        Assert.Throws<Bunit.ElementNotFoundException>(() => cut.Find(".a2ui-card-header"));
    }

    public void Dispose() => _ctx.Dispose();
}
