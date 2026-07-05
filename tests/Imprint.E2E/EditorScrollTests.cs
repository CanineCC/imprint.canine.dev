using Microsoft.Playwright;

namespace Imprint.E2E;

/// <summary>
/// The editor frame owns the viewport: the document itself must never scroll (scrolling
/// past the app into the page background), and wheel input over the canvas must scroll
/// the canvas viewport (.ed-canvas-scroll) when the page preview is taller than it.
/// </summary>
[Collection("editor")]
public sealed class EditorScrollTests(EditorFixture fixture)
{
    [Fact]
    public async Task The_document_does_not_scroll_and_the_canvas_does()
    {
        var page = await fixture.OpenEditor();

        var metrics = await page.EvaluateAsync<int[]>(
            """
            () => [
                document.documentElement.scrollHeight,
                window.innerHeight,
                document.querySelector('.ed-canvas-scroll').scrollHeight,
                document.querySelector('.ed-canvas-scroll').clientHeight,
            ]
            """);
        var (docScroll, viewport, canvasScroll, canvasClient) =
            (metrics[0], metrics[1], metrics[2], metrics[3]);

        Assert.True(docScroll <= viewport + 1,
            $"The document scrolls ({docScroll}px content in a {viewport}px viewport) — the frame must own the height.");
        Assert.True(canvasScroll > canvasClient,
            $"Template page should overflow the canvas viewport ({canvasScroll} <= {canvasClient}) — cannot test scrolling.");

        // Wheel over the canvas scrolls the canvas viewport, not the document.
        await page.Locator(".ed-canvas-scroll").HoverAsync();
        await page.Mouse.WheelAsync(0, 600);
        await page.WaitForTimeoutAsync(200);

        var after = await page.EvaluateAsync<int[]>(
            "() => [Math.round(document.querySelector('.ed-canvas-scroll').scrollTop), Math.round(window.scrollY)]");
        Assert.True(after[0] > 0, "Wheel over the canvas did not scroll .ed-canvas-scroll.");
        Assert.Equal(0, after[1]);
    }
}
