using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;

namespace Imprint.Publishing.Tests.Pipeline;

/// <summary>
/// llms-full.txt is the one output that carries body prose rather than a description of it.
/// Its correctness questions are therefore about the words themselves: that they are the
/// words the page shows, that they come from one locale, and that the file stays a size
/// something can actually read.
/// </summary>
public sealed class LlmsFullTxtTests
{
    [Fact]
    public async Task The_corpus_carries_the_words_the_index_only_summarises()
    {
        await using var host = new PublishingTestHost();
        var siteId = await host.CreateSite("Acme Studio", "en");
        var homeId = await host.CreatePage(siteId, "home", "Home");
        await host.AddSection(homeId, new SectionNode
        {
            Id = NodeId.New(),
            Children = NodeList.Of(
                new HeadingNode
                {
                    Id = NodeId.New(),
                    Level = 2,
                    Text = LocalizedText.Of(PublishingTestHost.En, "How the survey runs"),
                },
                new RichTextNode
                {
                    Id = NodeId.New(),
                    Html = LocalizedText.Of(
                        PublishingTestHost.En,
                        "<p>The score recomputes from published evidence, so anyone can falsify it.</p>"),
                },
                new ButtonNode
                {
                    Id = NodeId.New(),
                    Label = LocalizedText.Of(PublishingTestHost.En, "Survey a repo"),
                    LinkTo = new ExternalLink("https://example.test/start"),
                },
                new ImageNode
                {
                    Id = NodeId.New(),
                    Alt = LocalizedText.Of(PublishingTestHost.En, "A studio photo"),
                }),
        });
        await host.SetMeta(homeId, "en", null, "What Acme measures.");
        await host.SetNavigation(siteId, homeId);
        await host.Publish(homeId);
        await host.Publisher.Synchronize();

        var index = host.ReadText("llms.txt");
        var corpus = host.ReadText("llms-full.txt");

        // The distinction that justifies the file: the index has the summary, the corpus
        // has the sentence. A model given only the index cannot answer from the body.
        Assert.Contains("What Acme measures.", index);
        Assert.DoesNotContain("falsify", index);
        Assert.Contains("The score recomputes from published evidence, so anyone can falsify it.", corpus);

        // Structure survives: headings stay headings, one level under the page's own "##".
        Assert.Contains("## Home\n", corpus);
        Assert.Contains("### How the survey runs", corpus);
        Assert.Contains("What Acme measures.", corpus);

        // A call to action is a link, not a dangling verb.
        Assert.Contains("[Survey a repo](https://example.test/start)", corpus);
        Assert.Contains("![A studio photo]", corpus);
    }

    [Fact]
    public async Task The_index_points_at_the_corpus()
    {
        await using var host = new PublishingTestHost();
        await TemplatedSiteScenario.Build(host);
        await host.Publisher.Synchronize();

        Assert.Contains("/llms-full.txt", host.ReadText("llms.txt"));
    }

    [Fact]
    public async Task A_placed_block_contributes_the_words_the_page_shows()
    {
        await using var host = new PublishingTestHost();
        var siteId = await host.CreateSite("Acme Studio", "en");

        var definitionHeadingId = NodeId.New();
        var blockId = await host.DefineBlock("Promo", new StackNode
        {
            Id = NodeId.New(),
            Children = NodeList.Of(new HeadingNode
            {
                Id = definitionHeadingId,
                Level = 2,
                Text = LocalizedText.Of(PublishingTestHost.En, "The definition's own words"),
            }),
        });

        var instanceId = NodeId.New();
        var homeId = await host.CreatePage(siteId, "home", "Home");
        await host.AddSection(homeId, new SectionNode
        {
            Id = NodeId.New(),
            Children = NodeList.Of(new BlockInstanceNode { Id = instanceId, DefinitionId = blockId }),
        });
        await host.SetBlockOverride(homeId, instanceId, definitionHeadingId, "text", "What this page actually says");
        await host.SetNavigation(siteId, homeId);
        await host.Publish(homeId);
        await host.Publisher.Synchronize();

        var corpus = host.ReadText("llms-full.txt");

        // The rendered page shows the override, so the corpus must too — otherwise the file
        // quotes the site saying something no visitor ever sees.
        Assert.Contains("What this page actually says", corpus);
        Assert.DoesNotContain("The definition's own words", corpus);
    }

