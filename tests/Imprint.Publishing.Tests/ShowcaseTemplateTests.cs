using Imprint.Publishing;

namespace Imprint.Publishing.Tests;

/// <summary>
/// The three surfaces that replaced an iframe: published-survey cards, real findings, and the file
/// mix. Each one's whole reason to exist is that a crawler, a language model and a reader without
/// JavaScript can now see what the frame kept to itself — so the assertions are about the TEXT being
/// in the markup, not about the markup being well formed.
/// </summary>
public sealed class ShowcaseTemplateTests
{
    private const string Origin = "https://app.watchdog.canine.dev";

    private const string Reports = """
        {
          "totals": { "repositories": 6785, "publishedSurveys": 3112 },
          "matched": 3112, "capped": true, "curated": true,
          "reports": [
            { "owner": "ruben-rasmussen", "name": "auth", "display": "ruben-rasmussen/auth",
              "score": 98, "band": "Exemplary", "bandHex": "#0E5C3A", "primaryLanguage": "C#",
              "lenses": ["codeHealth", "architecture"], "reportPath": "/api/oss/ruben-rasmussen/auth/report" },
            { "owner": "a", "name": "b", "display": "a/b", "score": 61.5, "band": "Adequate",
              "bandHex": "#AD8217", "primaryLanguage": null, "lenses": ["codeHealth"],
              "reportPath": "/api/oss/a/b/report" }
          ]
        }
        """;

    [Fact]
    public void A_survey_card_carries_the_repository_the_score_and_the_band_as_text()
    {
        var html = ScoreCardTemplate.Render(Reports, Origin)!;

        Assert.Contains("<h3>ruben-rasmussen/auth</h3>", html, StringComparison.Ordinal);
        Assert.Contains(">98</span>", html, StringComparison.Ordinal);
        Assert.Contains("Exemplary", html, StringComparison.Ordinal);
        Assert.Contains("2 lenses measured · C#", html, StringComparison.Ordinal);
    }

    [Fact]
    public void A_score_keeps_one_decimal_only_when_it_has_one()
    {
        var html = ScoreCardTemplate.Render(Reports, Origin)!;

        Assert.Contains(">98</span>", html, StringComparison.Ordinal);
        Assert.Contains(">61.5</span>", html, StringComparison.Ordinal);
        Assert.DoesNotContain("98.0", html, StringComparison.Ordinal);
    }

    [Fact]
    public void The_band_word_picks_the_sites_own_themed_hue_rather_than_the_payloads_hex()
    {
        // The payload carries ONE hex and this site is themed light and dark. A light-mode hex on a
        // dark card is the kind of thing that passes review and fails a contrast check.
        var html = ScoreCardTemplate.Render(Reports, Origin)!;

        Assert.Contains("ip-band-exemplary", html, StringComparison.Ordinal);
        Assert.DoesNotContain("#0E5C3A", html, StringComparison.Ordinal);
    }

    [Fact]
    public void An_unknown_band_word_falls_back_to_the_payload_hex_but_only_if_it_is_one()
    {
        var known = ScoreCardTemplate.Render(
            Reports.Replace("\"Exemplary\"", "\"Spectacular\"", StringComparison.Ordinal), Origin)!;

        Assert.Contains("color:#0E5C3A", known, StringComparison.Ordinal);

        // ★ The hex lands in a style attribute, which HTML-escaping does not protect: none of the
        //   characters in `;background:url(...)` need escaping, so only the SHAPE check stops it.
        var hostile = ScoreCardTemplate.Render(
            Reports.Replace("\"Exemplary\"", "\"Spectacular\"", StringComparison.Ordinal)
                .Replace("#0E5C3A", "red;background:url(//evil/x)", StringComparison.Ordinal), Origin)!;

        Assert.DoesNotContain("evil", hostile, StringComparison.Ordinal);
        Assert.DoesNotContain("background:url", hostile, StringComparison.Ordinal);
    }

    [Fact]
    public void A_report_link_is_absolute_against_the_origin_the_payload_came_from()
    {
        var html = ScoreCardTemplate.Render(Reports, Origin)!;

        Assert.Contains("https://app.watchdog.canine.dev/api/oss/ruben-rasmussen/auth/report",
            html, StringComparison.Ordinal);
    }

    [Fact]
    public void A_javascript_url_in_the_payload_never_becomes_a_link()
    {
        var html = ScoreCardTemplate.Render(
            Reports.Replace("/api/oss/a/b/report", "javascript:alert(1)", StringComparison.Ordinal), Origin)!;

        Assert.DoesNotContain("javascript:", html, StringComparison.Ordinal);
    }

