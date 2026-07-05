using Microsoft.Playwright;

namespace Imprint.E2E;

/// <summary>
/// The full loop, driven like a human: onboarding → templated site → canvas renders
/// the domain tree → publish → static files appear with the delivery contract intact.
/// </summary>
[Collection("editor")]
public sealed class WalkingSkeletonTests(EditorFixture fixture)
{
    [Fact]
    public async Task Onboarding_to_published_static_site()
    {
        var page = await fixture.OpenEditor();
        var sections = await page.Locator(".ed-canvas [data-node-type='section']").CountAsync();
        Assert.True(sections >= 4, $"expected the Launch template's sections, found {sections}");

        // ---- selection: click a heading → breadcrumb shows the path
        await page.ClickAsync(".ed-canvas [data-node-type='heading'] >> nth=0");
        await page.WaitForSelectorAsync(".ed-crumb-current");

        // ---- publish everything
        await page.ClickAsync("button:has-text('Publish')");
        await page.WaitForSelectorAsync("text=Published", new PageWaitForSelectorOptions { Timeout = 15_000 });

        // ---- the file-system projection materializes (publisher debounce ≈ 2s).
        // Wait for the precompressed sibling, not just index.html: the pass writes the
        // page first and its .br ~100ms later (404 + css are brotli'd in between), so a
        // poll that stops at index.html can catch the pass mid-window and flake on the
        // sibling asserts below. The .br is written after its page, so once it exists
        // the html next to it is the fresh render.
        var indexPath = Path.Combine(fixture.PublishDirectory, "index.html");
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (!(File.Exists(indexPath) && File.Exists(indexPath + ".br")) && DateTime.UtcNow < deadline)
        {
            await Task.Delay(500);
        }

        Assert.True(File.Exists(indexPath), "publisher did not write index.html");
        var html = await File.ReadAllTextAsync(indexPath);
        Assert.Contains("<html lang=\"en\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data-node-id", html, StringComparison.Ordinal);
        Assert.Contains("css/site.", html, StringComparison.Ordinal);
        Assert.Contains("Skeleton Works", html, StringComparison.Ordinal);

        // Sibling artifacts of the delivery contract.
        Assert.True(File.Exists(indexPath + ".br"), "missing precompressed sibling");
        Assert.True(File.Exists(Path.Combine(fixture.PublishDirectory, "sitemap.xml")));
        Assert.True(File.Exists(Path.Combine(fixture.PublishDirectory, "publish-manifest.json")));
    }
}