    [Fact]
    public async Task Only_the_default_locale_is_written()
    {
        await using var host = new PublishingTestHost();
        await TemplatedSiteScenario.Build(host);
        await host.Publisher.Synchronize();

        var corpus = host.ReadText("llms-full.txt");

        // The scenario publishes en and da. One question deserves one answer.
        Assert.DoesNotContain("/da/", corpus);
        Assert.DoesNotContain("Om os", corpus);
        Assert.Contains("/about/", corpus);
    }

    [Fact]
    public async Task A_page_without_a_description_is_not_introduced_by_its_own_title_twice()
    {
        await using var host = new PublishingTestHost();
        var siteId = await host.CreateSite("Acme Studio", "en");
        var homeId = await host.CreatePage(siteId, "home", "Home");
        await host.SetNavigation(siteId, homeId);
        await host.Publish(homeId);
        await host.Publisher.Synchronize();

        // The index falls back to the title when there is no description, which beats a
        // blank. Here the title is already the heading directly above.
        Assert.DoesNotContain("## Home\n\n/\n\nHome\n", host.ReadText("llms-full.txt"));
    }

    [Fact]
    public async Task A_site_that_speaks_for_itself_heads_its_own_corpus()
    {
        await using var host = new PublishingTestHost();
        var scenario = await TemplatedSiteScenario.Build(host);
        await host.SetLlmsPreamble(scenario.SiteId, "# Acme Studio — the independent surveyor");
        await host.Publisher.Synchronize();

        Assert.StartsWith("# Acme Studio — the independent surveyor", host.ReadText("llms-full.txt"));
    }

    [Fact]
    public async Task A_large_corpus_is_bounded_and_says_what_it_left_out()
    {
        await using var host = new PublishingTestHost();
        var siteId = await host.CreateSite("Corpus", "en");
        var homeId = await host.CreatePage(siteId, "home", "Home");
        await host.SetNavigation(siteId, homeId);
        await host.Publish(homeId);

        // Pages heavy enough that the budget, not the page count, is what stops the file.
        // A single node is capped at 20k characters, so the weight comes from several.
        var paragraph = $"<p>{new string('w', 19_000)}</p>";
        for (var i = 0; i < 12; i++)
        {
            var id = await host.CreatePage(siteId, $"item-{i:D3}", $"Item {i:D3}");
            await host.AddSection(id, new SectionNode
            {
                Id = NodeId.New(),
                Children = NodeList.Of([
                    .. Enumerable.Range(0, 8).Select(_ => new RichTextNode
                    {
                        Id = NodeId.New(),
                        Html = LocalizedText.Of(PublishingTestHost.En, paragraph),
                    }),
                ]),
            });
            await host.Publish(id);
        }

        await host.Publisher.Synchronize();
        var corpus = host.ReadText("llms-full.txt");

        Assert.True(corpus.Length <= 1_000_000, $"llms-full.txt should stay within budget; was {corpus.Length} chars");
        Assert.Contains("further pages are not included here", corpus);
        Assert.Contains("/sitemap.xml", corpus);

        // Bounded, but not stingy: the budget exists to be spent.
        Assert.True(corpus.Length > 500_000, $"llms-full.txt gave up too early; was {corpus.Length} chars");

        // The cut falls between pages, never inside one.
        Assert.EndsWith(".\n", corpus);
    }

    [Fact]
    public async Task An_unpublished_page_leaves_the_corpus()
    {
        await using var host = new PublishingTestHost();
        var scenario = await TemplatedSiteScenario.Build(host);
        await host.Publisher.Synchronize();
        Assert.Contains("/about/", host.ReadText("llms-full.txt"));

        await host.Unpublish(scenario.AboutId);
        await host.Publisher.Synchronize();

        Assert.DoesNotContain("/about/", host.ReadText("llms-full.txt"));
    }
}
