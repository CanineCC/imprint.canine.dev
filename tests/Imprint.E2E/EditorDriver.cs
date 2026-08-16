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
            // site's card — the "New site" card is excluded by class, and the whole query is
            // scoped to the Sites shelf because the Blogs shelf below it draws the same card
            // markup. An unscoped selector would open a blog here and then wait forever for
            // an /edit/ URL that a blog never navigates to.
            await page.ClickAsync("ul[aria-label='Sites'] .dash-open:not(.dash-open-new)");
        }

        await page.WaitForURLAsync("**/edit/**");
        await page.WaitForInteractive();
        await page.WaitForSelectorAsync(".ed-canvas [data-node-id]");
        return page;
    }

    /// <summary>
    /// Creates a fresh site through the "New site" flow and lands in its editor with the
    /// canvas rendered — the multi-site equivalent of OpenEditor for a brand-new site.
    /// </summary>
    public static async Task CreateSiteViaDashboard(this IPage page, string name)
    {
        await page.GotoAsync("/sites/new");
        await page.WaitForInteractive();
        await page.FillAsync("#ob-name", name);
        await page.FillAsync("#ob-locale", "en");
        await page.SelectOptionAsync("#ob-template", "launch");
        await page.ClickAsync("button:has-text('Create site')");
        await page.WaitForURLAsync("**/edit/**");
        await page.WaitForInteractive();
        await page.WaitForSelectorAsync(".ed-canvas [data-node-id]");
    }

    /// <summary>
    /// Blazor prerenders identical-looking dead HTML; clicks before the circuit
    /// attaches vanish. The marker is rendered only from OnAfterRender (interactive).
    /// </summary>
    public static Task WaitForInteractive(this IPage page) =>
        page.WaitForSelectorAsync("[data-interactive]",
            new PageWaitForSelectorOptions { State = WaitForSelectorState.Attached });

    public static ILocator Node(this IPage page, string type, int nth = 0) =>
        page.Locator($".ed-canvas [data-node-type='{type}']").Nth(nth);

    public static async Task<string> NodeId(this ILocator node) =>
        await node.GetAttributeAsync("data-node-id") ?? throw new InvalidOperationException("node id missing");

    /// <summary>
    /// Clicks a node and waits until the selection overlay is outlining THAT node.
    /// <para>The obvious wait — "an outline is visible" — is already satisfied by the PREVIOUS
    /// selection, so it returns instantly and the next action runs against stale state. That is
    /// invisible on an idle machine (the circuit answers in a few ms) and shows up as a flaky
    /// suite the moment the box is busy: measured here, the E2E assembly takes 21s alone and 50s
    /// alongside the rest of the suite, and the failures appear only in the slow runs. A wait that
    /// a prior state can satisfy is not a wait.</para>
    /// </summary>
    public static async Task Select(this IPage page, ILocator node)
    {
        var nodeId = await node.NodeId();
        await node.ClickAsync(new LocatorClickOptions { Position = new Position { X = 8, Y = 8 } });
        // Attached, not Visible. Visible is Playwright's LAYOUT test (a non-empty box), and the
        // overlay is sized by place() from the target's getBoundingClientRect at draw time — so a
        // node measured mid-layout yields a 0×0 overlay that stays 0×0 until something triggers
        // another draw. Waiting on Visible then blocks forever on a selection that in fact
        // succeeded, which is why this failed on the full 30s rather than merely running slow.
        // The contract here is WHICH node is outlined; the attribute says exactly that.
        await page.WaitForSelectorAsync(
            $".ed-ov-selection:not([hidden])[data-node-id='{nodeId}']",
            new PageWaitForSelectorOptions { State = WaitForSelectorState.Attached });
    }

    /// <summary>
    /// Clicks a button that ADDS a row, and waits until the row exists before returning.
    /// <para>The trap this closes: a following <c>nth=-1</c> already matches the row that was last
    /// BEFORE the click, so filling it "works" against the wrong element whenever the circuit has
    /// not rendered the new one yet. Auto-waiting does not help — the locator is satisfied, just by
    /// the wrong thing. Counting is the only way to tell the new state from the old.</para>
    /// </summary>
    public static async Task ClickToAdd(this IPage page, string buttonSelector, string rowSelector)
    {
        var before = await page.Locator(rowSelector).CountAsync();
        await page.ClickAsync(buttonSelector);
        // GREATER than the baseline, not exactly one more. The baseline is read before the click,
        // and if the page is still rendering its existing rows at that moment it is an undercount —
        // the total then overshoots "n + 1" and an equality wait never comes true. Asking only that
        // the count has GROWN still tells the new state from the old, which is the whole job.
        await page.WaitForFunctionAsync(
            "args => document.querySelectorAll(args.sel).length > args.n",
            new { sel = rowSelector, n = before });
    }

    /// <summary>
    /// Clicks a Save button and returns once the change is READABLE AFTER A RELOAD — the durable
    /// fact the test is really about.
    /// <para>What this replaces: waiting for the "… saved." toast. ToastHost fades a success toast
    /// 5s after it renders it, so the signal deletes itself — and the two render batches ("show"
    /// and "dismiss") travel over the same circuit, so a server under load can deliver them
    /// back-to-back and the toast is never in the DOM long enough to be seen at all. The wait then
    /// burns its full timeout on a save that in fact succeeded. Error toasts are exempt (ToastHost
    /// keeps those until dismissed), which is why only the success paths need this.</para>
    /// <para>Reloading is safe: the click is dispatched to the server the moment it happens, and
    /// the command handler runs there regardless of what the browser does next.</para>
    /// </summary>
    public static async Task SaveAndConfirm(this IPage page, string saveButton, Func<Task> persisted)
    {
        await page.ClickAsync(saveButton);

        var deadline = DateTime.UtcNow.AddSeconds(60);
        while (true)
        {
            await page.ReloadAsync();
            await page.WaitForInteractive();
            try
            {
                await persisted();
                return;
            }
            catch (Exception e) when (e is PlaywrightException or TimeoutException)
            {
                // Not there yet: the append may still be in flight. Only give up on the clock.
                if (DateTime.UtcNow > deadline)
                {
                    throw;
                }
            }
        }
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
        // 30s, not 5: the indicator appears after a circuit round trip, and 5s is comfortable
        // on an idle box and marginal when the suite runs alongside everything else.
        await page.WaitForSelectorAsync(".ed-ov-indicator:not([hidden]), .ed-ov-into:not([hidden])");
        await page.Mouse.UpAsync();
    }
}
