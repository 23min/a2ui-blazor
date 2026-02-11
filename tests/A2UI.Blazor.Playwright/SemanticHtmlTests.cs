using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace A2UI.Blazor.Playwright;

[TestFixture]
public class SemanticHtmlTests : PageTest
{
    private string BaseUrl => ServerFixture.SpaBaseUrl;

    [Test]
    public async Task ContactsList_RendersAsUlWithLiItems()
    {
        await Page.GotoAsync($"{BaseUrl}/contacts");

        // Wait for contacts to load
        var heading = Page.GetByRole(AriaRole.Heading, new() { Name = "Contact Directory" });
        await Expect(heading).ToBeVisibleAsync(new() { Timeout = 15_000 });

        // List should be a <ul> with role="list"
        var list = Page.Locator("ul.a2ui-list");
        await Expect(list).ToBeVisibleAsync(new() { Timeout = 10_000 });
        await Expect(list).ToHaveAttributeAsync("role", "list");

        // Items should be <li> elements
        var items = list.Locator("li.a2ui-list-item");
        await Expect(items).ToHaveCountAsync(5, new() { Timeout = 10_000 });
    }

    [Test]
    public async Task RestaurantCards_RenderAsArticleElements()
    {
        await Page.GotoAsync($"{BaseUrl}/restaurant");

        // Wait for restaurant cards to load
        var heading = Page.GetByRole(AriaRole.Heading, new() { Name = "Restaurant Finder" });
        await Expect(heading).ToBeVisibleAsync(new() { Timeout = 15_000 });

        // Cards should be <article> elements
        var cards = Page.Locator("article.a2ui-card");
        await Expect(cards.First).ToBeVisibleAsync(new() { Timeout = 10_000 });

        var count = await cards.CountAsync();
        Assert.That(count, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public async Task Surface_HasRegionLandmark()
    {
        await Page.GotoAsync($"{BaseUrl}/contacts");

        // Wait for page to load
        var heading = Page.GetByRole(AriaRole.Heading, new() { Name = "Contact Directory" });
        await Expect(heading).ToBeVisibleAsync(new() { Timeout = 15_000 });

        // Surface should have role="region" and aria-label
        var surface = Page.Locator(".a2ui-surface");
        await Expect(surface).ToHaveAttributeAsync("role", "region");
        await Expect(surface).ToHaveAttributeAsync("aria-label", "A2UI Surface");
    }

    [Test]
    public async Task ErrorDemo_UnknownComponent_HasStatusRole()
    {
        await Page.GotoAsync($"{BaseUrl}/error-demo");

        // Wait for the error demo page to load — unknown component renders with role="status"
        var unknownComponent = Page.Locator(".a2ui-component-unknown");
        await Expect(unknownComponent).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await Expect(unknownComponent).ToHaveAttributeAsync("role", "status");
        await Expect(unknownComponent).ToContainTextAsync("Unknown component: FancyWidget");
    }
}
