using System.Text.Json;
using System.Text.RegularExpressions;

namespace Imprint.Publishing.Tests.Pipeline;

/// <summary>
/// The half of a published page that no reader ever sees: the share card and the structured
/// data a crawler or a language model reads instead of the prose.
///
/// All of it is derived from metadata the page already carries, which is the property worth
/// protecting — a site of two pages and a site of three thousand syndicated ones both get a
/// complete machine layer, and it cannot drift out of step with the title it came from.
/// </summary>
public sealed class MachineLayerTests
{
    private static string JsonLdOf(string html)
    {
        var match = Regex.Match(
            html, "<script type=\"application/ld\\+json\">(.*?)</script>", RegexOptions.Singleline);
        Assert.True(match.Success, "the page carries a JSON-LD block");
        return match.Groups[1].Value;
    }

    private static JsonElement[] GraphOf(string html)
    {
        using var document = JsonDocument.Parse(JsonLdOf(html));
        Assert.Equal("https://schema.org", document.RootElement.GetProperty("@context").GetString());
        return [.. document.RootElement.GetProperty("@graph").EnumerateArray().Select(node => node.Clone())];
    }

    private static JsonElement NodeOfType(JsonElement[] graph, string type) =>
        Assert.Single(graph, node => node.GetProperty("@type").GetString() == type);

    [Fact]
    public async Task Page_carries_a_share_card_derived_from_its_own_metadata()
    {
        await using var host = new PublishingTestHost(baseUrl: "https://acme.example");
        await TemplatedSiteScenario.Build(host);
        await host.Publisher.Synchronize();

        var html = host.ReadText("about/index.html");

        // The share card says exactly what the search snippet says: same title, same
        // description. Authoring them separately would only create a way to disagree.
        Assert.Contains("<meta property=\"og:title\" content=\"About\" />", html);
        Assert.Contains("<meta property=\"og:description\" content=\"About Acme Studio.\" />", html);
        Assert.Contains("<meta name=\"description\" content=\"About Acme Studio.\" />", html);
        Assert.Contains("<meta property=\"og:type\" content=\"website\" />", html);
        Assert.Contains("<meta property=\"og:site_name\" content=\"Acme Studio\" />", html);
        Assert.Contains("<meta property=\"og:url\" content=\"https://acme.example/about/\" />", html);

        // No wide share asset is set, so the card degrades to the small form rather than
        // promising an image that is not there.
        Assert.DoesNotContain("og:image", html);
        Assert.Contains("<meta name=\"twitter:card\" content=\"summary\" />", html);
    }

    [Fact]
    public async Task A_share_image_is_published_at_its_widest_variant_and_upgrades_the_card()
    {
        await using var host = new PublishingTestHost(baseUrl: "https://acme.example");
        var scenario = await TemplatedSiteScenario.Build(host);
        var socialId = await host.CreateImageAsset("share", 600, 1200);
        await host.SetSocialImage(scenario.SiteId, socialId);
        await host.Publisher.Synchronize();

        var html = host.ReadText("index.html");

        // The share card ships as the file that was uploaded, not as a derived WebP.
        // Link scrapers are not browsers: several of them (LinkedIn among them) skip a
        // WebP og:image entirely and show a no-image card, so a page can score perfectly
        // on "og:image present" and still share as a bare link. The format is the finding.
        var image = Regex.Match(html, "<meta property=\"og:image\" content=\"([^\"]+)\"");
        Assert.True(image.Success, "the page carries an og:image");
        Assert.StartsWith($"https://acme.example/assets/{socialId.Compact}-original.", image.Groups[1].Value);
        Assert.EndsWith(".jpg", image.Groups[1].Value);
        Assert.DoesNotContain(".webp", image.Groups[1].Value);

        // And the file it points at is really published, not merely named.
        Assert.True(
            host.FileExists(image.Groups[1].Value.Replace("https://acme.example/", "")),
            "the share card's original must exist in the output");

        // With an image there is something to show large; without one there was not.
        Assert.Contains("<meta name=\"twitter:card\" content=\"summary_large_image\" />", html);
        Assert.DoesNotContain("/media/", html);
    }

    [Fact]
    public async Task A_share_image_needs_an_origin_to_be_addressable()
    {
        // Same reasoning as og:url: a root-relative og:image is resolved by each consumer
        // against their own host, which is a wrong answer rather than a partial one.
        await using var host = new PublishingTestHost();
        var scenario = await TemplatedSiteScenario.Build(host);
        await host.SetSocialImage(scenario.SiteId, await host.CreateImageAsset("share", 1200));
        await host.Publisher.Synchronize();

        var html = host.ReadText("index.html");

        Assert.Contains("og:title", html);
        Assert.DoesNotContain("og:image", html);
        Assert.Contains("<meta name=\"twitter:card\" content=\"summary\" />", html);
    }

    [Fact]
    public async Task Open_graph_url_is_omitted_rather_than_emitted_relative()
    {
        // Open Graph has no base to resolve a relative reference against. Without a
        // configured origin, a root-relative og:url is not a weaker answer — it is a wrong
        // one, and every consumer resolves it against their own host.
        await using var host = new PublishingTestHost();
        await TemplatedSiteScenario.Build(host);
        await host.Publisher.Synchronize();

        var html = host.ReadText("about/index.html");

        Assert.Contains("og:title", html);
        Assert.DoesNotContain("og:url", html);
    }

