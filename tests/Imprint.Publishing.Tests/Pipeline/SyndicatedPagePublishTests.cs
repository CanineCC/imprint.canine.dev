using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Syndication;
using Microsoft.Extensions.DependencyInjection;

namespace Imprint.Publishing.Tests.Pipeline;

/// <summary>
/// Pages produced by another system and served as part of this site. The whole point is that once
/// they arrive they are ordinary pages: same views, same chrome, same stylesheet, same sitemap, same
/// sweep. If any of that needed a second code path, the two would drift — which is the failure the
/// manifest's renderer version exists to catch, and better still to prevent.
/// </summary>
public sealed class SyndicatedPagePublishTests
{
    [Fact]
    public async Task Pushing_new_content_to_the_same_path_re_renders_it()
    {
        // A syndicated page has no aggregate, so it is created with PublishedVersion 0 and stays there —
        // and staleness asks "old.PublishedVersion < page.PublishedVersion", which is 0 < 0 forever. Without
        // its content hash standing in as its version, a page renders ONCE and every later push is stored
        // and never re-rendered.
        //
        // That shipped: the producer reported "955 page(s) built, 955 changed" while the site re-rendered
        // two of them — the two whose PATH was new. So a corpus index went on advertising 2,853 projects and
        // linking to 1,883 pages that had just been withdrawn, and the only way it ever refreshed was a
        // chrome or CSS edit happening to re-render the whole site.
        await using var host = new PublishingTestHost();
        var scenario = await TemplatedSiteScenario.Build(host);
        var store = host.Services.GetRequiredService<SyndicatedPageStore>();

        store.Upsert(Survey(scenario.SiteId, "registry/github/jasperfx/marten", "JasperFx/marten"));
        await host.Publisher.Synchronize();
        Assert.Contains("A document database and event store", host.ReadText("registry/github/jasperfx/marten/index.html"));

        store.Upsert(Survey(scenario.SiteId, "registry/github/jasperfx/marten", "JasperFx/marten",
            body: "<p>Now it says something else entirely.</p>", contentHash: "hash-2"));
        await host.Publisher.Synchronize();

        var html = host.ReadText("registry/github/jasperfx/marten/index.html");
        Assert.Contains("Now it says something else entirely.", html);
        Assert.DoesNotContain("A document database and event store", html);
    }

    [Fact]
    public async Task A_syndicated_page_can_carry_one_of_the_sites_own_widgets()
    {
        // The corpus producer sends an ISLAND, never a drawing: the site renders the widget from live data, so a
        // survey page shows the same score card the marketing site does and cannot drift from it. That only works
        // if a widget node survives syndication and gets the island treatment like any authored page's would.
        await using var host = new PublishingTestHost();
        var scenario = await TemplatedSiteScenario.Build(host);
        var store = host.Services.GetRequiredService<SyndicatedPageStore>();

        var page = Survey(scenario.SiteId, "registry/github/jasperfx/marten", "JasperFx/marten") with
        {
            Node = new SectionNode
            {
                Id = NodeId.New(),
                Children = NodeList.Of([
                    new WidgetNode
                    {
                        Id = NodeId.New(),
                        Tag = "x-note",
                        Props = PropBag.Of([new KeyValuePair<string, string>("text", "hello")]),
                    },
                ]),
            },
        };
        store.Upsert(page);

        await host.Publisher.Synchronize();

        var html = host.ReadText("registry/github/jasperfx/marten/index.html");
        Assert.Contains("<x-note class=\"ip-widget\" text=\"hello\" data-island=\"/widgets/x-note.", html);
        Assert.Contains(PublisherScripts.IslandLoader, html);   // and the loader ships with it
    }

    private static SyndicatedPage Survey(
        SiteId siteId, string path, string heading,
        string body = "<p>A document database and event store built on PostgreSQL.</p>",
        string contentHash = "hash-1") =>
        new(siteId, path,
            LocalizedText.Of(new Locale("en"), heading),
            LocalizedText.Empty,
            LocalizedText.Of(new Locale("en"), "A published survey of one repository."),
            new SectionNode
            {
                Id = NodeId.New(),
                Children = NodeList.Of([
                    new HeadingNode { Id = NodeId.New(), Level = 1, Text = LocalizedText.Of(new Locale("en"), heading) },
                    new RichTextNode
                    {
                        Id = NodeId.New(),
                        Html = LocalizedText.Of(new Locale("en"), body),
                    },
                    new HeadingNode
                    {
                        Id = NodeId.New(), Level = 2,
                        Text = LocalizedText.Of(new Locale("en"), "The condition"),
                    },
                ]),
            },
            ContentHash: contentHash,
            UpdatedAt: DateTimeOffset.Parse("2026-07-29T00:00:00Z"));

