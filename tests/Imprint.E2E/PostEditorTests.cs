using System.Text.RegularExpressions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Imprint.E2E;

/// <summary>
/// The blog module end to end, driven like a person: create a post, type markdown, watch the
/// preview become the thing it will publish as, publish it, and find the static page, the index
/// and the feed on disk.
///
/// <para>The assertion that carries the design is the preview one. Everything else could be true
/// of a client-side markdown pane; only "the preview contains the same rendered nodes the
/// published page does" says the two came from one renderer.</para>
/// </summary>
[Collection("editor")]
public sealed class PostEditorTests(EditorFixture fixture)
{
    private const string Body = """
        # Hello from the editor

        A paragraph with **bold** text and a [link](https://example.test/).

        - first
        - second

        ```sh
        echo "hello" > file.txt
        ```
        """;

    [Fact]
    public async Task Write_markdown_preview_it_publish_it_and_find_it_on_disk()
    {
        var page = await OpenPosts(fixture);

        var title = $"E2E post {Guid.NewGuid():N}"[..14];
        await page.FillAsync("[data-testid='new-post-title']", title);
        await page.ClickAsync("[data-testid='new-post-create']");
        await page.WaitForURLAsync("**/posts/**");
        await page.WaitForInteractive();

        // ---- type markdown; the preview is rendered server-side from the same converter
        await page.FillAsync("[data-testid='post-body']", Body);

        var preview = page.Locator("[data-testid='post-preview']");
        await preview.Locator("h1:has-text('Hello from the editor')").WaitForAsync(
            );

        // Every construct, in the preview, as REAL nodes — not a markdown library's opinion.
        Assert.Equal(1, await preview.Locator("strong:has-text('bold')").CountAsync());
        Assert.Equal(1, await preview.Locator("a[href='https://example.test/']").CountAsync());
        Assert.Equal(2, await preview.Locator("li").CountAsync());
        Assert.Equal(1, await preview.Locator("pre.ip-code").CountAsync());
        // The code block is CONTENT: the quotes and the > are visible characters, not markup.
        Assert.Contains("echo \"hello\" > file.txt", await preview.Locator("pre.ip-code").InnerTextAsync(), StringComparison.Ordinal);

        // ---- publish
        await page.ClickAsync("[data-testid='post-publish']");
        await page.WaitForSelectorAsync("[data-testid='post-status']:has-text('Published')");

        // ---- the static page appears, with the same rendered nodes the preview showed
        var slug = SlugOf(title);
        var postPath = Path.Combine(fixture.PublishDirectory, "blog", slug, "index.html");
        await WaitForFile(postPath + ".br");

        var html = await File.ReadAllTextAsync(postPath);
        Assert.Contains("Hello from the editor", html, StringComparison.Ordinal);
        Assert.Contains("<strong>bold</strong>", html, StringComparison.Ordinal);
        Assert.Contains("class=\"ip-code\"", html, StringComparison.Ordinal);
        Assert.Contains("echo &quot;hello&quot; &gt; file.txt", html, StringComparison.Ordinal);
        // Published markup carries no editor residue — the same contract every page has.
        Assert.DoesNotContain("data-node-id", html, StringComparison.Ordinal);

        // ---- the index and the feed are generated from the same published set
        var indexPath = Path.Combine(fixture.PublishDirectory, "blog", "index.html");
        await WaitForFile(indexPath);
        Assert.Contains(title, await File.ReadAllTextAsync(indexPath), StringComparison.Ordinal);

        var feedPath = Path.Combine(fixture.PublishDirectory, "feed.xml");
        await WaitForFile(feedPath);
        var feed = await File.ReadAllTextAsync(feedPath);
        Assert.Contains("<rss version=\"2.0\">", feed, StringComparison.Ordinal);
        Assert.Contains(title, feed, StringComparison.Ordinal);
        Assert.Contains($"blog/{slug}/", feed, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_body_that_cannot_convert_is_reported_and_blocks_publishing()
    {
        var page = await OpenPosts(fixture);
        await page.FillAsync("[data-testid='new-post-title']", $"Broken {Guid.NewGuid():N}"[..12]);
        await page.ClickAsync("[data-testid='new-post-create']");
        await page.WaitForURLAsync("**/posts/**");
        await page.WaitForInteractive();

        await page.FillAsync("[data-testid='post-body']", "Fine paragraph.\n\n> a blockquote\n");

        // The author is told at the line, while typing — not at the moment they wanted to finish.
        var problems = page.Locator("[data-testid='post-problems']");
        await problems.WaitForAsync();
        Assert.Contains("Line 3", await problems.InnerTextAsync(), StringComparison.OrdinalIgnoreCase);

        // …and the gate holds. Proving that is harder than it looks: "still a draft" is also true
        // BEFORE the click, so on its own it would pass even if the Publish button did nothing at
        // all. Waiting for the error toast does not fix that either — it says a refusal was
        // reported, not that publishing works when the body is fine.
        //
        // So the test does the one thing that separates "refused" from "broken": press Publish on
        // the bad body, then FIX the body and press it again. The first must not publish and the
        // second must, which can only both hold if the gate is what stopped it.
        // Asserted on the CLASS, not the text: the chip's label is "Draft" but CSS uppercases it,
        // so the visible string depends on where you read it from, while the class is built by
        // ToLowerInvariant() and says exactly one thing.
        await page.ClickAsync("[data-testid='post-publish']");
        await Expect(page.Locator("[data-testid='post-status']")).ToHaveClassAsync(
            new Regex(@"\bpost-status-draft\b"));

        await page.FillAsync("[data-testid='post-body']", "Fine paragraph.\n\nAnd another one.\n");
        await Expect(page.Locator("[data-testid='post-problems']")).ToHaveCountAsync(0);
        await page.ClickAsync("[data-testid='post-publish']");
        await Expect(page.Locator("[data-testid='post-status']")).ToHaveClassAsync(
            new Regex(@"\bpost-status-published\b"));
    }

    [Fact]
    public async Task A_widget_directive_becomes_a_live_island_in_the_preview()
    {
        var page = await OpenPosts(fixture);
        await page.FillAsync("[data-testid='new-post-title']", $"Widget {Guid.NewGuid():N}"[..12]);
        await page.ClickAsync("[data-testid='new-post-create']");
        await page.WaitForURLAsync("**/posts/**");
        await page.WaitForInteractive();

        await page.FillAsync("[data-testid='post-body']", "Before.\n\n::: widget x-theme-toggle :::\n\nAfter.");

        var preview = page.Locator("[data-testid='post-preview']");
        await preview.Locator("[data-node-type='widget']").WaitForAsync(
            );
        Assert.Equal(2, await preview.Locator("p").CountAsync());
    }

    /// <summary>Reaches the posts index the way a person does — from the site card on the
    /// dashboard — which also keeps the test honest that the feature is navigable at all.</summary>
    private static async Task<IPage> OpenPosts(EditorFixture fixture)
    {
        var page = await fixture.OpenEditor();   // ensures a site exists (onboarding on a fresh dir)
        await page.GotoAsync("/");
        await page.WaitForInteractive();
        await page.ClickAsync("[data-testid='site-posts']");
        await page.WaitForURLAsync("**/posts");
        await page.WaitForInteractive();
        return page;
    }

    /// <summary>The slug the index derives from a title — mirrors Slug.Suggest for the assert.</summary>
    private static string SlugOf(string title) =>
        new(title.ToLowerInvariant().Select(c => char.IsAsciiLetterOrDigit(c) ? c : '-').ToArray());

    private static async Task WaitForFile(string path)
    {
        // The publisher debounces (~2s) and writes a page before its precompressed sibling, so a
        // poll that stops at the first file can catch a pass mid-window.
        var deadline = DateTime.UtcNow.AddSeconds(60);
        while (!File.Exists(path) && DateTime.UtcNow < deadline)
        {
            await Task.Delay(500);
        }

        Assert.True(File.Exists(path), $"publisher did not write {path}");
    }
}
