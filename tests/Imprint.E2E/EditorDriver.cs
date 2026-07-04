using Microsoft.Playwright;

namespace Imprint.E2E;

/// <summary>Shared driving helpers: the suite's tests each assume a site exists and an editor page is open.</summary>
public static class EditorDriver
{
    /// <summary>Opens the editor, running onboarding first if this data dir is fresh.</summary>
    public static async Task<IPage> OpenEditor(this EditorFixture fixture)
    {
        // One live circuit at a time: without this, every finished test leaves its
        // context (and Blazor circuit) running, and interop from the newest circuit
        // started getting lost mid-suite.
        foreach (var stale in fixture.Browser.Contexts.ToList())
        {
            await stale.CloseAsync();
        }

        var page = await fixture.NewPage();
        await page.GotoAsync("/");
        await page.WaitForInteractive();
        if (await page.Locator("#ob-name").CountAsync() > 0)
        {
            // Empty dashboard: the onboarding form is shown for the first site.
            await page.FillAsync("#ob-name", "Skeleton Works");
            await page.FillAsync("#ob-locale", "en");
            await page.SelectOptionAsync("#ob-template", "launch");
            await page.ClickAsync("button:has-text('Create site')");
        }
        else
        {
            // Dashboard with existing sites (shared fixture, later tests): open the first
            // site's card — the "New site" card is excluded by class.
            await page.ClickAsync(".dash-open:not(.dash-open-new)");
        }

        await page.WaitForURLAsync("**/edit/**", new PageWaitForURLOptions { Timeout = 30_000 });
        await page.WaitForInteractive();
        await page.WaitForSelectorAsync(".ed-canvas [data-node-id]");
        return page;
    }

    /// <summary>
    /// Blazor prerenders identical-looking dead HTML; clicks before the circuit
    /// attaches vanish. The marker is rendered only from OnAfterRender (interactive).
    /// </summary>
    public static Task WaitForInteractive(this IPage page) =>
        page.WaitForSelectorAsync("[data-interactive]",
            new PageWaitForSelectorOptions { State = WaitForSelectorState.Attached, Timeout = 30_000 });

    public static ILocator Node(this IPage page, string type, int nth = 0) =>
        page.Locator($".ed-canvas [data-node-type='{type}']").Nth(nth);

    public static async Task<string> NodeId(this ILocator node) =>
        await node.GetAttributeAsync("data-node-id") ?? throw new InvalidOperationException("node id missing");

    public static async Task Select(this IPage page, ILocator node)
    {
        await node.ClickAsync(new LocatorClickOptions { Position = new Position { X = 8, Y = 8 } });
        await page.WaitForSelectorAsync(".ed-ov-selection:not([hidden])");
    }

    /// <summary>Document order of node ids — structural assertions read the canvas like the publisher would.</summary>
    public static async Task<IReadOnlyList<string>> CanvasOrder(this IPage page, string type) =>
        await page.Locator($".ed-canvas [data-node-type='{type}']")
            .EvaluateAllAsync<string[]>("els => els.map(e => e.getAttribute('data-node-id'))");

    /// <summary>Drags the current selection's handle to the center-bottom of a target node.</summary>
    public static async Task DragSelectionTo(this IPage page, ILocator target)
    {
        var handle = page.Locator(".ed-ov-handle");
        var handleBox = await handle.BoundingBoxAsync() ?? throw new InvalidOperationException("no drag handle");
        var targetBox = await target.BoundingBoxAsync() ?? throw new InvalidOperationException("no target box");

        await page.Mouse.MoveAsync(handleBox.X + handleBox.Width / 2, handleBox.Y + handleBox.Height / 2);
        await page.Mouse.DownAsync();
        // Cross the lift threshold, then travel in steps so tracking sees real moves.
        await page.Mouse.MoveAsync(handleBox.X + 20, handleBox.Y + 20, new MouseMoveOptions { Steps = 4 });
        await page.Mouse.MoveAsync(
            targetBox.X + targetBox.Width / 2,
            targetBox.Y + targetBox.Height - 4,
            new MouseMoveOptions { Steps = 12 });
        await page.WaitForSelectorAsync(".ed-ov-indicator:not([hidden]), .ed-ov-into:not([hidden])",
            new PageWaitForSelectorOptions { Timeout = 5000 });
        await page.Mouse.UpAsync();
    }
}
