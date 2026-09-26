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

    // ── The plans row (RenderPlans) ──────────────────────────────────────────────
    // Two catalogues: the one the product serves today (every paid package priced by bucket, two
    // free lanes with ladders) and one shaped like the announced change of model (flat prices with
    // an included allowance, no ladders). The row has to read right for both, because the page
    // follows the catalogue without anyone rewriting it.

    private const string TodayCatalogue = """
        {
          "currency": "EUR", "distribution": null,
          "cohorts": [
            { "key": "personal", "name": "Student", "tagline": "Your own projects.", "fromEur": "€0",
              "isFlatPrice": false, "modules": ["Core survey"],
              "buckets": [ { "key": "personal", "lineScansPerMonth": 50000, "baseEur": "€0" },
                           { "key": "Starter", "lineScansPerMonth": 250000, "baseEur": "€11" } ] },
            { "key": "freelancer", "name": "Freelancer", "tagline": "For one-person shops.", "fromEur": "€24",
              "isFlatPrice": false, "modules": ["Core survey"],
              "buckets": [ { "key": "Starter", "lineScansPerMonth": 250000, "baseEur": "€24" },
                           { "key": "XXS", "lineScansPerMonth": 1000000, "baseEur": "€95" } ] },
            { "key": "teams", "name": "Engineering teams", "tagline": "The whole product.", "fromEur": "€61",
              "isFlatPrice": false, "modules": ["Core survey", "Agent-fix loop"],
              "buckets": [ { "key": "Starter", "lineScansPerMonth": 250000, "baseEur": "€61" },
                           { "key": "XXS", "lineScansPerMonth": 1000000, "baseEur": "€245" } ] },
            { "key": "enterprise", "name": "Enterprise", "tagline": "Under your governance.", "fromEur": "€124",
              "isFlatPrice": false, "modules": ["Core survey", "Enterprise SSO"],
              "buckets": [ { "key": "Starter", "lineScansPerMonth": 250000, "baseEur": "€124" },
                           { "key": "XXS", "lineScansPerMonth": 1000000, "baseEur": "€495" } ] },
            { "key": "freeoss", "name": "Free OSS", "tagline": "Open source.", "fromEur": "€0",
              "isFlatPrice": false, "modules": ["Core survey"],
              "buckets": [ { "key": "oss", "lineScansPerMonth": 100000, "baseEur": "€0" },
                           { "key": "Starter", "lineScansPerMonth": 250000, "baseEur": "€18" } ] }
          ],
          "onPrem": [
            { "key": "L", "name": "On-prem L", "blurb": "", "lineScansPerYear": 600000000, "pricePerYearEur": 100000.00 },
            { "key": "XL", "name": "On-prem XL", "blurb": "", "lineScansPerYear": 1800000000, "pricePerYearEur": 200000.00 },
            { "key": "XXL", "name": "On-prem XXL", "blurb": "", "lineScansPerYear": null, "pricePerYearEur": 500000.00 }
          ]
        }
        """;

    // Deliberately NOT in price order: the row, not the catalogue, puts the cheapest first.
    private const string NewModelCatalogue = """
        {
          "cohorts": [
            { "key": "professional", "name": "Professional", "tagline": "For teams of any size.", "fromEur": "€1,995",
              "isFlatPrice": true, "includedLocScans": 15000000, "modules": ["Core survey", "Enterprise SSO"], "buckets": [] },
            { "key": "contributor", "name": "Contributor Unlimited", "tagline": "For one person.", "fromEur": "€95",
              "isFlatPrice": true, "includedLocScans": 1000000, "modules": ["Core survey"], "buckets": [] },
            { "key": "contributor-free", "name": "Contributor Free", "tagline": "For one person, free.", "fromEur": "€0",
              "isFlatPrice": true, "includedLocScans": 250000, "modules": ["Core survey"], "buckets": [] }
          ],
          "onPrem": [
            { "key": "L", "name": "On-prem L", "lineScansPerYear": 600000000, "pricePerYearEur": 100000 },
            { "key": "XL", "name": "On-prem XL", "lineScansPerYear": 1800000000, "pricePerYearEur": 200000 },
            { "key": "XXL", "name": "On-prem XXL", "lineScansPerYear": null, "pricePerYearEur": 500000 }
          ]
        }
        """;

    [Fact]
    public void Plans_are_ordered_cheapest_first_with_on_prem_last()
    {
        // The two free lanes tie at €0 and keep the catalogue's order between them.
        var html = PricingTemplate.RenderPlans(TodayCatalogue)!;

        AssertInOrder(html, "<h3>Student</h3>", "<h3>Free OSS</h3>", "<h3>Freelancer</h3>",
            "<h3>Engineering teams</h3>", "<h3>Enterprise</h3>", "<h3>On-prem</h3>");
    }

    [Fact]
    public void The_new_model_is_ordered_the_same_way_whatever_order_the_catalogue_sends()
    {
        var html = PricingTemplate.RenderPlans(NewModelCatalogue)!;

        AssertInOrder(html, "<h3>Contributor Free</h3>", "<h3>Contributor Unlimited</h3>",
            "<h3>Professional</h3>", "<h3>On-prem</h3>");
    }

    [Fact]
    public void A_bucket_priced_plan_shows_its_entry_price_and_what_that_price_buys()
    {
        var html = PricingTemplate.RenderPlans(TodayCatalogue)!;

        Assert.Contains(
            "<p class=\"ip-price\"><span class=\"ip-price-lead\">From</span>€61<span class=\"ip-price-unit\">a month</span></p>"
            + "<p class=\"ip-plan-allowance\">For up to 250,000 line-scans a month</p>",
            html, StringComparison.Ordinal);
    }

    [Fact]
    public void A_bucket_ladder_is_folded_under_see_every_price()
    {
        var html = PricingTemplate.RenderPlans(TodayCatalogue)!;

        Assert.Contains("<details class=\"ip-plan-prices\"><summary>See every price</summary>", html, StringComparison.Ordinal);
        Assert.Contains("<th scope=\"row\">1M line-scans</th><td>€245</td>", html, StringComparison.Ordinal);
        Assert.Contains(
            "<caption class=\"sr-only\">Engineering teams: price a month by line-scan allowance</caption>",
            html, StringComparison.Ordinal);
    }

    [Fact]
    public void A_free_lane_says_how_far_free_goes_and_what_comes_after_it()
    {
        var html = PricingTemplate.RenderPlans(TodayCatalogue)!;

        Assert.Contains(
            "<p class=\"ip-price\">Free</p><p class=\"ip-plan-allowance\">Up to 50,000 line-scans a month, then from €11</p>",
            html, StringComparison.Ordinal);
        Assert.Contains("<th scope=\"row\">50k line-scans</th><td>Free</td>", html, StringComparison.Ordinal);
    }

    [Fact]
    public void A_flat_plan_shows_one_price_and_its_allowance_with_nothing_to_fold()
    {
        // "From" would promise a ladder, and a fold would open onto nothing.
        var html = PricingTemplate.RenderPlans(NewModelCatalogue)!;

        Assert.Contains(
            "<p class=\"ip-price\">€95<span class=\"ip-price-unit\">a month</span></p>"
            + "<p class=\"ip-plan-allowance\">Up to 1,000,000 line-scans a month</p>",
            html, StringComparison.Ordinal);
        Assert.Contains("<p class=\"ip-price\">€1,995<span class=\"ip-price-unit\">a month</span></p>",
            html, StringComparison.Ordinal);
        Assert.DoesNotContain("ip-price-lead", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<details", html, StringComparison.Ordinal);
    }

    [Fact]
    public void A_free_plan_with_a_stated_allowance_says_free_and_how_far()
    {
        var html = PricingTemplate.RenderPlans(NewModelCatalogue)!;

        Assert.Contains(
            "<p class=\"ip-price\">Free</p><p class=\"ip-plan-allowance\">Up to 250,000 line-scans a month</p>",
            html, StringComparison.Ordinal);
    }

    [Fact]
    public void The_modules_are_the_included_list()
    {
        var html = PricingTemplate.RenderPlans(TodayCatalogue)!;

        Assert.Contains(
            "<p class=\"ip-plan-label\">Included</p><ul class=\"ip-plan-included\"><li>Core survey</li><li>Agent-fix loop</li></ul>",
            html, StringComparison.Ordinal);
    }

    [Fact]
    public void On_prem_is_one_card_priced_at_its_smallest_size_with_the_larger_sizes_listed()
    {
        var html = PricingTemplate.RenderPlans(TodayCatalogue)!;

        Assert.Contains(
            "<div class=\"ip-plan ip-plan-onprem\"><h3>On-prem</h3>"
            + "<p class=\"ip-price\">€100,000<span class=\"ip-price-unit\">a year</span></p>"
            + "<p class=\"ip-plan-allowance\">On-prem L: 600,000,000 line-scans a year</p>",
            html, StringComparison.Ordinal);
        Assert.Contains("<th scope=\"row\">On-prem XL · 1.8B a year</th><td>€200,000</td>", html, StringComparison.Ordinal);
        Assert.Contains("<th scope=\"row\">On-prem XXL · unlimited</th><td>€500,000</td>", html, StringComparison.Ordinal);
        Assert.Equal(1, Occurrences(html, "ip-plan-onprem"));
    }

    [Fact]
    public void Without_on_prem_rows_there_is_no_on_prem_card()
    {
        var html = PricingTemplate.RenderPlans(
            "{\"cohorts\":[{\"name\":\"P\",\"fromEur\":\"€1\",\"modules\":[],\"buckets\":[]}],\"onPrem\":[]}")!;

        Assert.DoesNotContain("ip-plan-onprem", html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("{\"cohorts\":[]}")]
    public void A_catalogue_with_no_packages_renders_no_plans_row(string? payload)
    {
        Assert.Null(PricingTemplate.RenderPlans(payload));
    }

    [Fact]
    public void A_price_with_no_digits_sorts_last_rather_than_passing_for_the_cheapest()
    {
        var html = PricingTemplate.RenderPlans(
            "{\"cohorts\":[{\"name\":\"Odd\",\"fromEur\":\"ask\",\"modules\":[],\"buckets\":[]},"
            + "{\"name\":\"Cheap\",\"fromEur\":\"€5\",\"modules\":[],\"buckets\":[]}]}")!;

        AssertInOrder(html, "<h3>Cheap</h3>", "<h3>Odd</h3>");
    }

    [Fact]
    public void Plan_text_from_the_catalogue_is_escaped()
    {
        var html = PricingTemplate.RenderPlans(
            "{\"cohorts\":[{\"name\":\"<script>x</script>\",\"tagline\":\"<b>t</b>\",\"fromEur\":\"€1\","
            + "\"modules\":[\"<i>m</i>\"],\"buckets\":[]}]}")!;

        Assert.DoesNotContain("<script>", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<b>", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<i>", html, StringComparison.Ordinal);
        Assert.Contains("&lt;script&gt;", html, StringComparison.Ordinal);
    }

    [Fact]
    public void The_publisher_routes_the_plans_layout_and_the_original_layout_is_unchanged()
    {
        // The live page keeps the original widget until an editor swaps it, so the original
        // template must render exactly as before alongside the new one.
        var plans = PrerenderTemplates.Render(PricingTemplate.PlansName, TodayCatalogue)!;
        var original = PrerenderTemplates.Render(PricingTemplate.Name, TodayCatalogue)!;

        Assert.Contains("class=\"ip-plans\"", plans, StringComparison.Ordinal);
        Assert.Contains("class=\"ip-grid ip-grid-5up\"", original, StringComparison.Ordinal);
        Assert.DoesNotContain("ip-plans", original, StringComparison.Ordinal);
    }

    private static void AssertInOrder(string html, params string[] fragments)
    {
        var positions = fragments.Select(f => html.IndexOf(f, StringComparison.Ordinal)).ToList();

        Assert.DoesNotContain(-1, positions);
        Assert.Equal(positions.OrderBy(p => p), positions);
    }

    private static int Occurrences(string html, string fragment) =>
        (html.Length - html.Replace(fragment, "", StringComparison.Ordinal).Length) / fragment.Length;

    private static string Bucket(long lines) =>
        $$"""
          {"cohorts":[{"name":"P","fromEur":"€1","modules":[],
           "buckets":[{"key":"B","lineScansPerMonth":{{lines}},"baseEur":"€1"}]}],"onPrem":[]}
          """;
}