    [Fact]
    public void The_strip_shows_four_and_the_full_feed_shows_everything()
    {
        var many = Reports.Replace(
            "\"reports\": [",
            "\"reports\": [" + string.Concat(Enumerable.Range(0, 6).Select(i =>
                $"{{\"display\":\"x/{i}\",\"score\":50,\"band\":\"Adequate\",\"lenses\":[],\"reportPath\":\"/r/{i}\"}},")),
            StringComparison.Ordinal);

        Assert.Equal(4, Occurrences(ScoreCardTemplate.Render(many, Origin)!, "ip-survey\">"));
        Assert.Equal(8, Occurrences(ScoreCardTemplate.RenderFull(many, Origin)!, "ip-survey\">"));
    }

    [Fact]
    public void One_card_is_one_card_and_says_so_in_its_grid()
    {
        // The hero sets its copy beside a SINGLE card. Four there is not more evidence, it is a
        // broken layout — and auto-fill would give one card a 280px track in a 1100px section.
        var html = ScoreCardTemplate.RenderOne(Reports, Origin)!;

        Assert.Equal(1, Occurrences(html, "ip-survey\">"));
        Assert.Contains("ip-grid ip-grid-1up", html, StringComparison.Ordinal);
        Assert.DoesNotContain("a/b", html, StringComparison.Ordinal);
    }

    [Fact]
    public void A_curated_list_says_how_much_it_is_a_selection_from()
    {
        // Four cards with no frame around them read as "four surveys exist".
        var html = ScoreCardTemplate.Render(Reports, Origin)!;

        Assert.Contains("3,112</strong> published surveys", html, StringComparison.Ordinal);
    }

