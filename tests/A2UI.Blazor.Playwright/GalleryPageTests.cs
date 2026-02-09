using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace A2UI.Blazor.Playwright;

[TestFixture]
public class GalleryPageTests : PageTest
{
    private string BaseUrl => ServerFixture.SpaBaseUrl;

    private ILocator Title => Page.GetByRole(AriaRole.Heading, new() { Name = "A2UI Component Gallery" });

    [SetUp]
    public async Task NavigateToPage()
    {
        await Page.GotoAsync($"{BaseUrl}/gallery");
    }

    [Test]
    public async Task Page_LoadsWithGalleryTitle()
    {
        await Expect(Title).ToBeVisibleAsync(new() { Timeout = 15_000 });
    }

    [Test]
    public async Task DisplayComponents_RenderCorrectly()
    {
        await Expect(Title).ToBeVisibleAsync(new() { Timeout = 15_000 });

        await Expect(Page.Locator("text=Display Components")).ToBeVisibleAsync();
        await Expect(Page.Locator("text=This text is coming from a Python FastAPI server")).ToBeVisibleAsync();

        // Icon
        await Expect(Page.Locator(".a2ui-icon")).ToBeVisibleAsync();

        // Image
        var image = Page.Locator(".a2ui-image");
        await Expect(image).ToBeVisibleAsync();
        await Expect(image).ToHaveAttributeAsync("alt", "Sample landscape");
    }

    [Test]
    public async Task LayoutComponents_RenderCorrectly()
    {
        await Expect(Title).ToBeVisibleAsync(new() { Timeout = 15_000 });

        await Expect(Page.Locator("text=Layout Components")).ToBeVisibleAsync();

        // Tabs - verify tab buttons and content
        var tabButtons = Page.Locator(".a2ui-tab-button");
        await Expect(tabButtons).ToHaveCountAsync(2);
        await Expect(Page.Locator("text=Tab One")).ToBeVisibleAsync();
        await Expect(Page.Locator("text=Tab Two")).ToBeVisibleAsync();

        // First tab content is visible by default
        await Expect(Page.Locator("text=Content of the first tab")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Tabs_SwitchContent()
    {
        await Expect(Title).ToBeVisibleAsync(new() { Timeout = 15_000 });

        // Click second tab
        await Page.Locator(".a2ui-tab-button", new() { HasText = "Tab Two" }).ClickAsync();
        await Expect(Page.Locator("text=Content of the second tab")).ToBeVisibleAsync();

        // Click first tab back
        await Page.Locator(".a2ui-tab-button", new() { HasText = "Tab One" }).ClickAsync();
        await Expect(Page.Locator("text=Content of the first tab")).ToBeVisibleAsync();
    }

    [Test]
    public async Task InputComponents_RenderCorrectly()
    {
        await Expect(Title).ToBeVisibleAsync(new() { Timeout = 15_000 });

        await Expect(Page.Locator("text=Input Components")).ToBeVisibleAsync();

        // Button
        await Expect(Page.Locator("button", new() { HasText = "Click Me" })).ToBeVisibleAsync();

        // TextField
        await Expect(Page.Locator("input[placeholder='Type here...']")).ToBeVisibleAsync();

        // CheckBox
        await Expect(Page.Locator("input[type='checkbox']")).ToBeVisibleAsync();

        // Slider
        var slider = Page.Locator("input[type='range']");
        await Expect(slider).ToBeVisibleAsync();
        Assert.That(await slider.InputValueAsync(), Is.EqualTo("50"));

        // ChoicePicker
        var select = Page.Locator(".a2ui-choicepicker-select");
        await Expect(select).ToBeVisibleAsync();
        await Expect(Page.Locator("text=Favorite color")).ToBeVisibleAsync();

        // DateTimeInput
        var dateInput = Page.Locator(".a2ui-datetime-input");
        await Expect(dateInput).ToBeVisibleAsync();
        await Expect(Page.Locator("text=Pick a date")).ToBeVisibleAsync();
    }

    [Test]
    public async Task MediaComponents_RenderCorrectly()
    {
        await Expect(Title).ToBeVisibleAsync(new() { Timeout = 15_000 });

        await Expect(Page.Locator("text=Media Components")).ToBeVisibleAsync();

        // Video
        var video = Page.Locator("video");
        await Expect(video).ToBeVisibleAsync();
        await Expect(video).ToHaveAttributeAsync("controls", "");

        // Audio
        var audio = Page.Locator("audio");
        await Expect(audio).ToHaveCountAsync(1);
    }
}
