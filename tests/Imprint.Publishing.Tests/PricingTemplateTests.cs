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
    public void The_free_lanes_are_rendered_last_because_the_layout_depends_on_it()
    {
        // .ip-grid-5up steps five-across to THREE-across, so the fourth and fifth cards make the
        // second row. "The paid packages, with the free lanes under them" is only what that reads as
        // while the free lanes ARE the last two — reorder here and the breakpoint means nothing.
        var payload = """
            {"cohorts":[
              {"name":"Student","fromEur":"€0","modules":[],"buckets":[]},
              {"name":"Freelancer","fromEur":"€24","modules":[],"buckets":[]},
              {"name":"Teams","fromEur":"€61","modules":[],"buckets":[]},
              {"name":"Enterprise","fromEur":"€124","modules":[],"buckets":[]},
              {"name":"Free OSS","fromEur":"€0","modules":[],"buckets":[]}],
             "onPrem":[]}
            """;

        var html = PricingTemplate.Render(payload)!;
        var order = new[] { "Freelancer", "Teams", "Enterprise", "Student", "Free OSS" }
            .Select(name => html.IndexOf($"<h3>{name}</h3>", StringComparison.Ordinal))
            .ToList();

        Assert.DoesNotContain(-1, order);
        Assert.Equal(order.OrderBy(i => i), order);
    }

    [Fact]
    public void The_catalogues_own_order_is_kept_within_each_group()
    {
        // The partition decides which GROUP a card is in and nothing else: the catalogue still
        // decides that Freelancer comes before Enterprise, and that Student comes before Free OSS.
        var payload = """
            {"cohorts":[
              {"name":"Enterprise","fromEur":"€124","modules":[],"buckets":[]},
              {"name":"Free OSS","fromEur":"€0","modules":[],"buckets":[]},
              {"name":"Freelancer","fromEur":"€24","modules":[],"buckets":[]},
              {"name":"Student","fromEur":"€0","modules":[],"buckets":[]}],
             "onPrem":[]}
            """;

        var html = PricingTemplate.Render(payload)!;

        Assert.True(html.IndexOf("Enterprise", StringComparison.Ordinal)
                    < html.IndexOf("Freelancer", StringComparison.Ordinal));
        Assert.True(html.IndexOf("Free OSS", StringComparison.Ordinal)
                    < html.IndexOf("Student", StringComparison.Ordinal));
    }

    [Fact]
    public void A_free_lane_is_not_labelled_as_a_starting_price()
    {
        var html = PricingTemplate.Render(Payload)!;
        var freeSection = html[html.IndexOf("Free OSS", StringComparison.Ordinal)..];

        Assert.DoesNotContain("€0", freeSection, StringComparison.Ordinal);
        Assert.Contains("<p class=\"ip-price\">Free</p>", freeSection, StringComparison.Ordinal);
    }

    [Fact]
    public void A_flat_paid_package_is_not_labelled_from()
    {
        // "From" promises a ladder. A flat package has one price.
        var payload = """
            {"cohorts":[{"name":"Flat","fromEur":"€99","isFlatPrice":true,"modules":[],"buckets":[]}],
             "onPrem":[]}
            """;

        var html = PricingTemplate.Render(payload)!;

        Assert.DoesNotContain("ip-price-lead", html, StringComparison.Ordinal);
        Assert.Contains("<p class=\"ip-price\">€99<span class=\"ip-price-unit\">a month</span></p>",
            html, StringComparison.Ordinal);
    }

    [Fact]
    public void The_on_prem_sizes_are_cards_with_their_allowance_and_price()
    {
        var html = PricingTemplate.RenderOnPrem(Payload)!;

        Assert.Contains("class=\"ip-grid ip-grid-3up\"", html, StringComparison.Ordinal);
        Assert.Contains("<h3>On-prem L</h3>", html, StringComparison.Ordinal);
        Assert.Contains("<p class=\"ip-price\">€100,000<span class=\"ip-price-unit\">a year</span></p>",
            html, StringComparison.Ordinal);
        Assert.Contains("600,000,000 line-scans a year", html, StringComparison.Ordinal);
    }

    [Fact]
    public void An_unlimited_on_prem_allowance_says_so_rather_than_going_blank()
    {
        var html = PricingTemplate.RenderOnPrem(Payload)!;

        Assert.Contains("Unlimited line-scans", html, StringComparison.Ordinal);
        Assert.Contains("€500,000", html, StringComparison.Ordinal);
    }

    [Fact]
    public void The_packages_template_leaves_on_prem_to_its_own_section()
    {
        // The page keeps self-hosted in a section with its own heading and copy. One template
        // rendering both would duplicate the sizes or dictate the page's shape.
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

        Assert.Contains("class=\"ip-grid ip-grid-5up\"", html, StringComparison.Ordinal);
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

    [Fact]
    public void A_free_lane_says_what_is_free_and_what_comes_after()
    {
        // "From €0 a month" is technically true and reads as a sales line. The lane is free up to an
        // allowance and then it is not, and both halves come from the catalogue.
        var payload = """
            {"cohorts":[{"key":"personal","name":"Student","tagline":"t","fromEur":"€0",
             "isFlatPrice":false,"modules":[],
             "buckets":[{"key":"personal","lineScansPerMonth":50000,"baseEur":"€0"},
                        {"key":"Starter","lineScansPerMonth":250000,"baseEur":"€11"}]}],
             "onPrem":[]}
            """;

        var html = PricingTemplate.Render(payload)!;

        Assert.Contains(
            "Free<span class=\"ip-price-allowance\">up to 50,000 lines a month, then from €11</span>",
            html, StringComparison.Ordinal);
        Assert.DoesNotContain("ip-price-lead", html, StringComparison.Ordinal);
    }

    [Fact]
    public void A_paid_package_says_from_its_entry_price()
    {
        var html = PricingTemplate.Render(Payload)!;

        Assert.Contains(
            "<span class=\"ip-price-lead\">From</span>€61<span class=\"ip-price-unit\">a month</span>",
            html, StringComparison.Ordinal);
    }

    [Fact]
    public void A_bucket_reads_as_one_fact_with_the_price_on_the_right()
    {
        var html = PricingTemplate.Render(Payload)!;

        Assert.Contains("<th scope=\"row\">XXS · 1M</th><td>€245</td>", html, StringComparison.Ordinal);
    }

    [Fact]
    public void The_bucket_ladder_names_its_package_for_a_screen_reader_only()
    {
        // Printed, this caption was the same sentence on all five cards; removed, a screen reader
        // lands in a table of numbers with no idea which package it belongs to.
        var html = PricingTemplate.Render(Payload)!;

        Assert.Contains(
            "<caption class=\"sr-only\">Engineering teams — price a month by line-scan allowance</caption>",
            html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(50000, "50k")]
    [InlineData(250000, "250k")]
    [InlineData(1000000, "1M")]
    [InlineData(2500000, "2.5M")]
    [InlineData(500000000, "500M")]
    [InlineData(1800000000, "1.8B")]
    public void An_allowance_that_abbreviates_exactly_is_abbreviated(long lines, string expected)
    {
        var html = PricingTemplate.Render(Bucket(lines))!;

        Assert.Contains($"<th scope=\"row\">B · {expected}</th>", html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(1234567)]
    [InlineData(1050)]
    public void An_allowance_that_does_not_is_printed_in_full(long lines)
    {
        // ★ The abbreviation exists to fit a 230px card, and a reader must never be able to take a
        // ROUNDED figure for the allowance they are charged against. No clean division, no suffix.
        var html = PricingTemplate.Render(Bucket(lines))!;

        Assert.Contains($"<th scope=\"row\">B · {lines:#,##0}</th>", html, StringComparison.Ordinal);
    }

    private static string Bucket(long lines) =>
        $$"""
          {"cohorts":[{"name":"P","fromEur":"€1","modules":[],
           "buckets":[{"key":"B","lineScansPerMonth":{{lines}},"baseEur":"€1"}]}],"onPrem":[]}
          """;
}
