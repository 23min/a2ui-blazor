using A2UI.Blazor.Components.Display;
using A2UI.Blazor.Tests.Helpers;

namespace A2UI.Blazor.Tests.Components.Display;

public class TextTests : IDisposable
{
    private readonly SurfaceTestContext _ctx = new();

    [Theory]
    [InlineData("h1", "h1")]
    [InlineData("h2", "h2")]
    [InlineData("h3", "h3")]
    [InlineData("h4", "h4")]
    [InlineData("h5", "h5")]
    public void Renders_HeadingElement_ForUsageHint(string usageHint, string expectedTag)
    {
        var surface = _ctx.SetupSurface("s", [
            SurfaceTestContext.MakeComponent("root", "Text", new()
            {
                ["text"] = "Hello",
                ["usageHint"] = usageHint
            })
        ]);

        var cut = _ctx.Render<A2UIText>(p => p
            .Add(c => c.Data, surface.Components["root"])
            .Add(c => c.Surface, surface));

        cut.Find(expectedTag).MarkupMatches($"<{expectedTag} class=\"a2ui-text a2ui-text-{usageHint}\" >Hello</{expectedTag}>");
    }

    [Fact]
    public void Renders_Paragraph_ForBodyUsageHint()
    {
        var surface = _ctx.SetupSurface("s", [
            SurfaceTestContext.MakeComponent("root", "Text", new()
            {
                ["text"] = "Body text",
                ["usageHint"] = "body"
            })
        ]);

        var cut = _ctx.Render<A2UIText>(p => p
            .Add(c => c.Data, surface.Components["root"])
            .Add(c => c.Surface, surface));

        var el = cut.Find("p");
        Assert.Contains("Body text", el.TextContent);
    }

    [Fact]
    public void Renders_Paragraph_WhenNoUsageHint()
    {
        var surface = _ctx.SetupSurface("s", [
            SurfaceTestContext.MakeComponent("root", "Text", new()
            {
                ["text"] = "Default text"
            })
        ]);

        var cut = _ctx.Render<A2UIText>(p => p
            .Add(c => c.Data, surface.Components["root"])
            .Add(c => c.Surface, surface));

        var el = cut.Find("p");
        Assert.Contains("Default text", el.TextContent);
    }

    [Fact]
    public void Renders_Span_ForCaptionUsageHint()
    {
        var surface = _ctx.SetupSurface("s", [
            SurfaceTestContext.MakeComponent("root", "Text", new()
            {
                ["text"] = "Caption",
                ["usageHint"] = "caption"
            })
        ]);

        var cut = _ctx.Render<A2UIText>(p => p
            .Add(c => c.Data, surface.Components["root"])
            .Add(c => c.Surface, surface));

        var el = cut.Find("span");
        Assert.Contains("Caption", el.TextContent);
    }

    [Fact]
    public void Resolves_DataBinding_ForText()
    {
        var surface = _ctx.SetupSurface("s",
            [SurfaceTestContext.MakeComponent("root", "Text", new()
            {
                ["text"] = "/greeting"
            })],
            new { greeting = "Hello World" });

        var cut = _ctx.Render(builder =>
        {
            builder.OpenComponent<Microsoft.AspNetCore.Components.CascadingValue<SurfaceManager>>(0);
            builder.AddAttribute(1, "Value", _ctx.SurfaceManager);
            builder.AddAttribute(2, "ChildContent",
                (Microsoft.AspNetCore.Components.RenderFragment)(b2 =>
                {
                    b2.OpenComponent<A2UIText>(0);
                    b2.AddAttribute(1, "Data", surface.Components["root"]);
                    b2.AddAttribute(2, "Surface", surface);
                    b2.CloseComponent();
                }));
            builder.CloseComponent();
        });

        Assert.Contains("Hello World", cut.Markup);
    }

    public void Dispose() => _ctx.Dispose();
}
