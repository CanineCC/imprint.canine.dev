namespace Imprint.Publishing.Tests.Pipeline;

/// <summary>
/// llms.txt is the one published file a model is expected to read WHOLE. That makes it
/// unlike every other output: its value is bounded by what fits, so what goes in it and
/// what stays out are both correctness questions.
/// </summary>
public sealed class LlmsTxtTests
{
    [Fact]
    public async Task A_site_speaks_for_itself_above_the_page_index()
    {
        await using var host = new PublishingTestHost();
        var scenario = await TemplatedSiteScenario.Build(host);
        await host.SetLlmsPreamble(scenario.SiteId, """
            # Acme Studio — the independent surveyor

            > What we are, in our own words.

            ## Key facts
            - Something a list of page titles could never say.
            """);
        await host.Publisher.Synchronize();

        var llms = host.ReadText("llms.txt");

        Assert.StartsWith("# Acme Studio — the independent surveyor", llms);
        Assert.Contains("## Key facts", llms);
        Assert.Contains("Something a list of page titles could never say.", llms);

        // The preamble stands in for the generated header entirely — the site does not get
        // introduced twice, once in its own voice and once in the template's.
        Assert.DoesNotContain("# Acme Studio\n", llms);

        // The generated index still follows: what the site says, then what it has.
        Assert.Contains("## Pages", llms);
        Assert.Contains("](/about/)", llms);
    }

    [Fact]
    public async Task Without_a_preamble_the_generated_header_still_introduces_the_site()
    {
        await using var host = new PublishingTestHost();
        await TemplatedSiteScenario.Build(host);
        await host.Publisher.Synchronize();

        var llms = host.ReadText("llms.txt");

        Assert.StartsWith("# Acme Studio\n", llms);
        Assert.Contains("## Pages", llms);
    }

    [Fact]
    public async Task Clearing_the_preamble_restores_the_generated_header()
    {
        await using var host = new PublishingTestHost();
        var scenario = await TemplatedSiteScenario.Build(host);
        await host.SetLlmsPreamble(scenario.SiteId, "# Written by hand");
        await host.Publisher.Synchronize();
        Assert.StartsWith("# Written by hand", host.ReadText("llms.txt"));

        await host.SetLlmsPreamble(scenario.SiteId, "   ");
        await host.Publisher.Synchronize();

        // Blank is not a preamble of spaces; it is no preamble.
        Assert.StartsWith("# Acme Studio\n", host.ReadText("llms.txt"));
    }

    [Fact]
    public async Task A_large_corpus_is_bounded_and_says_what_it_left_out()
    {
        await using var host = new PublishingTestHost();
        var siteId = await host.CreateSite("Corpus", "en");
        var homeId = await host.CreatePage(siteId, "home", "Home");
        await host.SetNavigation(siteId, homeId);
        await host.Publish(homeId);

        // 250 pages: past the 200 cap, the shape of a generated corpus.
        for (var i = 0; i < 250; i++)
        {
            var id = await host.CreatePage(siteId, $"item-{i:D3}", $"Item {i:D3}");
            await host.Publish(id);
        }

        await host.Publisher.Synchronize();
        var llms = host.ReadText("llms.txt");

        var listed = llms.Split('\n').Count(line => line.StartsWith("- [", StringComparison.Ordinal));
        Assert.Equal(200, listed);

        // A file that stops early without saying so reads as "this is everything". The
        // omission is stated, and it points at where the complete set actually lives.
        Assert.Contains("51 further pages are not listed here", llms);
        Assert.Contains("/sitemap.xml", llms);

        // The whole point of the cap: it stays a file a model can read.
        Assert.True(llms.Length < 50_000, $"llms.txt should stay small; was {llms.Length} bytes");
    }

    [Fact]
    public async Task A_small_site_is_listed_whole_and_claims_no_omission()
    {
        await using var host = new PublishingTestHost();
        await TemplatedSiteScenario.Build(host);
        await host.Publisher.Synchronize();

        var llms = host.ReadText("llms.txt");

        Assert.DoesNotContain("further pages are not listed", llms);
    }
}
