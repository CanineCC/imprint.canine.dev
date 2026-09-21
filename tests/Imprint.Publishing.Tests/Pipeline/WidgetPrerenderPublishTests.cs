using System.Text.Json;
using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace Imprint.Publishing.Tests.Pipeline;

/// <summary>
/// A widget that renders itself in a shadow root publishes an EMPTY element: the words a crawler or
/// a language model needs are fetched by script that never runs for them. These pin the publish-time
/// bake that puts those words back into the document — and that a fetch which fails changes nothing.
/// </summary>
public sealed class WidgetPrerenderPublishTests
{
    private sealed class StubSource(string? html) : IWidgetPrerenderSource
    {
        public List<Uri> Requested { get; } = [];

        public Task<string?> FetchAsync(Uri url, CancellationToken ct)
        {
            Requested.Add(url);
            return Task.FromResult(html);
        }
    }

    private static void WriteEmbedManifest(PublishingTestHost host)
    {
        var manifest = new[]
        {
            new
            {
                tag = "wd-embed",
                name = "Embed",
                bundle = "wd-embed.js",
                placeholder = "Loading the live view",
                fallbackHref = "https://app.example.test/embed/{view}",
                prerender = "https://app.example.test/embed/{view}",
                props = new[] { new { name = "view", label = "View" } },
            },
        };
        File.WriteAllText(
            Path.Combine(host.WidgetsDirectory, "manifest.json"), JsonSerializer.Serialize(manifest));
        File.WriteAllText(Path.Combine(host.WidgetsDirectory, "wd-embed.js"), "// island\n");
    }

    private static async Task<PublishingTestHost> PublishWithSource(StubSource source)
    {
        var host = new PublishingTestHost(configure: services =>
            services.AddSingleton<IWidgetPrerenderSource>(source));
        WriteEmbedManifest(host);

        var siteId = await host.CreateSite();
        var homeId = await host.CreatePage(siteId, "home", "Home");
        await host.AddSection(homeId, new SectionNode
        {
            Id = NodeId.New(),
            Children = NodeList.Of(new WidgetNode
            {
                Id = NodeId.New(),
                Tag = "wd-embed",
                Props = PropBag.Of([new KeyValuePair<string, string>("view", "pricing")]),
            }),
        });
        await host.SetNavigation(siteId, homeId);
        await host.Publish(homeId);
        await host.Publisher.Synchronize();
        return host;
    }

