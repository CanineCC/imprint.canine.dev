using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Imprint.E2E;

/// <summary>
/// The LinkedIn senders are set in the site's settings, and an article names one with its text in the post's
/// Details view. Both survive a reload. LinkedIn is not set up in this fixture, and the settings say so: nothing
/// here can reach LinkedIn.
/// </summary>
[Collection("editor")]
public sealed class LinkedInSettingsTests(EditorFixture fixture)
{
    [Fact]
    public async Task A_sender_set_in_settings_is_offered_to_an_article_whose_text_survives_a_reload()
    {
        var page = await fixture.OpenEditor();
        await page.ClickAsync(".ed-gear");
        await page.WaitForURLAsync("**/settings");
        await page.WaitForInteractive();

        await Expect(page.Locator("[data-testid='linkedin-connection']")).ToContainTextAsync("not set up on this install");
        if (await page.Locator("input[aria-label='Sender key']").CountAsync() == 0)
        {
            await page.ClickToAdd("[data-testid='linkedin-sender-add']", "input[aria-label='Sender key']");
            await page.FillAsync("input[aria-label='Sender key'] >> nth=-1", "canine");
            await page.FillAsync("input[aria-label='Sender name'] >> nth=-1", "Canine Development");
            await page.FillAsync("input[aria-label='Organization URN'] >> nth=-1", "urn:li:organization:12345");
            await page.SaveAndConfirm("[data-testid='linkedin-senders-save']", () =>
                Expect(page.Locator("input[aria-label='Sender key'] >> nth=-1")).ToHaveValueAsync("canine"));
        }

        await Shot(page, "linkedin-settings.png", "#set-linkedin");

        await page.GotoAsync("/");
        await page.WaitForInteractive();
        await page.ClickAsync("[data-testid='site-posts']");
        await page.WaitForURLAsync("**/posts");
        await page.WaitForInteractive();
        var title = $"LinkedIn {Guid.NewGuid():N}"[..15];
        await page.FillAsync("[data-testid='new-post-title']", title);
        await page.ClickAsync("[data-testid='new-post-create']");
        await page.WaitForURLAsync("**/posts/**");
        await page.WaitForInteractive();

        await page.ClickAsync("[data-testid='post-view-details']");
        await Expect(page.Locator("[data-testid='post-linkedin-sender']")).ToHaveValueAsync("canine");
        await page.FillAsync("[data-testid='post-linkedin-text']", "Ten questions to ask any code benchmark. Read the article.");
        await page.ClickAsync("[data-testid='post-linkedin-save']");
        await Expect(page.Locator("[data-testid='post-linkedin-save']")).ToBeDisabledAsync();
        await Shot(page, "linkedin-post-details.png", "[data-testid='post-details']");

        await page.ReloadAsync();
        await page.WaitForInteractive();
        await page.ClickAsync("[data-testid='post-view-details']");
        await Expect(page.Locator("[data-testid='post-linkedin-text']"))
            .ToHaveValueAsync("Ten questions to ask any code benchmark. Read the article.");
        await Expect(page.Locator("[data-testid='post-linkedin-sender']")).ToHaveValueAsync("canine");
    }

    private static async Task Shot(IPage page, string name, string selector)
    {
        if (Environment.GetEnvironmentVariable("IMPRINT_E2E_SHOTS") is { Length: > 0 } shots)
        {
            await page.Locator(selector).ScrollIntoViewIfNeededAsync();
            await page.ScreenshotAsync(new() { Path = Path.Combine(shots, name) });
        }
    }
}