    [Fact]
    public async Task A_syndicated_page_publishes_at_its_nested_path_with_the_site_chrome()
    {
        await using var host = new PublishingTestHost();
        var scenario = await TemplatedSiteScenario.Build(host);
        var store = host.Services.GetRequiredService<SyndicatedPageStore>();
        store.Upsert(Survey(scenario.SiteId, "registry/github/jasperfx/marten", "JasperFx/marten"));

        await host.Publisher.Synchronize();

        // A path an authored slug could never express: slugs are one flat segment by design.
        var html = host.ReadText("registry/github/jasperfx/marten/index.html");
        Assert.Contains("JasperFx/marten", html);
        Assert.Contains("A document database and event store", html);
        Assert.Contains("<footer", html);                     // the site's own chrome, not the producer's
        Assert.Contains("id=\"the-condition\"", html);       // and its heading anchors, for free
        Assert.DoesNotContain("id=\"jasperfx-marten\"", html);   // h1 gets none, same rule as an authored page
    }

    [Fact]
    public async Task It_is_indexed_like_any_other_page()
    {
        await using var host = new PublishingTestHost();
        var scenario = await TemplatedSiteScenario.Build(host);
        var store = host.Services.GetRequiredService<SyndicatedPageStore>();
        store.Upsert(Survey(scenario.SiteId, "registry/github/jasperfx/marten", "JasperFx/marten"));

        await host.Publisher.Synchronize();

        Assert.Contains("/registry/github/jasperfx/marten/", host.ReadText("sitemap.xml"));
        Assert.Contains("/registry/github/jasperfx/marten/", host.ReadText("llms.txt"));
    }

    [Fact]
    public async Task Withdrawing_one_removes_its_files()
    {
        // The producer stops publishing a survey — the site must stop serving it, not keep an orphan
        // that outlives the thing it described.
        await using var host = new PublishingTestHost();
        var scenario = await TemplatedSiteScenario.Build(host);
        var store = host.Services.GetRequiredService<SyndicatedPageStore>();
        store.Upsert(Survey(scenario.SiteId, "registry/github/jasperfx/marten", "JasperFx/marten"));
        await host.Publisher.Synchronize();
        Assert.True(host.FileExists("registry/github/jasperfx/marten/index.html"));

        store.Remove(scenario.SiteId, "registry/github/jasperfx/marten");
        await host.Publisher.Synchronize();

        Assert.False(host.FileExists("registry/github/jasperfx/marten/index.html"));
    }

    [Fact]
    public async Task Re_pushing_identical_content_writes_nothing()
    {
        // The producer re-pushes everything it owns on every run. Most of those pushes carry content
        // that has not changed, and none of them should churn the output.
        await using var host = new PublishingTestHost();
        var scenario = await TemplatedSiteScenario.Build(host);
        var store = host.Services.GetRequiredService<SyndicatedPageStore>();
        var page = Survey(scenario.SiteId, "registry/github/jasperfx/marten", "JasperFx/marten");
        store.Upsert(page);
        await host.Publisher.Synchronize();

        var changed = store.Upsert(page);
        var report = await host.Publisher.Synchronize();

        Assert.False(changed);                 // the store recognised it as the same content
        Assert.Equal(0, report.FilesWritten);
    }

    [Fact]
    public async Task An_authored_page_still_owns_its_own_path()
    {
        await using var host = new PublishingTestHost();
        var scenario = await TemplatedSiteScenario.Build(host);
        var store = host.Services.GetRequiredService<SyndicatedPageStore>();
        store.Upsert(Survey(scenario.SiteId, "registry/github/jasperfx/marten", "JasperFx/marten"));

        await host.Publisher.Synchronize();

        Assert.True(host.FileExists("index.html"));
        Assert.True(host.FileExists("about/index.html"));
    }

    [Fact]
    public async Task A_change_announces_itself_so_the_publisher_can_pick_it_up()
    {
        // These pages are not event-sourced, so they never reach the projection engine. Without a signal of their
        // own a pushed page would sit in the table until some UNRELATED authoring event happened to trigger a pass —
        // on a site nobody is editing, that is indefinitely, and the producer would have been told it succeeded.
        await using var host = new PublishingTestHost();
        var store = host.Services.GetRequiredService<SyndicatedPageStore>();
        var siteId = SiteId.New();
        var page = Survey(siteId, "registry/github/jasperfx/marten", "JasperFx/marten");

        var woken = 0;
        store.Changed += () => woken++;

        store.Upsert(page);
        Assert.Equal(1, woken);

        store.Upsert(page);                     // identical content changes nothing, so it wakes nothing
        Assert.Equal(1, woken);

        store.Remove(siteId, page.Path);
        Assert.Equal(2, woken);

        store.Remove(siteId, page.Path);        // already gone
        Assert.Equal(2, woken);
    }
}
