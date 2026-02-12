using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace A2UI.Blazor.Playwright;

[TestFixture]
public class AccessibleFormsTests : PageTest
{
    private string BaseUrl => ServerFixture.SpaBaseUrl;

    [Test]
    public async Task GalleryTextField_LabelLinkedToInput()
    {
        await Page.GotoAsync($"{BaseUrl}/gallery");

        // Wait for gallery to load
        var heading = Page.GetByRole(AriaRole.Heading, new() { Name = "A2UI Component Gallery" });
        await Expect(heading).ToBeVisibleAsync(new() { Timeout = 15_000 });

        // Label for="textfield1" should match input id="textfield1"
        var label = Page.Locator("label.a2ui-textfield-label[for='textfield1']");
        await Expect(label).ToBeVisibleAsync(new() { Timeout = 10_000 });
        await Expect(label).ToContainTextAsync("Text Field");

        var input = Page.Locator("input#textfield1");
        await Expect(input).ToBeVisibleAsync();
    }

    [Test]
    public async Task GalleryChoicePicker_LabelLinkedToSelect()
    {
        await Page.GotoAsync($"{BaseUrl}/gallery");

        var heading = Page.GetByRole(AriaRole.Heading, new() { Name = "A2UI Component Gallery" });
        await Expect(heading).ToBeVisibleAsync(new() { Timeout = 15_000 });

        // Label for="choicepicker1" should match select id="choicepicker1"
        var label = Page.Locator("label.a2ui-choicepicker-label[for='choicepicker1']");
        await Expect(label).ToBeVisibleAsync(new() { Timeout = 10_000 });
        await Expect(label).ToContainTextAsync("Favorite color");

        var select = Page.Locator("select#choicepicker1");
        await Expect(select).ToBeVisibleAsync();
    }

    [Test]
    public async Task GallerySlider_LabelLinkedToInput()
    {
        await Page.GotoAsync($"{BaseUrl}/gallery");

        var heading = Page.GetByRole(AriaRole.Heading, new() { Name = "A2UI Component Gallery" });
        await Expect(heading).ToBeVisibleAsync(new() { Timeout = 15_000 });

        // Label for="slider1" should match input id="slider1"
        var label = Page.Locator("label.a2ui-slider-label[for='slider1']");
        await Expect(label).ToBeVisibleAsync(new() { Timeout = 10_000 });
        await Expect(label).ToContainTextAsync("Volume");

        var input = Page.Locator("input#slider1");
        await Expect(input).ToBeVisibleAsync();
    }

    [Test]
    public async Task GalleryValidation_ErrorTextField_RendersWithErrorState()
    {
        await Page.GotoAsync($"{BaseUrl}/gallery");

        var heading = Page.GetByRole(AriaRole.Heading, new() { Name = "Validation" });
        await Expect(heading).ToBeVisibleAsync(new() { Timeout = 15_000 });

        // TextField with error should render (not show "failed to render")
        var errorTextField = Page.Locator(".a2ui-textfield--error");
        await Expect(errorTextField).ToBeVisibleAsync(new() { Timeout = 10_000 });

        // Should show the error text
        var errorText = Page.Locator("#tf-error-error.a2ui-input-error");
        await Expect(errorText).ToBeVisibleAsync();
        await Expect(errorText).ToContainTextAsync("valid email");

        // Should have aria-invalid on input
        var input = Page.Locator("input#tf-error");
        await Expect(input).ToHaveAttributeAsync("aria-invalid", "true");
    }

    [Test]
    public async Task GalleryValidation_HelperTextField_RendersWithHelperText()
    {
        await Page.GotoAsync($"{BaseUrl}/gallery");

        var heading = Page.GetByRole(AriaRole.Heading, new() { Name = "Validation" });
        await Expect(heading).ToBeVisibleAsync(new() { Timeout = 15_000 });

        // TextField with helperText should render
        var helperText = Page.Locator("#tf-helper-helper.a2ui-input-helper");
        await Expect(helperText).ToBeVisibleAsync(new() { Timeout = 10_000 });
        await Expect(helperText).ToContainTextAsync("3-20 characters");

        // Input should link to helper via aria-describedby
        var input = Page.Locator("input#tf-helper");
        await Expect(input).ToHaveAttributeAsync("aria-describedby", "tf-helper-helper");
    }

    [Test]
    public async Task GalleryValidation_NoRenderFailures()
    {
        await Page.GotoAsync($"{BaseUrl}/gallery");

        var heading = Page.GetByRole(AriaRole.Heading, new() { Name = "Validation" });
        await Expect(heading).ToBeVisibleAsync(new() { Timeout = 15_000 });

        // No components should show "failed to render" on the entire page
        var errors = Page.Locator(".a2ui-component-error");
        await Expect(errors).ToHaveCountAsync(0);
    }

    [Test]
    public async Task GalleryTextField_TypingDoesNotCrash()
    {
        await Page.GotoAsync($"{BaseUrl}/gallery");

        var heading = Page.GetByRole(AriaRole.Heading, new() { Name = "A2UI Component Gallery" });
        await Expect(heading).ToBeVisibleAsync(new() { Timeout = 15_000 });

        // Type into the regular text field
        var input = Page.Locator("input#textfield1");
        await Expect(input).ToBeVisibleAsync(new() { Timeout = 10_000 });
        await input.FillAsync("hello");

        // Wait for any re-render
        await Page.WaitForTimeoutAsync(1000);

        // TextField should still be visible (not replaced by error boundary)
        await Expect(input).ToBeVisibleAsync();

        // No render errors should appear
        var errors = Page.Locator(".a2ui-component-error");
        await Expect(errors).ToHaveCountAsync(0);
    }

    [Test]
    public async Task GalleryValidation_TypingInErrorTextField_DoesNotCrash()
    {
        await Page.GotoAsync($"{BaseUrl}/gallery");

        var heading = Page.GetByRole(AriaRole.Heading, new() { Name = "Validation" });
        await Expect(heading).ToBeVisibleAsync(new() { Timeout = 15_000 });

        // Type into the error text field
        var input = Page.Locator("input#tf-error");
        await Expect(input).ToBeVisibleAsync(new() { Timeout = 10_000 });
        await input.FillAsync("test@example.com");

        // Wait for any re-render
        await Page.WaitForTimeoutAsync(1000);

        // TextField should still be visible
        await Expect(input).ToBeVisibleAsync();

        // No NEW render errors should appear
        var errors = Page.Locator(".a2ui-component-error");
        await Expect(errors).ToHaveCountAsync(0);
    }

    [Test]
    public async Task GalleryValidation_ErrorDismissesOnInteraction()
    {
        await Page.GotoAsync($"{BaseUrl}/gallery");

        var heading = Page.GetByRole(AriaRole.Heading, new() { Name = "Validation" });
        await Expect(heading).ToBeVisibleAsync(new() { Timeout = 15_000 });

        // Error text should be visible initially
        var errorText = Page.Locator("#tf-error-error.a2ui-input-error");
        await Expect(errorText).ToBeVisibleAsync(new() { Timeout = 10_000 });

        // Type into the error text field
        var input = Page.Locator("input#tf-error");
        await input.FillAsync("test@example.com");

        // Error text should disappear after interaction
        await Expect(errorText).ToBeHiddenAsync(new() { Timeout = 5_000 });

        // aria-invalid should also be removed
        await Expect(input).Not.ToHaveAttributeAsync("aria-invalid", "true");
    }
}
