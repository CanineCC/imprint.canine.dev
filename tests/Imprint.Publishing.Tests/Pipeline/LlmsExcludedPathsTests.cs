using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Syndication;
using Microsoft.Extensions.DependencyInjection;

namespace Imprint.Publishing.Tests.Pipeline;

/// <summary>
/// A site can publish thousands of generated pages that are entirely legitimate SEO and
/// pure noise to a model trying to learn what the site is. The dividing line is not how a
/// page was produced — on cai.canine.dev the rubric catalogues are syndicated exactly like
/// the survey pages, and one is the standard while the other is a long tail. So the site
/// declares the paths, and the LLM files honour them while the sitemap does not.
/// </summary>
public sealed class LlmsExcludedPathsTests
{
    [Fact]
    public async Task Excluded_pages_leave_both_llm_files_but_stay_in_the_sitemap()
    {
        await using var host = new PublishingTestHost();
        var scenario = await Site(host);
        await host.SetLlmsExcludedPaths(scenario, "surveys/github");
        await host.Publisher.Synchronize();

        var index = host.ReadText("llms.txt");
        var corpus = host.ReadText("llms-full.txt");
        var sitemap = host.ReadText("sitemap.xml");

        Assert.DoesNotContain("/surveys/github/acme/widget/", index);
        Assert.DoesNotContain("/surveys/github/acme/widget/", corpus);
        Assert.DoesNotContain("A survey of one repository", corpus);

        // The pages exist to be indexed. Taking them out of the sitemap would defeat the
        // only reason they are published.
        Assert.Contains("/surveys/github/acme/widget/", sitemap);

        // And the page is still served — this is a listing policy, not an unpublish.
        Assert.True(host.FileExists("surveys/github/acme/widget/index.html"));
    }

    [Fact]
    public async Task The_prefix_covers_what_is_under_it_and_leaves_its_own_index_page()
    {
        await using var host = new PublishingTestHost();
        var scenario = await Site(host);
        await host.SetLlmsExcludedPaths(scenario, "surveys/github");
        await host.Publisher.Synchronize();

        var index = host.ReadText("llms.txt");

        // "surveys/github" is the declared prefix, so the /surveys/ landing page — the one
        // that tells a model these surveys exist at all — is still listed.
        Assert.Contains("](/surveys/)", index);
        Assert.DoesNotContain("](/surveys/github/", index);
    }

    [Fact]
    public async Task A_sibling_that_merely_starts_with_the_prefix_is_not_swallowed()
    {
        await using var host = new PublishingTestHost();
        var scenario = await Site(host);
        var store = host.Services.GetRequiredService<SyndicatedPageStore>();
        store.Upsert(Page(scenario, "surveys-explained", "How we survey"));

        await host.SetLlmsExcludedPaths(scenario, "surveys");
        await host.Publisher.Synchronize();

        var index = host.ReadText("llms.txt");

        Assert.Contains("](/surveys-explained/)", index);
        Assert.DoesNotContain("](/surveys/)", index);
    }

    [Fact]
    public async Task A_trailing_wildcard_covers_a_family_of_generated_names()
    {
        await using var host = new PublishingTestHost();
        var scenario = await Site(host);
        var store = host.Services.GetRequiredService<SyndicatedPageStore>();
        store.Upsert(Page(scenario, "dimensions", "Dimensions & lenses", "The vocabulary."));
        store.Upsert(Page(scenario, "dimensions/rubric-2026.08.12", "Rubric 08.12"));
        store.Upsert(Page(scenario, "dimensions/rubric-2026.08.19", "Rubric 08.19"));

        await host.SetLlmsExcludedPaths(scenario, "dimensions/rubric*");
        await host.Publisher.Synchronize();

        var index = host.ReadText("llms.txt");

        // Every dated snapshot goes, including ones that did not exist when the policy was
        // written — that is the whole reason the wildcard exists.
        Assert.DoesNotContain("rubric-2026.08.12", index);
        Assert.DoesNotContain("rubric-2026.08.19", index);

        // The catalogue page itself is the standard, not a snapshot of it. It stays.
        Assert.Contains("](/dimensions/)", index);
        Assert.Contains("2 pages under /dimensions/rubric*", index);
    }

    [Fact]
    public async Task The_omission_is_stated_and_points_at_the_sitemap()
    {
        await using var host = new PublishingTestHost();
        var scenario = await Site(host);
        await host.SetLlmsExcludedPaths(scenario, "surveys/github");
        await host.Publisher.Synchronize();

        // Two survey pages are excluded. A file that silently drops a section of the site
        // reads as the whole site, and a model would answer "it is not mentioned".
        foreach (var file in (string[])["llms.txt", "llms-full.txt"])
        {
            var text = host.ReadText(file);
            Assert.Contains("2 pages under /surveys/github/", text);
            Assert.Contains("are published for search engines and deliberately left out", text);
            Assert.Contains("/sitemap.xml", text);
        }
    }

    [Fact]
    public async Task Without_a_policy_nothing_is_excluded_and_nothing_is_claimed()
    {
        await using var host = new PublishingTestHost();
        await Site(host);
        await host.Publisher.Synchronize();

        var index = host.ReadText("llms.txt");

        Assert.Contains("](/surveys/github/acme/widget/)", index);
        Assert.DoesNotContain("deliberately left out", index);
    }

    [Fact]
    public async Task Clearing_the_policy_puts_the_pages_back()
    {
        await using var host = new PublishingTestHost();
        var scenario = await Site(host);
        await host.SetLlmsExcludedPaths(scenario, "surveys/github");
        await host.Publisher.Synchronize();
        Assert.DoesNotContain("/surveys/github/acme/widget/", host.ReadText("llms.txt"));

        await host.SetLlmsExcludedPaths(scenario);
        await host.Publisher.Synchronize();

        Assert.Contains("](/surveys/github/acme/widget/)", host.ReadText("llms.txt"));
    }

    // ------------------------------------------------------------------- scenario

    /// <summary>A site shaped like cai: authored pages, a syndicated index, and a long tail under it.</summary>
    private static async Task<SiteId> Site(PublishingTestHost host)
    {
        var siteId = await host.CreateSite("CAI", "en");
        var homeId = await host.CreatePage(siteId, "home", "Home");
        await host.SetNavigation(siteId, homeId);
        await host.Publish(homeId);

        var store = host.Services.GetRequiredService<SyndicatedPageStore>();
        store.Upsert(Page(siteId, "surveys", "Measured open-source projects", "The corpus index."));
        store.Upsert(Page(siteId, "surveys/github/acme/widget", "acme/widget"));
        store.Upsert(Page(siteId, "surveys/github/acme/gadget", "acme/gadget"));
        return siteId;
    }

    private static SyndicatedPage Page(SiteId siteId, string path, string heading, string body = "A survey of one repository.") =>
        new(siteId, path,
            LocalizedText.Of(new Locale("en"), heading),
            LocalizedText.Empty,
            LocalizedText.Of(new Locale("en"), $"{heading} — an entry in the corpus."),
            new SectionNode
            {
                Id = NodeId.New(),
                Children = NodeList.Of([
                    new HeadingNode { Id = NodeId.New(), Level = 1, Text = LocalizedText.Of(new Locale("en"), heading) },
                    new RichTextNode
                    {
                        Id = NodeId.New(),
                        Html = LocalizedText.Of(new Locale("en"), $"<p>{body}</p>"),
                    },
                ]),
            },
            ContentHash: $"hash-{path}",
            UpdatedAt: DateTimeOffset.UnixEpoch);
}
