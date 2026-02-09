using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace A2UI.Blazor.Playwright;

[TestFixture]
public class RestaurantPageTests : PageTest
{
    private string BaseUrl => ServerFixture.SpaBaseUrl;

    [SetUp]
    public async Task NavigateToPage()
    {
        await Page.GotoAsync($"{BaseUrl}/restaurant");
    }

    private ILocator Heading => Page.GetByRole(AriaRole.Heading, new() { Name = "Restaurant Finder" });
    private ILocator SearchInput => Page.Locator(".a2ui-textfield-input");
    private ILocator Cards => Page.Locator(".a2ui-card");

    [Test]
    public async Task Page_LoadsAndDisplaysHeading()
    {
        await Expect(Heading).ToBeVisibleAsync(new() { Timeout = 15_000 });
    }

    [Test]
    public async Task InitialRender_ShowsFourRestaurants()
    {
        await Expect(Heading).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await Expect(Cards).ToHaveCountAsync(4, new() { Timeout = 10_000 });
    }

    [Test]
    public async Task Cards_DisplayRestaurantDetails()
    {
        await Expect(Heading).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await Expect(Cards).ToHaveCountAsync(4, new() { Timeout = 10_000 });

        await Expect(Page.Locator("text=The Golden Fork")).ToBeVisibleAsync();
        await Expect(Page.Locator("text=Italian")).ToBeVisibleAsync();
        await Expect(Page.Locator("text=Sushi Zen")).ToBeVisibleAsync();
        await Expect(Page.Locator("text=Japanese")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Search_ByName_FiltersResults()
    {
        await Expect(Heading).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await Expect(Cards).ToHaveCountAsync(4, new() { Timeout = 10_000 });

        // Typing triggers oninput which sends the search action to the server
        await SearchInput.FillAsync("Sushi");

        await Expect(Cards).ToHaveCountAsync(1, new() { Timeout = 10_000 });
        await Expect(Page.Locator("text=Sushi Zen")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Search_ByCuisine_FiltersResults()
    {
        await Expect(Heading).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await Expect(Cards).ToHaveCountAsync(4, new() { Timeout = 10_000 });

        await SearchInput.FillAsync("Italian");

        await Expect(Cards).ToHaveCountAsync(1, new() { Timeout = 10_000 });
        await Expect(Page.Locator("text=The Golden Fork")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Search_NonExistentTerm_ShowsNoResults()
    {
        await Expect(Heading).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await Expect(Cards).ToHaveCountAsync(4, new() { Timeout = 10_000 });

        await SearchInput.FillAsync("ZZZZZ_nonexistent");

        await Expect(Cards).ToHaveCountAsync(0, new() { Timeout = 10_000 });
    }

    [Test]
    public async Task Search_ClearInput_RestoresAllResults()
    {
        await Expect(Heading).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await Expect(Cards).ToHaveCountAsync(4, new() { Timeout = 10_000 });

        // Filter down
        await SearchInput.FillAsync("Sushi");
        await Expect(Cards).ToHaveCountAsync(1, new() { Timeout = 10_000 });

        // Clear restores all (oninput fires with empty value)
        await SearchInput.ClearAsync();
        await Expect(Cards).ToHaveCountAsync(4, new() { Timeout = 10_000 });
    }

    [Test]
    public async Task ConnectingMessage_DisappearsAfterLoad()
    {
        await Expect(Heading).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await Expect(Page.Locator("text=Connecting...")).Not.ToBeVisibleAsync();
    }
}
