using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace A2UI.Blazor.Playwright;

[TestFixture]
public class StateMachinePageTests : PageTest
{
    private string BaseUrl => ServerFixture.SpaBaseUrl;

    private ILocator Heading => Page.GetByRole(AriaRole.Heading, new() { Name = "Live State Machine" });

    [SetUp]
    public async Task NavigateToPage()
    {
        await Page.GotoAsync($"{BaseUrl}/state-machine");
    }

    [Test]
    public async Task Page_LoadsWithHeading()
    {
        await Expect(Heading).ToBeVisibleAsync(new() { Timeout = 15_000 });
    }

    [Test]
    public async Task SVG_RendersWithSixNodes()
    {
        await Expect(Heading).ToBeVisibleAsync(new() { Timeout = 15_000 });

        // Wait for the state machine SVG to appear
        var svg = Page.Locator(".a2ui-statemachine-svg");
        await Expect(svg).ToBeVisibleAsync(new() { Timeout = 10_000 });

        // 6 pipeline states
        var nodes = Page.Locator(".a2ui-sm-node");
        await Expect(nodes).ToHaveCountAsync(6, new() { Timeout = 10_000 });
    }

    [Test]
    public async Task Title_ShowsPipelineName()
    {
        await Expect(Heading).ToBeVisibleAsync(new() { Timeout = 15_000 });

        var title = Page.Locator(".a2ui-statemachine-title");
        await Expect(title).ToContainTextAsync("Order Processing Pipeline", new() { Timeout = 10_000 });
    }

    [Test]
    public async Task ActiveNode_AdvancesOverTime()
    {
        await Expect(Heading).ToBeVisibleAsync(new() { Timeout = 15_000 });

        // Wait for SVG to render
        var svg = Page.Locator(".a2ui-statemachine-svg");
        await Expect(svg).ToBeVisibleAsync(new() { Timeout = 10_000 });

        // Wait for the first active node to appear (server starts advancing after ~2s)
        var activeNode = Page.Locator(".a2ui-sm-node-active");
        await Expect(activeNode).ToHaveCountAsync(1, new() { Timeout = 10_000 });

        // Wait for at least one completed node to appear (meaning the pipeline has advanced)
        var completedNode = Page.Locator(".a2ui-sm-node-completed").First;
        await Expect(completedNode).ToBeVisibleAsync(new() { Timeout = 10_000 });
    }
}
