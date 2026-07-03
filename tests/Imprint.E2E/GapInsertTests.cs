using Microsoft.Playwright;

namespace Imprint.E2E;

/// <summary>
/// The insertion affordance (docs/editor-ux.md §5): hovering a boundary reveals a "+"
/// pill; clicking it opens the block picker; choosing an item inserts at that slot.
/// Regression guard — the pill used to vanish the instant the cursor reached it.
/// </summary>
[Collection("editor")]
public sealed class GapInsertTests(EditorFixture fixture)
{
    [Fact]
    public async Task Gap_pill_opens_the_picker_and_inserts_an_element()
    {
        var page = await fixture.OpenEditor();

        // Count all nodes before, so we can prove the insert happened regardless of
        // which group the picker shows for the boundary we land on.
        var before = await page.Locator(".ed-canvas [data-node-id]").CountAsync();

        // Find a boundary where the pill appears, inside the first section.
        var section = page.Node("section", 0);
        var box = await section.BoundingBoxAsync() ?? throw new InvalidOperationException("no section box");
        var pillBox = await ScanForPill(page, box.X + box.Width / 2, box.Y + 6, box.Y + box.Height - 6);
        Assert.NotNull(pillBox);

        // Move onto the pill (it must survive) and click it.
        await page.Mouse.MoveAsync(pillBox.X + (float)(pillBox.Width / 2), pillBox.Y + (float)(pillBox.Height / 2));
        Assert.True(await page.Locator(".ed-ov-gap:not([hidden])").CountAsync() > 0,
            "the pill vanished when the cursor moved onto it");
        await page.Mouse.DownAsync();
        await page.Mouse.UpAsync();

        // The picker opens on-screen; choosing the first item inserts new content.
        await page.WaitForSelectorAsync(".ed-picker");
        Assert.True(await page.Locator(".ed-picker").IsVisibleAsync());
        await page.Locator(".ed-picker-item").First.ClickAsync();

        await page.WaitForFunctionAsync(
            "n => document.querySelectorAll('.ed-canvas [data-node-id]').length > n", before);
    }

    [Fact]
    public async Task Slash_key_opens_the_picker_on_screen()
    {
        var page = await fixture.OpenEditor();
        await page.Select(page.Node("heading", 0));

        await page.Keyboard.PressAsync("/");
        await page.WaitForSelectorAsync(".ed-picker");
        Assert.True(await page.Locator(".ed-picker").IsVisibleAsync(), "the slash-command picker opened off-screen");
    }

    private static async Task<LocatorBoundingBoxResult?> ScanForPill(IPage page, double x, double yStart, double yEnd)
    {
        for (var y = yStart; y <= yEnd; y += 6)
        {
            await page.Mouse.MoveAsync((float)x, (float)y);
            await page.Mouse.MoveAsync((float)x, (float)(y + 1)); // nudge so a fresh pointermove fires
            var pill = page.Locator(".ed-ov-gap:not([hidden])");
            if (await pill.CountAsync() > 0 && await pill.BoundingBoxAsync() is { } b)
            {
                return b;
            }
        }

        return null;
    }
}
