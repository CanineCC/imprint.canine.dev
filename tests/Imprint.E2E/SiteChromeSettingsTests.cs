using Microsoft.Playwright;

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
        await page.WaitForURLAsync("**/settings", new PageWaitForURLOptions { Timeout = 30_000 });
        await page.WaitForInteractive();

        // Copy line.
        await page.FillAsync("input[aria-label='Footer copy line']", "© 2026 · Chrome test");
        await page.ClickAsync("button:has-text('Save copy line')");
        await page.WaitForSelectorAsync("text=Copy line saved.");

        // A footer column with one external link.
        await page.ClickAsync("button:has-text('＋ Add column')");
        await page.FillAsync("input[aria-label='Footer column heading'] >> nth=-1", "Elsewhere");
        await page.ClickAsync("button:has-text('＋ Add link') >> nth=-1");
        await page.FillAsync("input[aria-label='Footer link label'] >> nth=-1", "Example");
        await page.FillAsync("input[aria-label='Footer link target'] >> nth=-1", "https://example.com/");
        await page.ClickAsync("button:has-text('Save footer')");
        await page.WaitForSelectorAsync("text=Footer saved.");

        // Survives a full reload — the events really landed.
        await page.ReloadAsync();
        await page.WaitForInteractive();
        Assert.Equal("© 2026 · Chrome test",
            await page.InputValueAsync("input[aria-label='Footer copy line']"));
        Assert.Equal("Elsewhere",
            await page.InputValueAsync("input[aria-label='Footer column heading'] >> nth=-1"));
        Assert.Equal("https://example.com/",
            await page.InputValueAsync("input[aria-label='Footer link target'] >> nth=-1"));
    }

    [Fact]
    public async Task Navigation_entries_round_trip_through_the_menu_editor()
    {
        var page = await fixture.OpenEditor();

        await page.ClickAsync(".ed-gear");
        await page.WaitForURLAsync("**/settings", new PageWaitForURLOptions { Timeout = 30_000 });
        await page.WaitForInteractive();

        // An external entry plus a dropdown group with one sub-link.
        await page.ClickAsync("button:has-text('＋ Add menu entry')");
        await page.FillAsync("input[aria-label='Menu entry label'] >> nth=-1", "Docs");
        await page.FillAsync("input[aria-label='Menu entry target'] >> nth=-1", "https://docs.example.com/");

        await page.ClickAsync("button:has-text('＋ Add menu entry')");
        await page.FillAsync("input[aria-label='Menu entry label'] >> nth=-1", "More");
        await page.ClickAsync("button:has-text('＋ Add sub-link') >> nth=-1");
        await page.FillAsync("input[aria-label='Sub-link label'] >> nth=-1", "Blog");
        await page.FillAsync("input[aria-label='Sub-link target'] >> nth=-1", "https://blog.example.com/");

        await page.ClickAsync("button:has-text('Save navigation')");
        await page.WaitForSelectorAsync("text=Navigation saved.");

        await page.ReloadAsync();
        await page.WaitForInteractive();
        Assert.Equal("https://docs.example.com/",
            await page.InputValueAsync("input[aria-label='Menu entry target'] >> nth=-2"));
        Assert.Equal("Blog",
            await page.InputValueAsync("input[aria-label='Sub-link label'] >> nth=-1"));
    }
}
