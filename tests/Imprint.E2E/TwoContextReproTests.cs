namespace Imprint.E2E;

/// <summary>Temporary diagnosis rig for the second-context interop hang. Deleted once fixed.</summary>
[Collection("editor")]
public sealed class TwoContextReproTests(EditorFixture fixture)
{
    [Fact]
    public async Task Rich_edit_commits_after_delete_undo_in_another_context()
    {
        // Context A: delete the last section and undo — exactly what Delete_key does.
        var pageA = await fixture.OpenEditor();
        var sections = await pageA.CanvasOrder("section");
        await pageA.Select(pageA.Node("section", sections.Count - 1));
        await pageA.Keyboard.PressAsync("Delete");
        await pageA.WaitForFunctionAsync(
            "n => document.querySelectorAll('.ed-canvas [data-node-type=section]').length === n - 1",
            sections.Count);
        await pageA.Keyboard.PressAsync("Control+z");
        await pageA.WaitForFunctionAsync(
            "n => document.querySelectorAll('.ed-canvas [data-node-type=section]').length === n",
            sections.Count);

        // Context B: rich-text edit, committed by plain typing + blur via Escape? No —
        // debounce alone should persist within ~1s of idling.
        var pageB = await fixture.OpenEditor();
        var richText = pageB.Node("richtext");
        var id = await richText.NodeId();
        await richText.DblClickAsync();
        await pageB.WaitForSelectorAsync("[contenteditable].ed-editing");
        await pageB.Keyboard.PressAsync("Control+a");
        await pageB.Keyboard.TypeAsync("Plain and bold words.");
        // NO settle wait: blur must land inside the first debounce window — the
        // hypothesis is that the blur-path commit is the one that gets lost.

        // Step 2: word-select via dblclick inside the live editable.
        var editable = pageB.Locator("[contenteditable].ed-editing");
        var box = await editable.BoundingBoxAsync();
        await editable.DblClickAsync(new Microsoft.Playwright.LocatorDblClickOptions
        {
            Position = new Microsoft.Playwright.Position { X = 60, Y = (float)(box!.Height / 2) },
        });
        Assert.Equal(1, await pageB.Locator("[contenteditable].ed-editing").CountAsync());

        // Step 3: bold via the floating toolbar (real click).
        await pageB.WaitForSelectorAsync(".ed-richbar:not([hidden])");
        await pageB.Locator(".ed-richbar button").First.ClickAsync();

        // Step 4: immediate blur via the breadcrumb — inside the debounce window.
        await pageB.ClickAsync(".ed-crumb-page");
        await pageB.WaitForTimeoutAsync(1200);
        Assert.Equal(0, await pageB.Locator("[contenteditable].ed-editing").CountAsync());
        Assert.True(CountTextEvents() > 0,
            $"blur-path commit lost. data dir: {fixture.DataDirectory}");
    }

    private int CountTextEvents()
    {
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection(
            $"Data Source={Path.Combine(fixture.DataDirectory, "imprint.db")}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM events WHERE stable_id LIKE 'page.text%'";
        return Convert.ToInt32(command.ExecuteScalar());
    }
}
