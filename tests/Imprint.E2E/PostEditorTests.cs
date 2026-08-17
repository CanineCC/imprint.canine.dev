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

    [Fact]
    public async Task The_posts_index_reads_in_plan_order_not_in_the_order_they_were_written()
    {
        // A content plan has an order that has nothing to do with dates, and the number an author
        // puts at the front of a title is how they express it. Created deliberately out of order,
        // and numerically rather than lexicographically: "(10)" sorts before "(2)" as text.
        var page = await OpenPosts(fixture);
        var run = Guid.NewGuid().ToString("N")[..6];

        foreach (var (number, word) in new[] { (10, "ten"), (2, "two"), (1, "one") })
        {
            await page.FillAsync("[data-testid='new-post-title']", $"({number}) {run} {word}");
            await page.ClickAsync("[data-testid='new-post-create']");
            await page.WaitForURLAsync("**/posts/**");
            await page.WaitForInteractive();
            await page.GoBackAsync();
            await page.WaitForInteractive();
        }

        var mine = page.Locator($"[data-testid='post-list'] .dash-name:has-text('{run}')");
        await Expect(mine).ToHaveCountAsync(3);
        Assert.Equal(
            [$"(1) {run} one", $"(2) {run} two", $"(10) {run} ten"],
            (await mine.AllInnerTextsAsync()).Select(text => text.Trim()).ToArray());
    }

    [Fact]
    public async Task A_site_with_a_reviewer_publishes_through_them_and_the_date_they_set()
    {
        // The workflow as the people live it: an author cannot publish, sends it instead, the
        // reviewer moves the date and approves, and only then can it go out. Auth is off in this
        // fixture, so the one browser plays both parts — which is the "open install" reading the
        // rest of the editor takes, and it still exercises every command and every gate.
        var page = await OpenPosts(fixture);
        var siteId = new Regex(@"/site/([0-9a-f]{32})/posts").Match(page.Url).Groups[1].Value;

        var title = $"Review {Guid.NewGuid():N}"[..13];
        await page.FillAsync("[data-testid='new-post-title']", title);
        await page.ClickAsync("[data-testid='new-post-create']");
        await page.WaitForURLAsync("**/posts/**");
        await page.WaitForInteractive();
        var postUrl = page.Url;
        await page.FillAsync("[data-testid='post-body']", $"# {title}\n\nProse that needs clearing.\n");
        await page.Locator("[data-testid='post-preview'] h1").WaitForAsync();

        // Leaving the page is what flushes the debounced save — asserted here because everything
        // below depends on the body having survived, and because it used not to: a body typed and
        // navigated away from inside the debounce window was silently lost.
        await page.ReloadAsync();
        await page.WaitForInteractive();
        await Expect(page.Locator("[data-testid='post-body']")).ToHaveValueAsync(new Regex("Prose that needs clearing"));

        // ---- name a reviewer in the site's settings
        await page.GotoAsync($"/sites/{siteId}/settings");
        await page.WaitForInteractive();
        await page.FillAsync("[data-testid='site-reviewer-name']", "Lasse");
        await page.FillAsync("[data-testid='site-reviewer-email']", "lasse@example.test");
        await page.ClickAsync("[data-testid='site-reviewer-save']");
        await Expect(page.Locator(".ed-toast").First).ToContainTextAsync("Lasse");

        // ---- back on the post: Publish is gone, because it would only be refused
        await page.GotoAsync(postUrl);
        await page.WaitForInteractive();
        await Expect(page.Locator("[data-testid='post-publish']")).ToHaveCountAsync(0);

        // The author proposes a date and sends it. datetime-local takes the wall clock; the
        // editor reads it in the editorial zone.
        await page.FillAsync("[data-testid='post-publish-at']", "2027-03-01T09:00");
        await page.ClickAsync("[data-testid='post-submit-review']");
        await Expect(page.Locator("[data-testid='post-status']")).ToHaveTextAsync("In review");
        await Expect(page.Locator("[data-testid='post-review-line']")).ToContainTextAsync("Lasse");

        // ---- the reviewer sends it back, and the author sees why
        await page.ClickAsync("[data-testid='post-request-changes']");
        await page.FillAsync("[data-testid='post-sendback-reason']", "Name the customer more carefully.");
        await page.ClickAsync("[data-testid='post-sendback-send']");
        await Expect(page.Locator("[data-testid='post-status']")).ToHaveTextAsync("Changes requested");
        await Expect(page.Locator("[data-testid='post-review-line']")).ToContainTextAsync("Name the customer more carefully.");

        // ---- resubmitted, then approved with a date of the reviewer's own choosing
        await page.ClickAsync("[data-testid='post-submit-review']");
        await Expect(page.Locator("[data-testid='post-status']")).ToHaveTextAsync("In review");
        await page.FillAsync("[data-testid='post-publish-at']", "2027-04-15T08:30");
        await page.ClickAsync("[data-testid='post-approve']");

        // Approved with a FUTURE date: scheduled, and nothing published it in the meantime.
        await Expect(page.Locator("[data-testid='post-status']")).ToHaveTextAsync("Scheduled");
        await Expect(page.Locator("[data-testid='post-review-line']")).ToContainTextAsync("15 Apr 2027, 08:30");

        // ---- and only now is publishing on offer at all
        await Expect(page.Locator("[data-testid='post-publish']")).ToHaveTextAsync("Publish now");
        await page.ClickAsync("[data-testid='post-publish']");
        await Expect(page.Locator("[data-testid='post-status']")).ToHaveTextAsync("Published");

        // Leave the fixture's shared site as it was for every other test in the collection.
        await page.GotoAsync($"/sites/{siteId}/settings");
        await page.WaitForInteractive();
        await page.FillAsync("[data-testid='site-reviewer-email']", "");
        await page.FillAsync("[data-testid='site-reviewer-name']", "");
        await page.ClickAsync("[data-testid='site-reviewer-save']");
        await Expect(page.Locator(".ed-toast").First).ToContainTextAsync("publish directly");
    }

    [Fact]
    public async Task An_unpublished_post_can_be_opened_as_a_real_page_and_the_trail_leads_back()
    {
        // Two complaints, one journey: there was no way to see a post as a PAGE before deciding
        // to publish it, and no way out of the post editor except a lone "← Posts".
        var page = await OpenPosts(fixture);
        var title = $"Trail {Guid.NewGuid():N}"[..12];
        await page.FillAsync("[data-testid='new-post-title']", title);
        await page.ClickAsync("[data-testid='new-post-create']");
        await page.WaitForURLAsync("**/posts/**");
        await page.WaitForInteractive();
        await page.FillAsync("[data-testid='post-body']", $"# {title}\n\nProse nobody has published yet.\n");
        await page.Locator("[data-testid='post-preview'] h1").WaitForAsync();

        // The preview link points at the /preview plane, and the post is still a DRAFT: the
        // status chip says so while the page renders in full site chrome.
        await Expect(page.Locator("[data-testid='post-status']")).ToHaveClassAsync(new Regex(@"\bpost-status-draft\b"));
        var href = await page.Locator("[data-testid='post-preview-link']").GetAttributeAsync("href");
        Assert.Matches(@"^/preview/[0-9a-f]{32}/blog/[a-z0-9-]+/$", href);

        // Polled with real navigations, not a locator wait: the body save is debounced and the
        // preview plane caches each render for a few seconds, so the first hit can legitimately
        // 404 — and a 404 is a page that never changes no matter how long a locator waits on it.
        var previewPage = await page.Context.NewPageAsync();
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (true)
        {
            await previewPage.GotoAsync(fixture.BaseUrl + href);
            if (await previewPage.Locator($"h1:has-text('{title}')").CountAsync() > 0 || DateTime.UtcNow > deadline)
            {
                break;
            }

            await Task.Delay(1000);
        }

        // The real page: the post's own words, inside the published stylesheet's markup — not
        // the editor's node pane. An unpublished post reaching this plane is the whole feature.
        await previewPage.Locator($"h1:has-text('{title}')").WaitForAsync();
        Assert.Contains("Prose nobody has published yet.", await previewPage.Locator("body").InnerTextAsync(), StringComparison.Ordinal);
        Assert.Equal(1, await previewPage.Locator("link[rel='stylesheet']").CountAsync());
        await previewPage.CloseAsync();

        // …and the trail goes back, one step at a time, all the way to the dashboard.
        var crumbs = page.Locator("[data-testid='breadcrumbs']");
        await Expect(crumbs.Locator("[data-testid='breadcrumb-current']")).ToHaveTextAsync(title);
        await crumbs.GetByText("Posts", new LocatorGetByTextOptions { Exact = true }).ClickAsync();
        await page.WaitForURLAsync("**/posts");
        await page.WaitForInteractive();
        await page.Locator("[data-testid='breadcrumbs']").GetByText("Sites", new LocatorGetByTextOptions { Exact = true }).ClickAsync();
        await page.WaitForURLAsync(url => !url.Contains("/posts", StringComparison.Ordinal));
        await Expect(page.Locator("[data-testid='new-site']")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task The_panes_can_be_resized_and_the_split_survives_a_reload()
    {
        var page = await OpenPosts(fixture);
        await page.FillAsync("[data-testid='new-post-title']", $"Split {Guid.NewGuid():N}"[..12]);
        await page.ClickAsync("[data-testid='new-post-create']");
        await page.WaitForURLAsync("**/posts/**");
        await page.WaitForInteractive();

        // The divider is wired by a JS module imported after the first render, so "the element is
        // there" is not "the element works". Waiting for the readiness mark is the difference
        // between a test that passes alone and one that passes on a busy machine.
        await page.Locator(".post-panes[data-split-ready]").WaitForAsync();

        // The split is remembered per browser, so a previous test in this context may have left it
        // anywhere. Recentre first: this test is about the drag, not about where it started.
        var handle = page.Locator("[data-testid='post-split']");
        await handle.DblClickAsync();

        var source = page.Locator("[data-testid='post-body']");
        var before = (await source.BoundingBoxAsync())!.Width;

        // Dragged with the pointer, the way a person does it — the drag is what the feature IS,
        // and setting the CSS variable directly would test nothing but CSS.
        var grip = (await handle.BoundingBoxAsync())!;
        await page.Mouse.MoveAsync(grip.X + grip.Width / 2, grip.Y + grip.Height / 2);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(grip.X + grip.Width / 2 - 200, grip.Y + grip.Height / 2, new MouseMoveOptions { Steps = 8 });
        await page.Mouse.UpAsync();

        var after = (await source.BoundingBoxAsync())!.Width;
        Assert.True(after < before - 100, $"the markdown pane should have narrowed: {before} -> {after}");

        // …and it is remembered. An author who widened the markdown wants it wide next time, so
        // the split is stored rather than reset to half on every navigation.
        await page.ReloadAsync();
        await page.WaitForInteractive();
        await page.Locator(".post-panes[data-split-ready]").WaitForAsync();
        var restored = (await page.Locator("[data-testid='post-body']").BoundingBoxAsync())!.Width;
        Assert.True(Math.Abs(restored - after) < 20, $"the split should have been restored: {after} -> {restored}");

        // Double-click evens them up, which is what a divider does everywhere else.
        await handle.DblClickAsync();
        var evened = (await page.Locator("[data-testid='post-body']").BoundingBoxAsync())!.Width;
        Assert.True(evened > restored + 100, $"a double-click should recentre: {restored} -> {evened}");
    }

    [Fact]
    public async Task A_post_longer_than_the_preview_pane_can_be_scrolled()
    {
        // The preview element carries `inert`, and an inert subtree is not hit-tested — so when it
        // was itself the scroll container, a wheel over it scrolled nothing and a long post could
        // only ever be read from the top. Asserted by actually scrolling and reading scrollTop
        // back: "the CSS says overflow:auto" would have been true of the broken version too.
        var page = await OpenPosts(fixture);
        await page.FillAsync("[data-testid='new-post-title']", $"Long {Guid.NewGuid():N}"[..12]);
        await page.ClickAsync("[data-testid='new-post-create']");
        await page.WaitForURLAsync("**/posts/**");
        await page.WaitForInteractive();

        var body = string.Join("\n\n", Enumerable.Range(1, 60).Select(i => $"Paragraph {i} of a post that is taller than any pane."));
        await page.FillAsync("[data-testid='post-body']", body);
        await page.Locator("[data-testid='post-preview'] p").Last.WaitForAsync();

        var pane = page.Locator("[data-testid='post-view-body']");
        Assert.True(
            await pane.EvaluateAsync<int>("el => el.scrollHeight - el.clientHeight") > 0,
            "the preview pane should overflow for a 60-paragraph post");

        await pane.EvaluateAsync("el => el.scrollTo(0, 400)");
        Assert.True(await pane.EvaluateAsync<int>("el => el.scrollTop") > 0, "the preview pane did not scroll");

        // The mouse wheel over the preview must reach that scroller too — the failure was a wheel
        // landing on an inert element, which scrollTo() alone would not have caught. Hovered via
        // the wrapper because hit-testing over an inert subtree lands there by definition: that
        // redirection IS the fix, and Playwright refuses to hover the inert element for the same
        // reason ("post-view-body intercepts pointer events").
        await pane.EvaluateAsync("el => el.scrollTo(0, 0)");
        await pane.HoverAsync();
        await page.Mouse.WheelAsync(0, 600);
        await Expect(pane).Not.ToHaveJSPropertyAsync("scrollTop", 0);
    }

    [Fact]
    public async Task An_upload_is_tagged_with_the_post_by_default_and_its_tags_are_editable_on_the_tile()
    {
        // The complaint this answers, in order: the markdown never leaves the screen; an upload
        // is never unfiled, without the author being asked anything; the tags are ON the tile,
        // so adding and removing one are visible affordances rather than a discovery problem.
        var page = await OpenPosts(fixture);
        var title = $"Tagged {Guid.NewGuid():N}"[..14];
        await page.FillAsync("[data-testid='new-post-title']", title);
        await page.ClickAsync("[data-testid='new-post-create']");
        await page.WaitForURLAsync("**/posts/**");
        await page.WaitForInteractive();
        await page.FillAsync("[data-testid='post-body']", "# Tagged\n\nProse above the figure.\n");

        await page.ClickAsync("[data-testid='post-view-media']");
        await Expect(page.Locator("[data-testid='post-body']")).ToBeVisibleAsync();
        await Expect(page.Locator("[data-testid='post-preview']")).ToHaveCountAsync(0);

        // The post's own name is what the next upload is filed under, and the field says so
        // before anything is dropped — the author never has to know the rule to benefit from it.
        await Expect(page.Locator("[data-testid='post-tag-choose']")).ToHaveAttributeAsync("placeholder", title);

        var tiles = page.Locator("[data-testid='post-media-tile']");
        await page.SetInputFilesAsync("[data-testid='post-media-input']", await WriteSvg("#0e7c6b"));

        // Tagged with the post, with nothing asked of the author…
        await Expect(page.Locator($"[data-testid='post-tag'][data-tag='{title}']")).ToHaveTextAsync($"{title} (1)");
        await Expect(page.Locator("[data-testid='post-body']"))
            .ToHaveValueAsync(new Regex(@"!\[[^\]]*\]\(media:[0-9a-fA-F]{32}\)"));

        // …and the group is one click away, which is the point of filing it at all.
        await page.ClickAsync($"[data-testid='post-tag'][data-tag='{title}']");
        await Expect(tiles).ToHaveCountAsync(1);

        // The picture is in the post already, and the preview — one tab away, with the markdown
        // still on screen — shows the graphic itself.
        await page.ClickAsync("[data-testid='post-view-preview']");
        await page.Locator("[data-testid='post-preview'] .ip-svg svg").WaitForAsync();
        await page.ClickAsync("[data-testid='post-view-media']");

        // A second tag, added from the tile: the button says "+ Tag" and the input is right there.
        var second = $"Portraits-{Guid.NewGuid():N}"[..16];
        await page.ClickAsync("[data-testid='post-tag-add-open']");
        await page.FillAsync("[data-testid='post-tag-add']", second);
        await page.ClickAsync("[data-testid='post-tag-add-go']");
        await Expect(page.Locator($"[data-testid='post-tag'][data-tag='{second}']")).ToHaveTextAsync($"{second} (1)");
        await Expect(page.Locator($".post-media-tags .post-tag-chip[data-tag='{second}']")).ToHaveCountAsync(1);

        // Typing over the upload field files the NEXT upload elsewhere, and only the next one.
        var chosen = $"Blog-entry-{Guid.NewGuid():N}"[..18];
        await page.FillAsync("[data-testid='post-tag-choose']", chosen);
        await page.SetInputFilesAsync("[data-testid='post-media-input']", await WriteSvg("#8a5a00"));
        await Expect(page.Locator($"[data-testid='post-tag'][data-tag='{chosen}']")).ToHaveTextAsync($"{chosen} (1)");
        await Expect(page.Locator($"[data-testid='post-tag'][data-tag='{title}']")).ToHaveTextAsync($"{title} (1)");

        // Removing a tag is the × on the chip, on the tile, without opening anything first —
        // and the group ceases to exist, because a tag is only a label something carries.
        await page.ClickAsync($"[data-testid='post-tag'][data-tag='{second}']");
        await Expect(tiles).ToHaveCountAsync(1);
        await page.ClickAsync($"[data-testid='post-tag-remove'][data-tag='{second}']");
        await Expect(page.Locator($"[data-testid='post-tag'][data-tag='{second}']")).ToHaveCountAsync(0);
        await Expect(tiles).ToHaveCountAsync(0);
        await Expect(page.Locator("[data-testid='post-media-empty']")).ToBeVisibleAsync();
    }

    private static async Task<string> WriteSvg(string fill)
    {
        var path = Path.Combine(Path.GetTempPath(), $"e2e-{Guid.NewGuid():N}"[..12] + ".svg");
        await File.WriteAllTextAsync(path,
            $"""<svg viewBox="0 0 120 60" xmlns="http://www.w3.org/2000/svg"><rect x="4" y="4" width="112" height="52" fill="{fill}"/></svg>""");
        return path;
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
