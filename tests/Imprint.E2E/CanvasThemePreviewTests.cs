using Microsoft.Playwright;

namespace Imprint.E2E;

/// <summary>
/// The editor's "preview site dark mode" toggle for light/dark IMAGE variants. Two
/// halves of the fix are verified against the real page: (1) the ☀/☾ button drives
/// data-theme on .ed-canvas, and (2) the canvas CSS maps that attribute to which
/// rendition is visible. The renditions an image emits (ip-img-light / ip-img-dark)
/// are covered by the ImageView unit tests; here they are injected so the test needs no
/// upload/transcode, and only the canvas wiring is under test.
/// </summary>
[Collection("editor")]
public sealed class CanvasThemePreviewTests(EditorFixture fixture)
{
    [Fact]
    public async Task Dark_toggle_and_canvas_css_flip_the_visible_image_rendition()
    {
        var page = await fixture.OpenEditor();
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = ColorScheme.Light });
        var canvas = page.Locator(".ed-canvas");
        await canvas.WaitForAsync();

        // --- half 1: the real button drives .ed-canvas[data-theme] ---
        Assert.Equal("light", await canvas.GetAttributeAsync("data-theme"));
        var toggle = page.Locator("button[aria-label='Toggle canvas dark mode']");
        await toggle.ClickAsync();
        await page.WaitForSelectorAsync(".ed-canvas[data-theme='dark']");
        await toggle.ClickAsync(); // back to light for a clean CSS baseline
        await page.WaitForSelectorAsync(".ed-canvas[data-theme='light']");

        // --- half 2: the canvas CSS maps data-theme to rendition visibility ---
        // Inject the two <img>s an image node with a dark variant emits (no Blazor
        // re-render happens after this, so the injected nodes persist through the checks).
        await page.EvaluateAsync(@"() => {
            const c = document.querySelector('.ed-canvas');
            for (const cls of ['ip-img-light', 'ip-img-dark']) {
                const i = document.createElement('img');
                i.className = 'ip-img ' + cls;
                i.id = 'e2e-' + cls;
                c.appendChild(i);
            }
        }");

        async Task<string> Display(string id) =>
            await page.EvaluateAsync<string>($"() => getComputedStyle(document.getElementById('{id}')).display");

        // Light preview: light rendition shown, dark hidden.
        await SetCanvasTheme(page, "light");
        Assert.NotEqual("none", await Display("e2e-ip-img-light"));
        Assert.Equal("none", await Display("e2e-ip-img-dark"));

        // Dark preview: the flip. Without the fix the toggle did nothing here, because the
        // shared rules key off :root[data-theme], which the editor page never sets.
        await SetCanvasTheme(page, "dark");
        Assert.Equal("none", await Display("e2e-ip-img-light"));
        Assert.NotEqual("none", await Display("e2e-ip-img-dark"));
    }

    private static Task SetCanvasTheme(IPage page, string theme) =>
        page.EvaluateAsync($"() => document.querySelector('.ed-canvas').setAttribute('data-theme', '{theme}')");
}
