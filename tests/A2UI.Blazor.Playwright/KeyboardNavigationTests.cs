using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace A2UI.Blazor.Playwright;

[TestFixture]
public class KeyboardNavigationTests : PageTest
{
    private string BaseUrl => ServerFixture.SpaBaseUrl;

    [Test]
    public async Task GalleryButton_ShowsFocusRingOnTab()
    {
        await Page.GotoAsync($"{BaseUrl}/gallery");

        var heading = Page.GetByRole(AriaRole.Heading, new() { Name = "A2UI Component Gallery" });
        await Expect(heading).ToBeVisibleAsync(new() { Timeout = 15_000 });

        // Focus the first button by clicking the page body then tabbing
        var button = Page.Locator(".a2ui-button").First;
        await Expect(button).ToBeVisibleAsync(new() { Timeout = 10_000 });
        await button.FocusAsync();

        // Verify the focused button has a visible outline
        var outlineStyle = await button.EvaluateAsync<string>(
            "el => getComputedStyle(el).outlineStyle");
        Assert.That(outlineStyle, Is.Not.EqualTo("none"),
            "Button should have a visible outline when focused");
    }

    [Test]
    public async Task GalleryTextField_ShowsFocusRingOnTab()
    {
        await Page.GotoAsync($"{BaseUrl}/gallery");

        var heading = Page.GetByRole(AriaRole.Heading, new() { Name = "A2UI Component Gallery" });
        await Expect(heading).ToBeVisibleAsync(new() { Timeout = 15_000 });

        var input = Page.Locator("input#textfield1");
        await Expect(input).ToBeVisibleAsync(new() { Timeout = 10_000 });
        await input.FocusAsync();

        var outlineStyle = await input.EvaluateAsync<string>(
            "el => getComputedStyle(el).outlineStyle");
        Assert.That(outlineStyle, Is.Not.EqualTo("none"),
            "TextField input should have a visible outline when focused");
    }

    [Test]
    public async Task GalleryInteractiveElements_AllHaveFocusIndicators()
    {
        await Page.GotoAsync($"{BaseUrl}/gallery");

        var heading = Page.GetByRole(AriaRole.Heading, new() { Name = "A2UI Component Gallery" });
        await Expect(heading).ToBeVisibleAsync(new() { Timeout = 15_000 });

        // Check each type of interactive element
        var selectors = new (string selector, string name)[]
        {
            (".a2ui-button:not([disabled])", "Button"),
            (".a2ui-textfield-input", "TextField"),
            (".a2ui-checkbox input", "CheckBox"),
            (".a2ui-choicepicker-select", "ChoicePicker"),
            (".a2ui-datetime-input", "DateTimeInput"),
            (".a2ui-slider-input", "Slider"),
            (".a2ui-tab-button", "Tab"),
        };

        foreach (var (selector, name) in selectors)
        {
            var element = Page.Locator(selector).First;
            await Expect(element).ToBeVisibleAsync(new() { Timeout = 10_000 });
            await element.FocusAsync();

            var outlineStyle = await element.EvaluateAsync<string>(
                "el => getComputedStyle(el).outlineStyle");
            Assert.That(outlineStyle, Is.Not.EqualTo("none"),
                $"{name} should have a visible outline when focused");
        }
    }
}
