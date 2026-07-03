using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;

namespace Imprint.Publishing.Tests.Pipeline;

/// <summary>
/// A page's rendered output depends on more than its own version and the chrome: it
/// resolves the paths of pages it links to and the content of blocks it instances,
/// live at render time. Those cross-aggregate dependencies must feed staleness, or the
/// output silently rots (dead links, stale block copy). Audit findings, pinned here.
/// </summary>
public sealed class CrossAggregateStalenessTests
{
    private static readonly Locale En = PublishingTestHost.En;

    [Fact]
    public async Task Editing_a_block_definition_re_renders_instancing_pages()
    {
        await using var host = new PublishingTestHost();
        var scenario = await TemplatedSiteScenario.Build(host);
        await host.Publisher.Synchronize();
        Assert.Contains("Reusable promo block", host.ReadText("index.html"));

        // Edit the block the home page instances — a different aggregate entirely.
        await host.UpdateBlockSpec(scenario.BlockId, new StackNode
        {
            Id = NodeId.New(),
            Children = NodeList.Of(new HeadingNode
            {
                Id = NodeId.New(),
                Level = 2,
                Text = LocalizedText.Of(En, "Updated promo copy"),
            }),
        });

        var report = await host.Publisher.Synchronize();

        Assert.True(report.PagesRendered >= 1, "the instancing page should have re-rendered");
        var html = host.ReadText("index.html");
        Assert.Contains("Updated promo copy", html);
        Assert.DoesNotContain("Reusable promo block", html);
    }

    [Fact]
    public async Task A_linked_pages_slug_change_re_renders_the_linking_page()
    {
        await using var host = new PublishingTestHost();
        var (target, _) = await BuildLinkingSite(host);
        await host.Publisher.Synchronize();

        // The link resolves to the target's current path (target is not the home page).
        Assert.Contains("href=\"/target/\"", host.ReadText("linker/index.html"));

        // Move the target's slug and republish it — the linker itself never changed.
        await host.ChangeSlug(target, "renamed");
        await host.Publish(target);
        var report = await host.Publisher.Synchronize();

        Assert.True(report.PagesRendered >= 1);
        var linkerHtml = host.ReadText("linker/index.html");
        Assert.Contains("href=\"/renamed/\"", linkerHtml);
        Assert.DoesNotContain("href=\"/target/\"", linkerHtml);
    }

    [Fact]
    public async Task Unpublishing_a_linked_page_re_renders_the_linking_page()
    {
        await using var host = new PublishingTestHost();
        var (target, _) = await BuildLinkingSite(host);
        await host.Publisher.Synchronize();
        Assert.Contains("href=\"/target/\"", host.ReadText("linker/index.html"));

        // Withdraw the target: the link must not remain a 404.
        await host.Unpublish(target);
        var report = await host.Publisher.Synchronize();

        Assert.True(report.PagesRendered >= 1);
        Assert.DoesNotContain("href=\"/target/\"", host.ReadText("linker/index.html"));
    }

    /// <summary>home (nav-first, at "/"), a linkable target at /target/, and a linker at /linker/ whose button points at target.</summary>
    private static async Task<(PageId Target, PageId Linker)> BuildLinkingSite(PublishingTestHost host)
    {
        var siteId = await host.CreateSite("Links", "en");

        var home = await host.CreatePage(siteId, "home", "Home");
        await host.AddSection(home, Section(new HeadingNode
        {
            Id = NodeId.New(), Level = 1, Text = LocalizedText.Of(En, "Home"),
        }));

        var target = await host.CreatePage(siteId, "target", "Target");
        await host.AddSection(target, Section(new HeadingNode
        {
            Id = NodeId.New(), Level = 1, Text = LocalizedText.Of(En, "Target"),
        }));

        var linker = await host.CreatePage(siteId, "linker", "Linker");
        await host.AddSection(linker, Section(new ButtonNode
        {
            Id = NodeId.New(),
            Label = LocalizedText.Of(En, "Go to target"),
            LinkTo = new PageLink(target),
        }));

        await host.SetNavigation(siteId, home, target, linker);
        await host.Publish(home);
        await host.Publish(target);
        await host.Publish(linker);
        return (target, linker);
    }

    private static SectionNode Section(params Node[] children) => new()
    {
        Id = NodeId.New(),
        Children = NodeList.Of([.. children]),
    };
}
