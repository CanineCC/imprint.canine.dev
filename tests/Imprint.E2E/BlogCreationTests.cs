using System.Text.RegularExpressions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Imprint.E2E;

/// <summary>
/// Creating a blog, driven the way a person does it: find "New blog" on the dashboard,
/// name it, give it an address, and land somewhere you can write.
///
/// <para>The assertion that carries the design is the last one — the post is published
/// into the BLOG's own folder, at the origin the author typed on the creation form. That
/// is the whole claim of a blog being a site kind rather than a section of some other
/// site: it has its own address, and the address is a thing you say once, at the moment
/// you are thinking about it.</para>
/// </summary>
[Collection("editor")]
public sealed class BlogCreationTests(EditorFixture fixture)
{
    private const string Origin = "https://blog.e2e.test";

    [Fact]
    public async Task Create_a_blog_with_an_address_write_a_post_and_find_it_published_there()
    {
        var page = await OpenDashboard(fixture);
        var folder = Path.Combine(fixture.DataDirectory, $"blog-{Guid.NewGuid():N}"[..14]);

        // ---- the entry point exists where sites are created, not hidden on a card
        await page.ClickAsync("[data-testid='new-blog']");
        await page.WaitForURLAsync("**/blogs/new");
        await page.WaitForInteractive();

        var name = $"E2E blog {Guid.NewGuid():N}"[..14];
        await page.FillAsync("[data-testid='new-blog-name']", name);
        await page.FillAsync("[data-testid='new-blog-url']", Origin);
        await page.FillAsync("[data-testid='new-blog-folder']", folder);
        await page.ClickAsync("[data-testid='new-blog-create']");

        // ---- a new blog opens on its posts: there is no page tree to land in
        await page.WaitForURLAsync("**/posts");
        await page.WaitForInteractive();
        await page.WaitForSelectorAsync("[data-testid='posts-empty']");

        // ---- write and publish
        var title = $"First post {Guid.NewGuid():N}"[..16];
        await page.FillAsync("[data-testid='new-post-title']", title);
        await page.ClickAsync("[data-testid='new-post-create']");
        await page.WaitForURLAsync("**/posts/**");
        await page.WaitForInteractive();

        await page.FillAsync("[data-testid='post-body']", "Hello from a blog of its own.\n");
        await page.ClickAsync("[data-testid='post-publish']");
        await page.WaitForSelectorAsync("[data-testid='post-status']:has-text('Published')");

        // ---- published into THIS blog's folder, not the app's default output
        // No "blog" segment: on a site whose KIND is Blog, the blog IS the site, so the index is
        // its root and the posts sit directly beneath it (d084461).
        var slug = SlugOf(title);
        var postPath = Path.Combine(folder, slug, "index.html");
        await WaitForFile(postPath);

        var html = await File.ReadAllTextAsync(postPath);
        Assert.Contains("Hello from a blog of its own.", html, StringComparison.Ordinal);

        // The origin typed on the creation form is the one the published page is written
        // against — proof the environment made at creation is the one publishing uses.
        Assert.Contains(Origin, html, StringComparison.Ordinal);

        // ---- the index and the feed, in the same folder
        var indexPath = Path.Combine(folder, "index.html");
        await WaitForFile(indexPath);
        Assert.Contains(title, await File.ReadAllTextAsync(indexPath), StringComparison.Ordinal);

        var feedPath = Path.Combine(folder, "feed.xml");
        await WaitForFile(feedPath);
        var feed = await File.ReadAllTextAsync(feedPath);
        Assert.Contains(title, feed, StringComparison.Ordinal);
        Assert.Contains($"/{slug}/", feed, StringComparison.Ordinal);
        Assert.DoesNotContain($"/blog/{slug}/", feed, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_blog_sits_on_the_blogs_shelf_and_its_card_opens_its_posts()
    {
        var page = await OpenDashboard(fixture);

        var name = $"Shelf blog {Guid.NewGuid():N}"[..16];
        await page.ClickAsync("[data-testid='new-blog']");
        await page.WaitForURLAsync("**/blogs/new");
        await page.WaitForInteractive();
        await page.FillAsync("[data-testid='new-blog-name']", name);
        await page.ClickAsync("[data-testid='new-blog-create']");
        await page.WaitForURLAsync("**/posts");
        await page.WaitForInteractive();

        await page.GotoAsync("/");
        await page.WaitForInteractive();

        // On the Blogs shelf, and NOT among the sites: the two collections are separate,
        // which is the point of the kind.
        var onBlogShelf = page.Locator("ul[aria-label='Blogs'] .dash-card", new() { HasTextString = name });
        Assert.Equal(1, await onBlogShelf.CountAsync());
        Assert.Equal(0, await page.Locator("ul[aria-label='Sites'] .dash-card", new() { HasTextString = name }).CountAsync());

        // A blog with no address says so rather than pretending to be published.
        Assert.Equal(1, await onBlogShelf.Locator(".dash-env.is-none").CountAsync());

        await onBlogShelf.Locator(".dash-open").ClickAsync();
        await page.WaitForURLAsync("**/posts");
        await page.WaitForInteractive();
        await page.WaitForSelectorAsync("[data-testid='posts-empty']");
    }

    [Fact]
    public async Task An_svg_dropped_into_a_post_is_referenced_previewed_and_published()
    {
        // The test the first pass should have been. Publishing "Hello from a blog of its
        // own." proved a path and nothing about a post anyone would write: an SVG uploads as
        // a VECTOR with no raster variants, which the image view treated as unpublishable and
        // rendered as nothing — silently, on a post that reported success.
        var page = await OpenDashboard(fixture);
        var folder = Path.Combine(fixture.DataDirectory, $"svg-{Guid.NewGuid():N}"[..12]);

        await page.ClickAsync("[data-testid='new-blog']");
        await page.WaitForURLAsync("**/blogs/new");
        await page.WaitForInteractive();
        await page.FillAsync("[data-testid='new-blog-name']", $"Figures {Guid.NewGuid():N}"[..14]);
        await page.FillAsync("[data-testid='new-blog-folder']", folder);
        await page.ClickAsync("[data-testid='new-blog-create']");
        await page.WaitForURLAsync("**/posts");
        await page.WaitForInteractive();

        var title = $"Figured {Guid.NewGuid():N}"[..14];
        await page.FillAsync("[data-testid='new-post-title']", title);
        await page.ClickAsync("[data-testid='new-post-create']");
        await page.WaitForURLAsync("**/posts/**");
        await page.WaitForInteractive();
        await page.FillAsync("[data-testid='post-body']", "# Figured\n\nThe prose above the figure.\n");

        // Uploaded through the markdown pane's own drop input — the same element file-drop.js
        // hands a drop to, so this exercises the drop path without synthesising a DataTransfer.
        // It is the pane the author is looking at, and it works whichever view the right pane
        // happens to be showing.
        var svgPath = Path.Combine(Path.GetTempPath(), $"e2e-{Guid.NewGuid():N}"[..12] + ".svg");
        await File.WriteAllTextAsync(svgPath,
            """<svg viewBox="0 0 120 60" xmlns="http://www.w3.org/2000/svg"><rect x="4" y="4" width="112" height="52" fill="#0e7c6b"/></svg>""");
        await page.SetInputFilesAsync("[data-testid='post-source-drop']", svgPath);

        // It lands in the BODY — the author does not go hunting for an id, which is the whole
        // reason the shelf exists.
        await Expect(page.Locator("[data-testid='post-body']"))
            .ToHaveValueAsync(new Regex(@"!\[[^\]]*\]\(media:[0-9a-fA-F]{32}\)"));
        var body = await page.InputValueAsync("[data-testid='post-body']");
        Assert.Contains("The prose above the figure.", body, StringComparison.Ordinal);

        // The preview shows the graphic itself, not a placeholder standing in for one.
        await page.Locator("[data-testid='post-preview'] .ip-svg svg").WaitForAsync();

        // …and it is on the shelf, which is now a view of the right pane rather than a slab
        // under the text: the markdown stays on screen while you look at the library.
        await page.ClickAsync("[data-testid='post-view-media']");
        await page.WaitForSelectorAsync("[data-testid='post-media-tile']");
        await Expect(page.Locator("[data-testid='post-body']")).ToBeVisibleAsync();
        await page.ClickAsync("[data-testid='post-view-preview']");

        await page.ClickAsync("[data-testid='post-publish']");
        await page.WaitForSelectorAsync("[data-testid='post-status']:has-text('Published')");

        // And the published file carries the drawing. This is the assertion that matters:
        // the old behaviour published a post with the figure simply absent.
        var postPath = Path.Combine(folder, SlugOf(title), "index.html");
        await WaitForFile(postPath);
        var html = await File.ReadAllTextAsync(postPath);
        Assert.Contains("The prose above the figure.", html, StringComparison.Ordinal);
        Assert.Contains("<svg", html, StringComparison.Ordinal);
        Assert.Contains("#0e7c6b", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task On_a_blog_site_the_preview_link_points_at_the_root_not_at_a_blog_prefix()
    {
        // The link 404'd on the only site that mattered. A blog site's posts moved to its root —
        // blog.canine.dev/a-post rather than blog.canine.dev/blog/a-post — and the editor's
        // preview href still hard-coded the prefix, so the one button whose whole job is "show me
        // this before I publish it" led to a not-found page.
        var page = await OpenDashboard(fixture);
        var folder = Path.Combine(fixture.DataDirectory, $"root-{Guid.NewGuid():N}"[..12]);

        await page.ClickAsync("[data-testid='new-blog']");
        await page.WaitForURLAsync("**/blogs/new");
        await page.WaitForInteractive();
        await page.FillAsync("[data-testid='new-blog-name']", $"Rooted {Guid.NewGuid():N}"[..14]);
        await page.FillAsync("[data-testid='new-blog-folder']", folder);
        await page.ClickAsync("[data-testid='new-blog-create']");
        await page.WaitForURLAsync("**/posts");
        await page.WaitForInteractive();

        var title = $"Rooted {Guid.NewGuid():N}"[..13];
        await page.FillAsync("[data-testid='new-post-title']", title);
        await page.ClickAsync("[data-testid='new-post-create']");
        await page.WaitForURLAsync("**/posts/**");
        await page.WaitForInteractive();
        await page.FillAsync("[data-testid='post-body']", $"# {title}\n\nAt the root of its own site.\n");
        await page.Locator("[data-testid='post-preview'] h1").WaitForAsync();

        // No /blog/ segment: on a blog site the post IS at the root.
        var href = await page.Locator("[data-testid='post-preview-link']").GetAttributeAsync("href");
        Assert.Matches(@"^/preview/[0-9a-f]{32}/[a-z0-9-]+/$", href);
        Assert.DoesNotContain("/blog/", href, StringComparison.Ordinal);

        // …and it resolves. Polled with real navigations: the body save is debounced and the
        // preview plane caches, so the first hit can legitimately 404 — which is exactly the
        // failure being tested, and a 404 never changes no matter how long a locator waits.
        var preview = await page.Context.NewPageAsync();
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (true)
        {
            await preview.GotoAsync(fixture.BaseUrl + href);
            if (await preview.Locator($"h1:has-text('{title}')").CountAsync() > 0 || DateTime.UtcNow > deadline)
            {
                break;
            }

            await Task.Delay(1000);
        }

        await preview.Locator($"h1:has-text('{title}')").WaitForAsync();
        await preview.CloseAsync();
    }

    [Fact]
    public async Task A_blog_has_the_same_settings_surface_a_site_has()
    {
        var page = await OpenDashboard(fixture);
        var folder = Path.Combine(fixture.DataDirectory, $"settings-{Guid.NewGuid():N}"[..18]);

        var name = $"Settings blog {Guid.NewGuid():N}"[..18];
        await page.ClickAsync("[data-testid='new-blog']");
        await page.WaitForURLAsync("**/blogs/new");
        await page.WaitForInteractive();
        await page.FillAsync("[data-testid='new-blog-name']", name);
        await page.FillAsync("[data-testid='new-blog-url']", Origin);
        await page.FillAsync("[data-testid='new-blog-folder']", folder);
        await page.ClickAsync("[data-testid='new-blog-create']");
        await page.WaitForURLAsync("**/posts");
        await page.WaitForInteractive();

        // Reached the way an author reaches it: the gear on the blog's card.
        await page.GotoAsync("/");
        await page.WaitForInteractive();
        var card = page.Locator("ul[aria-label='Blogs'] .dash-card", new() { HasTextString = name });
        await card.Locator(".dash-gear").ClickAsync();
        await page.WaitForURLAsync("**/settings");
        await page.WaitForInteractive();

        // The address and folder given at creation are here, in the same fields a site's
        // are — one settings surface, not a second one written for blogs.
        await Expect(page.Locator("input[aria-label='Publish folder']")).ToHaveValueAsync(folder);
        await Expect(page.Locator("input[aria-label='Site address']")).ToHaveValueAsync(Origin);

        // The sections the kind does not change are all present.
        Assert.Equal(1, await page.Locator("h2:has-text('Languages')").CountAsync());
        Assert.Equal(1, await page.Locator("h2:has-text('People')").CountAsync());
        Assert.Equal(1, await page.Locator("h2:has-text('Publish & promote')").CountAsync());

        // And the way back in leads to posts. Before kinds existed this button fell through
        // to a page lookup, found none, and linked to the settings page it is drawn on.
        await page.ClickAsync("[data-testid='settings-open']");
        await page.WaitForURLAsync("**/posts");
        await page.WaitForInteractive();
        await page.WaitForSelectorAsync("[data-testid='posts-empty']");
    }

    [Fact]
    public async Task An_address_without_a_publish_folder_is_refused_and_creates_nothing()
    {
        var page = await OpenDashboard(fixture);

        var name = $"Refused {Guid.NewGuid():N}"[..14];
        await page.ClickAsync("[data-testid='new-blog']");
        await page.WaitForURLAsync("**/blogs/new");
        await page.WaitForInteractive();
        await page.FillAsync("[data-testid='new-blog-name']", name);
        await page.FillAsync("[data-testid='new-blog-url']", Origin);
        await page.ClickAsync("[data-testid='new-blog-create']");

        // The author is told, and stays on the form with what they typed still there —
        // rather than landing in a blog that exists and quietly points nowhere.
        await page.WaitForSelectorAsync(".ed-toast:has-text('publish folder')");
        Assert.Contains("/blogs/new", page.Url, StringComparison.Ordinal);
        Assert.Equal(name, await page.InputValueAsync("[data-testid='new-blog-name']"));

        await page.GotoAsync("/");
        await page.WaitForInteractive();
        Assert.Equal(0, await page.Locator(".dash-card", new() { HasTextString = name }).CountAsync());
    }

    /// <summary>The dashboard, with at least one site already present (shared fixture).</summary>
    private static async Task<IPage> OpenDashboard(EditorFixture fixture)
    {
        var page = await fixture.OpenEditor();
        await page.GotoAsync("/");
        await page.WaitForInteractive();
        return page;
    }

    /// <summary>The slug the index derives from a title — mirrors Slug.Suggest for the assert.</summary>
    private static string SlugOf(string title) =>
        new(title.ToLowerInvariant().Select(c => char.IsAsciiLetterOrDigit(c) ? c : '-').ToArray());

    private static async Task WaitForFile(string path)
    {
        var deadline = DateTime.UtcNow.AddSeconds(60);
        while (DateTime.UtcNow < deadline)
        {
            if (File.Exists(path))
            {
                return;
            }

            await Task.Delay(200);
        }

        Assert.Fail($"Expected a published file at {path}, which never appeared.");
    }
}
