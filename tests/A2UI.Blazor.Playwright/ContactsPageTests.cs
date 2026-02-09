using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace A2UI.Blazor.Playwright;

[TestFixture]
public class ContactsPageTests : PageTest
{
    private string BaseUrl => ServerFixture.SpaBaseUrl;

    [SetUp]
    public async Task NavigateToPage()
    {
        await Page.GotoAsync($"{BaseUrl}/contacts");
    }

    private ILocator Heading => Page.GetByRole(AriaRole.Heading, new() { Name = "Contact Directory" });
    private ILocator SearchInput => Page.Locator(".a2ui-textfield-input");
    private ILocator ListItems => Page.Locator(".a2ui-list-item");

    [Test]
    public async Task Page_LoadsWithFiveContacts()
    {
        await Expect(Heading).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await Expect(ListItems).ToHaveCountAsync(5, new() { Timeout = 10_000 });
    }

    [Test]
    public async Task Rows_DisplayContactDetails()
    {
        await Expect(Heading).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await Expect(ListItems).ToHaveCountAsync(5, new() { Timeout = 10_000 });

        await Expect(Page.Locator("text=Alice Johnson")).ToBeVisibleAsync();
        await Expect(Page.Locator("text=alice@example.com")).ToBeVisibleAsync();
        await Expect(Page.Locator("text=Bob Smith")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Search_ByDepartment_FiltersCorrectly()
    {
        await Expect(Heading).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await Expect(ListItems).ToHaveCountAsync(5, new() { Timeout = 10_000 });

        // Typing triggers oninput which sends the search action
        await SearchInput.FillAsync("Engineering");

        // Engineering has 3 contacts: Alice, Carol, Eve
        await Expect(ListItems).ToHaveCountAsync(3, new() { Timeout = 10_000 });
        await Expect(Page.Locator("text=Alice Johnson")).ToBeVisibleAsync();
        await Expect(Page.Locator("text=Carol Williams")).ToBeVisibleAsync();
        await Expect(Page.Locator("text=Eve Davis")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Search_ByName_FiltersCorrectly()
    {
        await Expect(Heading).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await Expect(ListItems).ToHaveCountAsync(5, new() { Timeout = 10_000 });

        await SearchInput.FillAsync("Alice");

        await Expect(ListItems).ToHaveCountAsync(1, new() { Timeout = 10_000 });
        await Expect(Page.Locator("text=Alice Johnson")).ToBeVisibleAsync();
        await Expect(Page.Locator("text=alice@example.com")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Search_ClearInput_RestoresAllContacts()
    {
        await Expect(Heading).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await Expect(ListItems).ToHaveCountAsync(5, new() { Timeout = 10_000 });

        // Filter
        await SearchInput.FillAsync("Alice");
        await Expect(ListItems).ToHaveCountAsync(1, new() { Timeout = 10_000 });

        // Clear restores all
        await SearchInput.ClearAsync();
        await Expect(ListItems).ToHaveCountAsync(5, new() { Timeout = 10_000 });
    }
}
