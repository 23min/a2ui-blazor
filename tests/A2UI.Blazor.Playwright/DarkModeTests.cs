using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace A2UI.Blazor.Playwright;

[TestFixture]
public class DarkModeTests : PageTest
{
    private string BaseUrl => ServerFixture.SpaBaseUrl;

    [Test]
    public async Task DarkMode_BackgroundChangesToDark()
    {
        await Page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Dark });
        await Page.GotoAsync($"{BaseUrl}/gallery");

        var heading = Page.GetByRole(AriaRole.Heading, new() { Name = "A2UI Component Gallery" });
        await Expect(heading).ToBeVisibleAsync(new() { Timeout = 15_000 });

        // Card background should be dark (not white)
        var card = Page.Locator(".a2ui-card").First;
        var bgColor = await card.EvaluateAsync<string>(
            "el => getComputedStyle(el).backgroundColor");

        // Should NOT be white (rgb(255, 255, 255))
        Assert.That(bgColor, Is.Not.EqualTo("rgb(255, 255, 255)"),
            "Card background should not be white in dark mode");
    }

    [Test]
    public async Task DarkMode_TextIsLightOnDarkBackground()
    {
        await Page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Dark });
        await Page.GotoAsync($"{BaseUrl}/gallery");

        var heading = Page.GetByRole(AriaRole.Heading, new() { Name = "A2UI Component Gallery" });
        await Expect(heading).ToBeVisibleAsync(new() { Timeout = 15_000 });

        // Text color should be light (high R/G/B values)
        var surface = Page.Locator(".a2ui-surface").First;
        var textColor = await surface.EvaluateAsync<string>(
            "el => getComputedStyle(el).color");

        // Should NOT be near-black (rgb(26, 26, 26) = #1a1a1a)
        Assert.That(textColor, Is.Not.EqualTo("rgb(26, 26, 26)"),
            "Text should not be dark in dark mode");
    }

    [Test]
    public async Task DarkMode_TextFieldInputHasDarkBackground()
    {
        await Page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Dark });
        await Page.GotoAsync($"{BaseUrl}/gallery");

        var heading = Page.GetByRole(AriaRole.Heading, new() { Name = "A2UI Component Gallery" });
        await Expect(heading).ToBeVisibleAsync(new() { Timeout = 15_000 });

        var input = Page.Locator("input.a2ui-textfield-input").First;
        await Expect(input).ToBeVisibleAsync(new() { Timeout = 10_000 });

        var bgColor = await input.EvaluateAsync<string>(
            "el => getComputedStyle(el).backgroundColor");

        // Should NOT be white (browser default for inputs)
        Assert.That(bgColor, Is.Not.EqualTo("rgb(255, 255, 255)"),
            "TextField input should not have white background in dark mode");
    }

    [Test]
    public async Task DarkMode_SelectElementTextIsReadable()
    {
        await Page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Dark });
        await Page.GotoAsync($"{BaseUrl}/gallery");

        var heading = Page.GetByRole(AriaRole.Heading, new() { Name = "A2UI Component Gallery" });
        await Expect(heading).ToBeVisibleAsync(new() { Timeout = 15_000 });

        var select = Page.Locator("select.a2ui-choicepicker-select").First;
        await Expect(select).ToBeVisibleAsync(new() { Timeout = 10_000 });

        var color = await select.EvaluateAsync<string>(
            "el => getComputedStyle(el).color");

        // Should NOT be black or near-black (unreadable on dark background)
        Assert.That(color, Is.Not.EqualTo("rgb(0, 0, 0)"),
            "Select text should not be black in dark mode");
    }

    [Test]
    public async Task DarkMode_AppNavHasDarkBackground()
    {
        await Page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Dark });
        await Page.GotoAsync($"{BaseUrl}/gallery");

        var heading = Page.GetByRole(AriaRole.Heading, new() { Name = "A2UI Component Gallery" });
        await Expect(heading).ToBeVisibleAsync(new() { Timeout = 15_000 });

        var nav = Page.Locator(".app-nav");
        var bgColor = await nav.EvaluateAsync<string>(
            "el => getComputedStyle(el).backgroundColor");

        // Nav should NOT be white in dark mode
        Assert.That(bgColor, Is.Not.EqualTo("rgb(255, 255, 255)"),
            "Nav should not have white background in dark mode");
    }

    [Test]
    public async Task DarkMode_ToggleButton_SwitchesTheme()
    {
        await Page.GotoAsync($"{BaseUrl}/gallery");

        var heading = Page.GetByRole(AriaRole.Heading, new() { Name = "A2UI Component Gallery" });
        await Expect(heading).ToBeVisibleAsync(new() { Timeout = 15_000 });

        // Find toggle button
        var toggle = Page.GetByRole(AriaRole.Button, new() { Name = "Toggle theme" });
        await Expect(toggle).ToBeVisibleAsync(new() { Timeout = 10_000 });

        // Initial state should be light (system default)
        var card = Page.Locator(".a2ui-card").First;
        var initialBg = await card.EvaluateAsync<string>(
            "el => getComputedStyle(el).backgroundColor");
        Assert.That(initialBg, Is.EqualTo("rgb(255, 255, 255)"),
            "Card should have white background in light mode");

        // Click to switch to dark mode
        await toggle.ClickAsync();

        // Verify dark mode is active
        var darkBg = await card.EvaluateAsync<string>(
            "el => getComputedStyle(el).backgroundColor");
        Assert.That(darkBg, Is.Not.EqualTo("rgb(255, 255, 255)"),
            "Card background should not be white after toggling to dark mode");

        // Verify data-theme attribute was set
        var theme = await Page.EvaluateAsync<string>(
            "document.documentElement.getAttribute('data-theme')");
        Assert.That(theme, Is.EqualTo("dark"));
    }

    [Test]
    public async Task ReducedMotion_TransitionsDisabled()
    {
        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        await Page.GotoAsync($"{BaseUrl}/gallery");

        var heading = Page.GetByRole(AriaRole.Heading, new() { Name = "A2UI Component Gallery" });
        await Expect(heading).ToBeVisibleAsync(new() { Timeout = 15_000 });

        // Button should have no transition
        var button = Page.Locator(".a2ui-button").First;
        await Expect(button).ToBeVisibleAsync(new() { Timeout = 10_000 });
        var transitionDuration = await button.EvaluateAsync<string>(
            "el => getComputedStyle(el).transitionDuration");

        Assert.That(transitionDuration, Is.EqualTo("0s"),
            "Button should have no transition when reduced motion is preferred");
    }
}
