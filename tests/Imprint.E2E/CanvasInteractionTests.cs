using Microsoft.Playwright;

namespace Imprint.E2E;

/// <summary>
/// The direct-manipulation contract, driven with a real mouse and keyboard:
/// docs/editor-ux.md §2 (selection), §3 (drag and drop), §4 (inline editing).
/// </summary>
[Collection("editor")]
public sealed class CanvasInteractionTests(EditorFixture fixture)
{
    [Fact]
    public async Task Drag_and_drop_reorders_sections_and_undo_restores()
    {
        var page = await fixture.OpenEditor();

        var before = await page.CanvasOrder("section");
        Assert.True(before.Count >= 3, "need at least three sections to reorder");

        // Drag the first section below the second: after-removal index 1.
        await page.Select(page.Node("section"));
        await page.DragSelectionTo(page.Node("section", 1));

        await page.WaitForFunctionAsync(
            "expected => document.querySelector('.ed-canvas [data-node-type=section]').getAttribute('data-node-id') !== expected",
            before[0]);
        var after = await page.CanvasOrder("section");
        Assert.Equal(before[1], after[0]);
        Assert.Equal(before[0], after[1]);
        Assert.Equal(before.Count, after.Count);

        // Ctrl+Z: the compensating command puts it back.
        await page.Keyboard.PressAsync("Control+z");
        await page.WaitForFunctionAsync(
            "expected => document.querySelector('.ed-canvas [data-node-type=section]').getAttribute('data-node-id') === expected",
            before[0]);
        Assert.Equal(before, await page.CanvasOrder("section"));
    }

    [Fact]
    public async Task Inline_edit_of_a_heading_commits_and_survives_reload()
    {
        var page = await fixture.OpenEditor();
        var heading = page.Node("heading");
        var headingId = await heading.NodeId();

        await heading.DblClickAsync();
        await page.WaitForSelectorAsync("[contenteditable].ed-editing");
        await page.Keyboard.PressAsync("Control+a");
        var text = $"Edited {Guid.NewGuid():N}"[..20];
        await page.Keyboard.TypeAsync(text);
        await page.Keyboard.PressAsync("Enter"); // plain mode: Enter commits

        await page.WaitForSelectorAsync($"[data-node-id='{headingId}']:has-text('{text}')");

        await page.ReloadAsync();
        await page.WaitForSelectorAsync($"[data-node-id='{headingId}']:has-text('{text}')");
    }

    [Fact]
    public async Task Rich_text_bold_survives_the_canonical_normalizer()
    {
        var page = await fixture.OpenEditor();
        var richText = page.Node("richtext");
        var id = await richText.NodeId();

        await richText.DblClickAsync();
        await page.WaitForSelectorAsync("[contenteditable].ed-editing");
        await page.Keyboard.PressAsync("Control+a");
        await page.Keyboard.TypeAsync("Plain and bold words.");

        // Real gestures only: double-click selects the word, a real click hits B —
        // synthetic Range/dispatchEvent scaffolding proved flaky and unrepresentative.
        await page.Locator("[contenteditable].ed-editing").DblClickAsync(new LocatorDblClickOptions
        {
            Position = await CenterOfWord(page, "bold"),
        });
        await page.WaitForSelectorAsync(".ed-richbar:not([hidden])");
        await page.Locator(".ed-richbar button", new PageLocatorOptions { HasTextString = "B" }).First
            .ClickAsync();

        // Blur commits (clicking the breadcrumb leaves the edit surface).
        await page.ClickAsync(".ed-crumb-page");
        try
        {
            await page.WaitForSelectorAsync($"[data-node-id='{id}'] strong:has-text('bold')",
                new PageWaitForSelectorOptions { Timeout = 10_000 });
        }
        catch (Exception e) when (e is System.TimeoutException or PlaywrightException)
        {
            var html = await page.Locator($"[data-node-id='{id}']").InnerHTMLAsync();
            var editing = await page.Locator("[contenteditable].ed-editing").CountAsync();
            var toasts = await page.Locator(".ed-toast").AllInnerTextsAsync();
            Assert.Fail($"bold did not commit.\nnode html: {html}\nstill editing: {editing}\ntoasts: {string.Join(" | ", toasts)}");
        }

        Assert.Equal(0, await page.Locator(".ed-toast-error").CountAsync());

        // The stored value passed the server-side canonical validator; reload proves it.
        await page.ReloadAsync();
        try
        {
            await page.WaitForSelectorAsync($"[data-node-id='{id}'] strong:has-text('bold')",
                new PageWaitForSelectorOptions { Timeout = 10_000 });
        }
        catch (Exception e) when (e is System.TimeoutException or PlaywrightException)
        {
            var html = await page.Locator($"[data-node-id='{id}']").InnerHTMLAsync();
            Assert.Fail($"bold lost after reload.\ndata dir: {fixture.DataDirectory}\nnode html now: {html}");
        }
    }

    /// <summary>Viewport-relative center of a word inside the live edit surface, as an element-relative position.</summary>
    private static async Task<Microsoft.Playwright.Position> CenterOfWord(IPage page, string word)
    {
        var box = await page.EvaluateAsync<double[]>(
            """
            word => {
                const el = document.querySelector('[contenteditable].ed-editing');
                const walker = document.createTreeWalker(el, NodeFilter.SHOW_TEXT);
                for (let node; (node = walker.nextNode()); ) {
                    const at = node.data.indexOf(word);
                    if (at >= 0) {
                        const range = document.createRange();
                        range.setStart(node, at);
                        range.setEnd(node, at + word.length);
                        const r = range.getBoundingClientRect();
                        const host = el.getBoundingClientRect();
                        return [r.left + r.width / 2 - host.left, r.top + r.height / 2 - host.top];
                    }
                }
                throw new Error('word not found: ' + word);
            }
            """, word);
        return new Microsoft.Playwright.Position { X = (float)box[0], Y = (float)box[1] };
    }

    [Fact]
    public async Task Escape_walks_selection_up_the_ancestor_path()
    {
        var page = await fixture.OpenEditor();
        await page.Select(page.Node("heading"));
        var crumbsDeep = await page.Locator(".ed-crumb").CountAsync();

        await page.Keyboard.PressAsync("Escape");
        await page.WaitForFunctionAsync(
            "n => document.querySelectorAll('.ed-crumb').length < n", crumbsDeep);

        Assert.True(await page.Locator(".ed-crumb").CountAsync() < crumbsDeep);
    }

    [Fact]
    public async Task Delete_key_removes_selection_and_undo_restores_it()
    {
        var page = await fixture.OpenEditor();
        var before = await page.CanvasOrder("section");

        await page.Select(page.Node("section", before.Count - 1));
        await page.Keyboard.PressAsync("Delete");
        await page.WaitForFunctionAsync(
            "n => document.querySelectorAll('.ed-canvas [data-node-type=section]').length === n - 1",
            before.Count);

        await page.Keyboard.PressAsync("Control+z");
        await page.WaitForFunctionAsync(
            "n => document.querySelectorAll('.ed-canvas [data-node-type=section]').length === n",
            before.Count);
        Assert.Equal(before, await page.CanvasOrder("section"));
    }
}
