using Imprint.Publishing;

namespace Imprint.Publishing.Tests;

/// <summary>
/// The catalogue arrives as data and leaves as this site's markup — no frame, no bundle, no script.
/// </summary>
public sealed class PricingTemplateTests
{
    private const string Payload = """
        {
          "currency": "EUR",
          "distribution": null,
          "cohorts": [
            { "key": "teams", "name": "Engineering teams", "tagline": "The whole product surveyed.",
              "fromEur": "€61", "isFlatPrice": false, "maxConcurrentScans": 3,
              "modules": ["Core survey", "Agent-fix loop"],
              "buckets": [ { "key": "XXS", "lineScansPerMonth": 1000000, "baseEur": "€245" } ] },
            { "key": "freeoss", "name": "Free OSS", "tagline": "Free for open source.",
              "fromEur": "€0", "isFlatPrice": true, "modules": [], "buckets": [] }
          ],
          "onPrem": [
            { "key": "L", "name": "On-prem L", "lineScansPerYear": 600000000, "pricePerYearEur": 100000 },
            { "key": "XXL", "name": "On-prem XXL", "lineScansPerYear": null, "pricePerYearEur": 500000 }
          ]
        }
        """;

    [Fact]
    public void Every_package_its_price_and_its_buckets_are_rendered()
    {
        var html = PricingTemplate.Render(Payload);

        Assert.NotNull(html);
        Assert.Contains("<h3>Engineering teams</h3>", html, StringComparison.Ordinal);
        Assert.Contains("€61", html, StringComparison.Ordinal);
        Assert.Contains("€245", html, StringComparison.Ordinal);
        Assert.Contains("1,000,000", html, StringComparison.Ordinal);
        Assert.Contains("Core survey", html, StringComparison.Ordinal);
    }

    [Fact]
    public void The_free_lanes_are_rendered_like_any_other_package()
    {
        // These were invisible to every crawler while the prices lived in a frame — a competitor's
        // "free forever" was plain text and ours was not.
        var html = PricingTemplate.Render(Payload);

        Assert.Contains("<h3>Free OSS</h3>", html, StringComparison.Ordinal);
    }

    [Fact]
    public void A_flat_price_is_not_labelled_as_a_starting_price()
    {
        var html = PricingTemplate.Render(Payload)!;
        var freeSection = html[html.IndexOf("Free OSS", StringComparison.Ordinal)..];

        Assert.DoesNotContain("€0</strong> / month and up", freeSection, StringComparison.Ordinal);
        Assert.Contains("€61</strong> / month and up", html, StringComparison.Ordinal);
    }

    [Fact]
    public void An_unlimited_on_prem_allowance_says_so_rather_than_going_blank()
    {
        var html = PricingTemplate.RenderOnPrem(Payload)!;

        Assert.Contains("600,000,000 line-scans", html, StringComparison.Ordinal);
        Assert.Contains("Unlimited", html, StringComparison.Ordinal);
        Assert.Contains("€100,000", html, StringComparison.Ordinal);
    }

    [Fact]
    public void The_packages_template_leaves_on_prem_to_its_own_section()
    {
        // The page keeps self-hosted in a section with its own heading and copy. One template
        // rendering both would duplicate the table or dictate the page's shape.
        var packages = PricingTemplate.Render(Payload)!;

        Assert.DoesNotContain("On-prem", packages, StringComparison.Ordinal);
        Assert.DoesNotContain("€100,000", packages, StringComparison.Ordinal);
    }

    [Fact]
    public void A_payload_with_no_on_prem_rows_renders_nothing()
    {
        Assert.Null(PricingTemplate.RenderOnPrem("{\"cohorts\":[],\"onPrem\":[]}"));
        Assert.Null(PricingTemplate.RenderOnPrem(null));
    }

    [Fact]
    public void It_renders_in_imprints_own_markup_so_the_site_styles_it()
    {
        var html = PricingTemplate.Render(Payload)!;

        Assert.Contains("class=\"ip-grid\"", html, StringComparison.Ordinal);
        Assert.Contains("class=\"ip-stack\"", html, StringComparison.Ordinal);
        Assert.Contains("class=\"ip-prose\"", html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("{\"cohorts\":[]}")]
    public void A_payload_with_no_packages_renders_nothing_rather_than_an_empty_price_list(string? payload)
    {
        // An empty price list reads as "free", which is worse than the fallback.
        Assert.Null(PricingTemplate.Render(payload));
    }

    [Fact]
    public void Text_from_the_payload_is_escaped()
    {
        var html = PricingTemplate.Render(
            "{\"cohorts\":[{\"name\":\"<script>x</script>\",\"fromEur\":\"€1\",\"modules\":[],\"buckets\":[]}]}");

        Assert.NotNull(html);
        Assert.DoesNotContain("<script>", html, StringComparison.Ordinal);
        Assert.Contains("&lt;script&gt;", html, StringComparison.Ordinal);
    }
}