    [Fact]
    public async Task The_fetched_fragment_is_baked_into_the_published_element()
    {
        var source = new StubSource(
            "<html><head><style>.x{color:red}</style></head><body>"
            + "<div class=\"wd-plan\"><h3>Engineering teams</h3><p>From €245 a month</p></div>"
            + "<script>boot()</script></body></html>");

        await using var host = await PublishWithSource(source);
        var html = host.ReadText("index.html");

        // The words are in the document, inside the element the island will take over.
        Assert.Contains("<h3>Engineering teams</h3>", html, StringComparison.Ordinal);
        Assert.Contains("From €245 a month", html, StringComparison.Ordinal);

        // The template resolved against this instance's props — one request, for that view.
        var requested = Assert.Single(source.Requested);
        Assert.Equal("https://app.example.test/embed/pricing", requested.ToString());

        // What must NOT cross: the fragment's script, its style, and its class names.
        Assert.DoesNotContain("boot()", html, StringComparison.Ordinal);
        Assert.DoesNotContain(".x{color:red}", html, StringComparison.Ordinal);
        Assert.DoesNotContain("wd-plan", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_fetch_that_fails_publishes_the_element_exactly_as_before()
    {
        // ★ The bake is an enhancement. A publish that breaks because a fragment did not arrive
        //   would make every page on the site hostage to another service being up.
        await using var host = await PublishWithSource(new StubSource(null));
        var html = host.ReadText("index.html");

        Assert.Contains("<wd-embed", html, StringComparison.Ordinal);
        Assert.Contains("Loading the live view", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task With_no_source_configured_the_publisher_behaves_as_it_always_did()
    {
        await using var host = new PublishingTestHost();
        WriteEmbedManifest(host);

        var siteId = await host.CreateSite();
        var homeId = await host.CreatePage(siteId, "home", "Home");
        await host.AddSection(homeId, new SectionNode
        {
            Id = NodeId.New(),
            Children = NodeList.Of(new WidgetNode
            {
                Id = NodeId.New(),
                Tag = "wd-embed",
                Props = PropBag.Of([new KeyValuePair<string, string>("view", "pricing")]),
            }),
        });
        await host.SetNavigation(siteId, homeId);
        await host.Publish(homeId);
        await host.Publisher.Synchronize();

        Assert.Contains("Loading the live view", host.ReadText("index.html"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_templated_widget_publishes_visible_markup_and_hydrates_nothing()
    {
        // ★ THE WHOLE POINT. No data-island means no script, which means no iframe — the prices are
        //   in the page as the page's own markup, styled by the site's own stylesheet.
        var payload = """
            {"cohorts":[{"key":"teams","name":"Engineering teams","tagline":"t",
             "fromEur":"\u20AC61","isFlatPrice":false,"modules":["Core survey"],
             "buckets":[{"key":"XXS","lineScansPerMonth":1000000,"baseEur":"\u20AC245"}]}],
             "onPrem":[]}
            """;
        var source = new StubSource(payload);
        var host = new PublishingTestHost(configure: services =>
            services.AddSingleton<IWidgetPrerenderSource>(source));

        var manifest = new[]
        {
            new
            {
                tag = "wd-pricing",
                name = "Pricing",
                bundle = "",
                placeholder = "Prices load here",
                prerender = "https://app.example.test/api/public/pricing",
                prerenderTemplate = "pricing",
                props = Array.Empty<object>(),
            },
        };
        File.WriteAllText(
            Path.Combine(host.WidgetsDirectory, "manifest.json"),
            System.Text.Json.JsonSerializer.Serialize(manifest));

        await using var _ = host;
        var siteId = await host.CreateSite();
        var homeId = await host.CreatePage(siteId, "home", "Home");
        await host.AddSection(homeId, new SectionNode
        {
            Id = NodeId.New(),
            Children = NodeList.Of(new WidgetNode { Id = NodeId.New(), Tag = "wd-pricing" }),
        });
        await host.SetNavigation(siteId, homeId);
        await host.Publish(homeId);
        await host.Publisher.Synchronize();

        var html = host.ReadText("index.html");

        Assert.Contains("Engineering teams", html, StringComparison.Ordinal);
        Assert.Contains("\u20AC245", html, StringComparison.Ordinal);
        Assert.Contains("class=\"ip-grid\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data-island", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Prices load here", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Two_widgets_reading_ONE_endpoint_each_get_their_own_template_output()
    {
        // ★ THE REGRESSION. The pricing page shows packages in one section and self-hosted rows in
        //   another, both from /api/public/pricing. Keyed by URL alone the second widget's template
        //   overwrote the first's, one rendering was produced, both sections looked it up — and the
        //   page published two placeholders with nothing in the log to say why.
        var payload = """
            {"cohorts":[{"key":"teams","name":"Engineering teams","tagline":"t","fromEur":"\u20AC61",
             "isFlatPrice":false,"modules":[],"buckets":[]}],
             "onPrem":[{"key":"L","name":"On-prem L","lineScansPerYear":600000000,"pricePerYearEur":100000}]}
            """;
        var source = new StubSource(payload);
        var host = new PublishingTestHost(configure: services =>
            services.AddSingleton<IWidgetPrerenderSource>(source));

        object Widget(string tag, string template) => new
        {
            tag,
            name = tag,
            bundle = "",
            placeholder = $"{tag} fallback",
            prerender = "https://app.example.test/api/public/pricing",
            prerenderTemplate = template,
            props = Array.Empty<object>(),
        };
        File.WriteAllText(
            Path.Combine(host.WidgetsDirectory, "manifest.json"),
            System.Text.Json.JsonSerializer.Serialize(
                new[] { Widget("wd-pricing", "pricing"), Widget("wd-pricing-onprem", "pricing-onprem") }));

        await using var _ = host;
        var siteId = await host.CreateSite();
        var homeId = await host.CreatePage(siteId, "home", "Home");
        await host.AddSection(homeId, new SectionNode
        {
            Id = NodeId.New(),
            Children = NodeList.Of(
                new WidgetNode { Id = NodeId.New(), Tag = "wd-pricing" },
                new WidgetNode { Id = NodeId.New(), Tag = "wd-pricing-onprem" }),
        });
        await host.SetNavigation(siteId, homeId);
        await host.Publish(homeId);
        await host.Publisher.Synchronize();

        var html = host.ReadText("index.html");

        Assert.Contains("Engineering teams", html, StringComparison.Ordinal);   // the packages template
        Assert.Contains("On-prem L", html, StringComparison.Ordinal);           // the on-prem template
        Assert.DoesNotContain("fallback", html, StringComparison.Ordinal);      // neither fell back
        Assert.Single(source.Requested);                                        // and it fetched ONCE
    }
}