    [Fact]
    public void The_list_makes_no_claim_about_the_order_it_is_given()
    {
        // ★ It said "best first" until the live rendering was looked at. The feed runs 98, 86, 83 …
        //   59, 51 and then 89, 81, 76 — a descending curated head with a tail the product appends
        //   for its own reasons. The sentence was a claim about the payload that the payload does not
        //   make, printed directly beside the numbers that disprove it.
        var html = ScoreCardTemplate.RenderFull(Reports, Origin)!;

        Assert.DoesNotContain("best first", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("highest", html, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("{\"reports\":[]}")]
    public void A_payload_with_no_surveys_renders_nothing(string? payload) =>
        Assert.Null(ScoreCardTemplate.Render(payload, Origin));

    private const string Findings = """
        {
          "items": [
            { "repo": "leytonoday/Atlas-API", "reportUrl": "/api/oss/leytonoday/Atlas-API/report",
              "shown": 2, "total": 78, "more": 76,
              "findings": [
                { "lens": "EventDriven", "lensLabel": "Event-Driven", "dim": "ED4",
                  "title": "Dual write (no outbox)", "file": "src/Users/Create.cs", "line": 9 },
                { "lens": "DomainModelling", "lensLabel": "Domain Modelling", "dim": "DM1",
                  "title": "Aggregate holds a reference to another aggregate", "file": "src/Feature.cs", "line": 43 }
              ] }
          ]
        }
        """;

    [Fact]
    public void A_finding_publishes_its_lens_its_sentence_and_the_file_it_names()
    {
        var html = FindingsTemplate.Render(Findings, Origin)!;

        Assert.Contains("Event-Driven · ED4", html, StringComparison.Ordinal);
        Assert.Contains("Dual write (no outbox)", html, StringComparison.Ordinal);
        Assert.Contains("src/Users/Create.cs:9", html, StringComparison.Ordinal);
        Assert.Contains("2 of 78 findings", html, StringComparison.Ordinal);
    }

    [Fact]
    public void A_repository_with_no_findings_is_not_shown_as_an_empty_card()
    {
        Assert.Null(FindingsTemplate.Render("{\"items\":[{\"repo\":\"a/b\",\"findings\":[]}]}", Origin));
    }

    [Fact]
    public void Findings_text_from_the_payload_is_escaped()
    {
        var html = FindingsTemplate.Render(
            Findings.Replace("Dual write (no outbox)", "<img src=x onerror=alert(1)>", StringComparison.Ordinal),
            Origin)!;

        Assert.DoesNotContain("<img", html, StringComparison.Ordinal);
        Assert.Contains("&lt;img", html, StringComparison.Ordinal);
    }

    private const string Insights = """
        {
          "items": [
            { "display": "a/b", "reportUrl": "/api/oss/a/b/report",
              "fileQuality": [ { "brilliant": 10, "fine": 80, "slop": 10 },
                               { "brilliant": 70.8, "fine": 29.2, "slop": 0 } ] }
          ]
        }
        """;

    [Fact]
    public void The_file_mix_reads_the_LAST_entry_because_it_is_a_series()
    {
        // Element zero is the mix the repository had when it was FIRST surveyed — for a repository
        // that improved, precisely the number it would least like shown, and it would look right.
        var html = CompositionTemplate.Render(Insights, Origin)!;

        Assert.Contains("70.8 % brilliant", html, StringComparison.Ordinal);
        Assert.DoesNotContain("10 % brilliant", html, StringComparison.Ordinal);
    }

    [Fact]
    public void The_file_mix_prints_its_shares_as_words_not_only_as_a_bar()
    {
        var html = CompositionTemplate.Render(Insights, Origin)!;

        Assert.Contains("29.2 % fine", html, StringComparison.Ordinal);
        Assert.Contains("0 % slop", html, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"70.8 % brilliant, 29.2 % fine, 0 % slop\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public void A_zero_share_draws_no_segment_at_all()
    {
        // A zero-width span still takes its border and reads as a sliver of a category that isn't there.
        var html = CompositionTemplate.Render(Insights, Origin)!;

        Assert.DoesNotContain("ip-mix-seg-slop", html, StringComparison.Ordinal);
        Assert.Contains("ip-mix-key-slop", html, StringComparison.Ordinal);
    }

    [Fact]
    public void A_repository_with_no_measured_mix_is_skipped_rather_than_drawn_empty()
    {
        Assert.Null(CompositionTemplate.Render(
            "{\"items\":[{\"display\":\"a/b\",\"fileQuality\":[{\"brilliant\":null,\"fine\":null,\"slop\":null}]}]}",
            Origin));
    }

    [Fact]
    public void An_svg_carrying_a_style_block_is_refused_rather_than_repaired()
    {
        // ★ This is why the architecture map is not inlined yet. A <style> inside an SVG that is
        //   inlined into the page is NOT scoped to the drawing — it restyles the whole document, so a
        //   figure fetched from another service could hide or impersonate anything on the page. The
        //   guard's allowlist excludes it deliberately, and the right fix is for the drawing to carry
        //   presentation attributes, not for this to relax.
        const string svg = """<svg xmlns="http://www.w3.org/2000/svg"><style>body{display:none}</style><text>x</text></svg>""";

        Assert.Null(ArchitectureSvgTemplate.Render(svg));
    }

    [Fact]
    public void An_svg_of_plain_shapes_and_text_is_inlined_with_its_labels_intact()
    {
        const string svg = """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 10 10"><text x="1" y="2">Gateway</text></svg>""";

        var html = ArchitectureSvgTemplate.Render(svg)!;

        Assert.Contains("<svg", html, StringComparison.Ordinal);
        Assert.Contains(">Gateway<", html, StringComparison.Ordinal);
        Assert.Contains("ip-svg", html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("<html><body>404</body></html>")]
    [InlineData("{\"error\":\"nope\"}")]
    public void A_payload_that_is_not_a_drawing_publishes_no_figure(string? payload) =>
        Assert.Null(ArchitectureSvgTemplate.Render(payload));

    [Fact]
    public void An_unknown_template_name_renders_nothing_rather_than_something_unexpected() =>
        Assert.Null(PrerenderTemplates.Render("no-such-template", Reports, Origin));

    [Fact]
    public void The_dispatcher_resolves_relative_links_against_the_url_it_fetched_from()
    {
        // ★ Not a widget prop: the bake is keyed by (url, template) and cannot see props, so a prop
        //   that changed the output would produce ONE fragment shared by instances that disagreed.
        var html = PrerenderTemplates.Render(
            ScoreCardTemplate.Name, Reports, "https://app.watchdog.canine.dev/api/public/reports")!;

        Assert.Contains("https://app.watchdog.canine.dev/api/oss/", html, StringComparison.Ordinal);
    }

    [Fact]
    public void A_non_https_fetch_url_yields_no_links_rather_than_insecure_ones()
    {
        var html = PrerenderTemplates.Render(ScoreCardTemplate.Name, Reports, "http://insecure.example/x")!;

        Assert.DoesNotContain("<a href", html, StringComparison.Ordinal);
    }

    private static int Occurrences(string haystack, string needle)
    {
        var count = 0;
        for (var i = haystack.IndexOf(needle, StringComparison.Ordinal); i >= 0;
             i = haystack.IndexOf(needle, i + needle.Length, StringComparison.Ordinal))
        {
            count++;
        }

        return count;
    }
}
