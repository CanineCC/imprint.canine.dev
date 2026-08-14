using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Imprint.E2E;

/// <summary>
/// The settings page's "Header & footer" card is where the marketing chrome is
/// maintained (header actions, footer columns, copy line) — previously seeder-only.
/// Round-trips the copy line and a footer column through the real commands.
/// </summary>
[Collection("editor")]
public sealed class SiteChromeSettingsTests(EditorFixture fixture)
{
    [Fact]
    public async Task Copy_line_and_footer_column_round_trip_through_settings()
    {
        var page = await fixture.OpenEditor();

        await page.ClickAsync(".ed-gear");
        await page.WaitForURLAsync("**/settings");
        await page.WaitForInteractive();

        // Copy line. SaveAndConfirm reloads and re-checks until the value is there, so what is
        // asserted is that the event LANDED — not that a toast happened to be on screen when we
        // looked. Retrying assertions throughout, never a bare read: nth=-1 already matches
        // whatever was last before the new rows render.
        await page.FillAsync("input[aria-label='Footer copy line']", "© 2026 · Chrome test");
        await page.SaveAndConfirm("button:has-text('Save copy line')", () =>
            Expect(page.Locator("input[aria-label='Footer copy line']")).ToHaveValueAsync("© 2026 · Chrome test"));

        // A footer column with one external link.
        await page.ClickToAdd("button:has-text('＋ Add column')", "input[aria-label='Footer column heading']");
        await page.FillAsync("input[aria-label='Footer column heading'] >> nth=-1", "Elsewhere");
        await page.ClickToAdd("button:has-text('＋ Add link') >> nth=-1", "input[aria-label='Footer link label']");
        await page.FillAsync("input[aria-label='Footer link label'] >> nth=-1", "Example");
        await page.FillAsync("input[aria-label='Footer link target'] >> nth=-1", "https://example.com/");
        await page.SaveAndConfirm("button:has-text('Save footer')", async () =>
        {
            await Expect(page.Locator("input[aria-label='Footer column heading'] >> nth=-1")).ToHaveValueAsync("Elsewhere");
            await Expect(page.Locator("input[aria-label='Footer link target'] >> nth=-1")).ToHaveValueAsync("https://example.com/");
        });

        // The copy line is still there too — the second save did not clobber the first.
        await Expect(page.Locator("input[aria-label='Footer copy line']")).ToHaveValueAsync("© 2026 · Chrome test");
    }

    [Fact]
    public async Task Navigation_entries_round_trip_through_the_menu_editor()
    {
        var page = await fixture.OpenEditor();

        await page.ClickAsync(".ed-gear");
        await page.WaitForURLAsync("**/settings");
        await page.WaitForInteractive();

        // An external entry plus a dropdown group with one sub-link.
        // Each add waits for its row to exist: nth=-1 otherwise names the row that was last
        // BEFORE the click, and the fill lands on the wrong entry whenever the render lags.
        await page.ClickToAdd("button:has-text('＋ Add menu entry')", "input[aria-label='Menu entry label']");
        await page.FillAsync("input[aria-label='Menu entry label'] >> nth=-1", "Docs");
        await page.FillAsync("input[aria-label='Menu entry target'] >> nth=-1", "https://docs.example.com/");

        await page.ClickToAdd("button:has-text('＋ Add menu entry')", "input[aria-label='Menu entry label']");
        await page.FillAsync("input[aria-label='Menu entry label'] >> nth=-1", "More");
        await page.ClickToAdd("button:has-text('＋ Add sub-link') >> nth=-1", "input[aria-label='Sub-link label']");
        await page.FillAsync("input[aria-label='Sub-link label'] >> nth=-1", "Blog");
        await page.FillAsync("input[aria-label='Sub-link target'] >> nth=-1", "https://blog.example.com/");

        await page.SaveAndConfirm("button:has-text('Save navigation')", async () =>
        {
            await Expect(page.Locator("input[aria-label='Menu entry target'] >> nth=-2")).ToHaveValueAsync("https://docs.example.com/");
            await Expect(page.Locator("input[aria-label='Sub-link label'] >> nth=-1")).ToHaveValueAsync("Blog");
        });
    }
}
