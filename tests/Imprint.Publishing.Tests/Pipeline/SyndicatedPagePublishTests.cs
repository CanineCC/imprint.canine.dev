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
    private static SyndicatedPage Survey(SiteId siteId, string path, string heading) =>
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
                        Html = LocalizedText.Of(new Locale("en"), "<p>A document database and event store built on PostgreSQL.</p>"),
                    },
                    new HeadingNode
                    {
                        Id = NodeId.New(), Level = 2,
                        Text = LocalizedText.Of(new Locale("en"), "The condition"),
                    },
                ]),
            },
            ContentHash: "hash-1",
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
}
