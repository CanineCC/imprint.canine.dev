using Imprint.Authoring.Domain;
using Imprint.Authoring.Features.Posts.ChangePostBody;
using Imprint.Authoring.Features.Posts.ChangePostMeta;
using Imprint.Authoring.Features.Posts.CreatePost;
using Imprint.Authoring.Features.Posts.PublishPost;
using Imprint.Authoring.Features.Posts.SchedulePost;
using Imprint.Authoring.Features.Posts.UnpublishPost;
using Imprint.Authoring.Features.Pages;
using Imprint.EventSourcing;
using Microsoft.Extensions.DependencyInjection;

namespace Imprint.Publishing.Tests.Pipeline;

/// <summary>
/// Posts through the real publisher: a post becomes an ordinary static page, the index and the
/// feed are generated from the same published set, and withdrawing one takes its file away.
/// </summary>
public sealed class PostPublishingTests
{
    private const string En = "en";
    private const string Base = "https://example.test";

    [Fact]
    public async Task A_published_post_becomes_a_static_page_under_the_blog_prefix()
    {
        await using var host = NewHost();
        var (site, id) = await NewPost(host, "hello-world");
        await host.Ok(new ChangePostBody(id, En, "# Hello\n\nSome **prose** and a line of code:\n\n```sh\nls -la\n```"));
        await host.Ok(new PublishPost(id, En));

        await host.Publisher.Synchronize();

        Assert.True(host.FileExists("blog/hello-world/index.html"));
        var html = host.ReadText("blog/hello-world/index.html");
        Assert.Contains("<h1", html, StringComparison.Ordinal);
        Assert.Contains("<strong>prose</strong>", html, StringComparison.Ordinal);
        // The code block rendered through the real node view, escaped as content.
        Assert.Contains("class=\"ip-code\"", html, StringComparison.Ordinal);
        Assert.Contains("ls -la", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_published_post_carries_its_date_on_the_page_not_only_in_the_index()
    {
        // The index has always been dated; the post itself was not, so the two disagreed about
        // whether this was a dated stream of writing at all.
        await using var host = NewHost();
        var (_, id) = await NewPost(host, "dated");
        await host.Ok(new ChangePostBody(id, En, "# Dated\n\nProse.\n"));
        // A date in the PAST: publishing then keeps the agreed instant (the scheduler wakes up
        // after the moment it was told about), which is also what makes this assertion a constant
        // rather than a reading of today's clock.
        await host.Ok(new SchedulePost(id, new DateTimeOffset(2026, 1, 5, 9, 0, 0, TimeSpan.FromHours(1))));
        await host.Ok(new PublishPost(id, En));

        await host.Publisher.Synchronize();

        var html = host.ReadText("blog/dated/index.html");
        // Written in the editorial zone, in the form a reader reads rather than an ISO stamp.
        Assert.Contains("5 January 2026", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_post_gets_the_sites_chrome_and_stylesheet_like_any_page()
    {
        // The whole reason a post is a node tree and not rendered markup.
        await using var host = NewHost();
        var (site, id) = await NewPost(host, "chrome");
        await host.Ok(new ChangePostBody(id, En, "Prose."));
        await host.Ok(new PublishPost(id, En));

        await host.Publisher.Synchronize();

        var html = host.ReadText("blog/chrome/index.html");
        var css = Assert.Single(host.FilesMatching("css/site.", ".css"));
        Assert.Contains(css, html, StringComparison.Ordinal);
        Assert.StartsWith("<!doctype html>", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_draft_is_not_published()
    {
        await using var host = NewHost();
        var (_, id) = await NewPost(host, "secret");
        await host.Ok(new ChangePostBody(id, En, "Not ready."));

        await host.Publisher.Synchronize();

        Assert.False(host.FileExists("blog/secret/index.html"));
        Assert.False(host.FileExists("feed.xml"));
    }

    [Fact]
    public async Task The_index_lists_published_posts_newest_first()
    {
        await using var host = NewHost();
        var site = await host.CreateSite();
        var older = await Publish(host, site, "older", "Older post");
        var newer = await Publish(host, site, "newer", "Newer post");

        await host.Publisher.Synchronize();

        var html = host.ReadText("blog/index.html");
        Assert.Contains("Older post", html, StringComparison.Ordinal);
        Assert.Contains("Newer post", html, StringComparison.Ordinal);
        Assert.True(
            html.IndexOf("Newer post", StringComparison.Ordinal) < html.IndexOf("Older post", StringComparison.Ordinal),
            "the newest post must come first on the index");
    }

    [Fact]
    public async Task The_feed_carries_absolute_links_and_publication_dates()
    {
        await using var host = NewHost();
        var site = await host.CreateSite();
        var id = await Publish(host, site, "hello-world", "Hello world");
        await host.Ok(new ChangePostMeta(id, En, null, "A first post."));
        await host.Ok(new PublishPost(id, En));

        await host.Publisher.Synchronize();

        var feed = host.ReadText("feed.xml");
        Assert.Contains("<rss version=\"2.0\">", feed, StringComparison.Ordinal);
        Assert.Contains($"<link>{Base}/blog/hello-world/</link>", feed, StringComparison.Ordinal);
        Assert.Contains($"<guid isPermaLink=\"true\">{Base}/blog/hello-world/</guid>", feed, StringComparison.Ordinal);
        Assert.Contains("<pubDate>", feed, StringComparison.Ordinal);
        Assert.Contains("A first post.", feed, StringComparison.Ordinal);
        // Descriptions only: a body in the feed would be a SECOND rendering of the post.
        Assert.DoesNotContain("<content:encoded", feed, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_post_is_in_the_sitemap_like_any_other_page()
    {
        await using var host = NewHost();
        var site = await host.CreateSite();
        await Publish(host, site, "hello-world", "Hello world");

        await host.Publisher.Synchronize();

        Assert.Contains($"{Base}/blog/hello-world/", host.ReadText("sitemap.xml"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Unpublishing_sweeps_the_page_away()
    {
        await using var host = NewHost();
        var site = await host.CreateSite();
        var id = await Publish(host, site, "temporary", "Temporary");
        await host.Publisher.Synchronize();
        Assert.True(host.FileExists("blog/temporary/index.html"));

        await host.Ok(new UnpublishPost(id));
        await host.Publisher.Synchronize();

        // Withdrawal has to reach the disk: a reader following an old link must get the 404,
        // not yesterday's page.
        Assert.False(host.FileExists("blog/temporary/index.html"));
    }

    [Fact]
    public async Task An_unpublished_edit_does_not_change_the_live_page()
    {
        await using var host = NewHost();
        var site = await host.CreateSite();
        var id = await Publish(host, site, "hello-world", "Hello world", "First cut.");
        await host.Publisher.Synchronize();

        await host.Ok(new ChangePostBody(id, En, "Second cut, still being written."));
        await host.Publisher.Synchronize();

        var html = host.ReadText("blog/hello-world/index.html");
        Assert.Contains("First cut.", html, StringComparison.Ordinal);
        Assert.DoesNotContain("still being written", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Republishing_updates_the_live_page()
    {
        await using var host = NewHost();
        var site = await host.CreateSite();
        var id = await Publish(host, site, "hello-world", "Hello world", "First cut.");
        await host.Publisher.Synchronize();

        await host.Ok(new ChangePostBody(id, En, "Second cut."));
        await host.Ok(new PublishPost(id, En));
        await host.Publisher.Synchronize();

        Assert.Contains("Second cut.", host.ReadText("blog/hello-world/index.html"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_site_with_no_posts_publishes_no_index_and_no_feed()
    {
        // An empty index is a promise of content that is not there, and an empty feed is one a
        // reader's aggregator would keep polling.
        await using var host = NewHost();
        await host.CreateSite();

        await host.Publisher.Synchronize();

        Assert.False(host.FileExists("blog/index.html"));
        Assert.False(host.FileExists("feed.xml"));
    }

    /// <summary>A host with a widget catalog: PublishPost needs one to answer "is this widget
    /// installed", and no host wires one by default (each must choose its own).</summary>
    private static PublishingTestHost NewHost() =>
        new(Base, configure: services => services.AddSingleton<IWidgetCatalog>(new EmptyWidgetCatalog()));

    private static async Task<(SiteId Site, PostId Id)> NewPost(PublishingTestHost host, string slug)
    {
        var site = await host.CreateSite();
        var id = PostId.New();
        await host.Ok(new CreatePost(id, site, slug, slug, En));
        return (site, id);
    }

    private static async Task<PostId> Publish(
        PublishingTestHost host, SiteId site, string slug, string title, string body = "Prose.")
    {
        var id = PostId.New();
        await host.Ok(new CreatePost(id, site, title, slug, En));
        await host.Ok(new ChangePostBody(id, En, body));
        await host.Ok(new PublishPost(id, En));
        return id;
    }
}

/// <summary>
/// Dispatch through the real command path. The publishing host drives aggregates directly for
/// arrangement, but a post must go through its slices so the projections the publisher reads are
/// populated the way production populates them.
/// </summary>
/// <summary>A manifest with nothing in it — these posts place no widgets, and an empty catalog
/// keeps the test honest about that rather than quietly declaring tags it never uses.</summary>
internal sealed class EmptyWidgetCatalog : IWidgetCatalog
{
    public bool Exists(string tag) => false;

    public IReadOnlySet<string> PropNames(string tag) => new HashSet<string>();
}

internal static class PostPublishingHostExtensions
{
    public static async Task Ok(this PublishingTestHost host, ICommand command)
    {
        var result = await host.Services.GetRequiredService<ICommandDispatcher>().Dispatch(command);
        Assert.True(result.Succeeded, $"{command.GetType().Name} failed: {result.ErrorMessage}");
        await host.Projections.CatchUp();
    }
}
