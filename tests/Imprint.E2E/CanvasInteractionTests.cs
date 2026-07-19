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

        // Select the word 'bold' with the keyboard — pixel-targeting a word is
        // font/layout-dependent and once bolded the wrong word in a full-suite run.
        // Caret sits at the end after typing: ← ×7 puts it after 'bold' (' words.'),
        // Shift+← ×4 selects it.
        for (var i = 0; i < 7; i++)
        {
            await page.Keyboard.PressAsync("ArrowLeft");
        }

        for (var i = 0; i < 4; i++)
        {
            await page.Keyboard.PressAsync("Shift+ArrowLeft");
        }

        await page.WaitForSelectorAsync(".ed-richbar:not([hidden])");
        await page.Locator(".ed-richbar button", new PageLocatorOptions { HasTextString = "B" }).First
            .ClickAsync();

        // Blur commits (clicking the breadcrumb leaves the edit surface).
        await page.ClickAsync(".ed-crumb-page");
        await page.WaitForSelectorAsync($"[data-node-id='{id}'] strong:has-text('bold')");
        Assert.Equal(0, await page.Locator(".ed-toast-error").CountAsync());

        // The stored value passed the server-side canonical validator; reload proves it
        // (a non-canonical value would have been rejected and nothing would persist).
        await page.ReloadAsync();
        await page.WaitForSelectorAsync($"[data-node-id='{id}'] strong:has-text('bold')");
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
    public async Task Inspector_delete_button_removes_selection_and_undo_restores_it()
    {
        var page = await fixture.OpenEditor();
        var before = await page.CanvasOrder("section");

        await page.Select(page.Node("section", before.Count - 1));
        await page.ClickAsync(".ed-insp-delete");
        await page.WaitForFunctionAsync(
            "n => document.querySelectorAll('.ed-canvas [data-node-type=section]').length === n - 1",
            before.Count);

        // The click left focus in the panel; undo listens on the canvas surface, so
        // reselect a node there before Ctrl+Z.
        await page.Select(page.Node("section"));
        await page.Keyboard.PressAsync("Control+z");
        await page.WaitForFunctionAsync(
            "n => document.querySelectorAll('.ed-canvas [data-node-type=section]').length === n",
            before.Count);
        Assert.Equal(before, await page.CanvasOrder("section"));
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