    [Fact]
    public async Task Home_page_declares_the_site_and_who_publishes_it()
    {
        await using var host = new PublishingTestHost(baseUrl: "https://acme.example");
        await TemplatedSiteScenario.Build(host);
        await host.Publisher.Synchronize();

        var graph = GraphOf(host.ReadText("index.html"));

        var website = NodeOfType(graph, "WebSite");
        Assert.Equal("https://acme.example/#website", website.GetProperty("@id").GetString());
        Assert.Equal("Acme Studio", website.GetProperty("name").GetString());
        Assert.Equal("en", website.GetProperty("inLanguage").GetString());

        var organization = NodeOfType(graph, "Organization");
        Assert.Equal("https://acme.example/#organization", organization.GetProperty("@id").GetString());
        Assert.Equal(
            organization.GetProperty("@id").GetString(),
            website.GetProperty("publisher").GetProperty("@id").GetString());

        var page = NodeOfType(graph, "WebPage");
        Assert.Equal("https://acme.example/#webpage", page.GetProperty("@id").GetString());
        Assert.Equal(
            website.GetProperty("@id").GetString(),
            page.GetProperty("isPartOf").GetProperty("@id").GetString());

        // A trail of one says nothing, so the front page carries no breadcrumb.
        Assert.DoesNotContain(graph, node => node.GetProperty("@type").GetString() == "BreadcrumbList");
    }

    [Fact]
    public async Task Inner_page_refers_to_the_publisher_instead_of_repeating_it()
    {
        await using var host = new PublishingTestHost(baseUrl: "https://acme.example");
        await TemplatedSiteScenario.Build(host);
        await host.Publisher.Synchronize();

        var graph = GraphOf(host.ReadText("about/index.html"));

        // Stating the whole Organization on every page would say one thing many times and
        // let the copies disagree. Inner pages name it by @id and stop.
        Assert.DoesNotContain(graph, node => node.GetProperty("@type").GetString() == "Organization");
        Assert.Equal(
            "https://acme.example/#organization",
            NodeOfType(graph, "WebSite").GetProperty("publisher").GetProperty("@id").GetString());

        var trail = NodeOfType(graph, "BreadcrumbList").GetProperty("itemListElement");
        var steps = trail.EnumerateArray()
            .Select(step => (Name: step.GetProperty("name").GetString(), Url: step.GetProperty("item").GetString()))
            .ToArray();
        Assert.Equal(
            [("Acme Studio", "https://acme.example/"), ("About", "https://acme.example/about/")],
            steps);
    }

    [Fact]
    public async Task Structured_data_cannot_close_its_own_script_element()
    {
        // A title containing </script> would otherwise end the block early and spill the
        // rest of the JSON into the document as markup. The encoder escapes < and >, and
        // that is a security property of this output, not a formatting preference.
        await using var host = new PublishingTestHost(baseUrl: "https://acme.example");
        var scenario = await TemplatedSiteScenario.Build(host);
        await host.SetMeta(
            scenario.AboutId, "en", "</script><img src=x onerror=alert(1)>", metaDescription: null);
        await host.Publish(scenario.AboutId); // a meta change is a draft until it is published
        await host.Publisher.Synchronize();

        var html = host.ReadText("about/index.html");
        var block = JsonLdOf(html);

        // The property is absolute rather than a particular spelling of the escape: the
        // payload contains no unescaped '<' at all, so there is no character sequence in it
        // that any HTML parser can read as the start of a tag.
        Assert.DoesNotContain("<", block);

        // And it still parses — escaped, not mangled.
        using var document = JsonDocument.Parse(block);
        Assert.Equal(
            "</script><img src=x onerror=alert(1)>",
            document.RootElement.GetProperty("@graph").EnumerateArray()
                .Single(node => node.GetProperty("@type").GetString() == "WebPage")
                .GetProperty("name").GetString());
    }

    [Fact]
    public async Task The_not_found_page_refuses_to_be_indexed_and_claims_nothing()
    {
        await using var host = new PublishingTestHost(baseUrl: "https://acme.example");
        await TemplatedSiteScenario.Build(host);
        await host.Publisher.Synchronize();

        var html = host.ReadText("404.html");

        // It is not a page about anything: no share card, no structured data, and the one
        // place in the output where an explicit robots directive earns its bytes.
        Assert.Contains("<meta name=\"robots\" content=\"noindex, follow\" />", html);
        Assert.DoesNotContain("og:title", html);
        Assert.DoesNotContain("application/ld+json", html);
        Assert.DoesNotContain("rel=\"canonical\"", html);
    }

    [Fact]
    public async Task Every_locale_gets_its_own_machine_layer()
    {
        await using var host = new PublishingTestHost(baseUrl: "https://acme.example");
        await TemplatedSiteScenario.Build(host);
        await host.Publisher.Synchronize();

        var danish = host.ReadText("da/index.html");
        var graph = GraphOf(danish);

        Assert.Equal("da", NodeOfType(graph, "WebSite").GetProperty("inLanguage").GetString());
        Assert.Equal("https://acme.example/da/", NodeOfType(graph, "WebPage").GetProperty("url").GetString());
        Assert.Contains("<meta property=\"og:url\" content=\"https://acme.example/da/\" />", danish);
    }
}
