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

        // ★ THE REPOSITORY IS TWO LINES, NOT ONE TOKEN. `owner/name` as a single heading wraps
        //   mid-word — this sheet refuses `break-word` sheet-wide, with a measured reason — so a long
        //   repository took three lines in a 280px card and pushed that card's score, ladder and link
        //   below its neighbours'. Both facts are still text, which is what this test is for.
        Assert.Contains("<span class=\"ip-survey-repo\">auth</span>", html, StringComparison.Ordinal);
        Assert.Contains("<span class=\"ip-survey-by\">by ruben-rasmussen</span>", html, StringComparison.Ordinal);
        Assert.Contains(">98</span>", html, StringComparison.Ordinal);
        Assert.Contains("Exemplary", html, StringComparison.Ordinal);
        Assert.Contains("2 lenses measured · C#", html, StringComparison.Ordinal);
    }

    [Fact]
    public void The_headline_score_is_the_whole_number_the_product_publishes()
    {
        // ★★ THIS TEST USED TO ASSERT THE OPPOSITE, AND THE OPPOSITE PUT TWO NUMBERS FOR ONE
        //    REPOSITORY ON ONE PAGE. /api/public/reports carries `score: 98` where
        //    /api/public/verdicts carries `bestScore: 97.6` for the same run, so the strip and the
        //    hero — one section apart — printed 98 and 97.6 and read as two measurements. The
        //    published figure is the whole number: the gallery, the report and the island card all
        //    print it, and the tenth is not a precision this index claims.
        var html = ScoreCardTemplate.Render(Reports, Origin)!;

        Assert.Contains(">98</span>", html, StringComparison.Ordinal);
        Assert.Contains(">62</span>", html, StringComparison.Ordinal);
        // Not "61.5": the tenth survives in the bar GEOMETRY, a drawing rather than a figure.
        Assert.DoesNotContain(">61.5", html, StringComparison.Ordinal);
        Assert.DoesNotContain("61.5 out of", html, StringComparison.Ordinal);
        Assert.DoesNotContain("98.0", html, StringComparison.Ordinal);
    }

    [Fact]
    public void The_band_word_picks_the_sites_own_themed_hue_rather_than_the_payloads_hex()
    {
        // The payload carries ONE hex and this site is themed light and dark. A light-mode hex on a
        // dark card is the kind of thing that passes review and fails a contrast check.
        var html = ScoreCardTemplate.Render(Reports, Origin)!;

        Assert.Contains("ip-chip-exemplary", html, StringComparison.Ordinal);
        Assert.Contains("ink-exemplary", html, StringComparison.Ordinal);
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

        // And no "Showing 1 of 3,112" beside it: that line exists to stop a STRIP reading as "four
        // surveys exist", which one card never does — next to a hero it is a sentence about the
        // widget rather than about the product.
        Assert.DoesNotContain("Showing", html, StringComparison.Ordinal);
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

    private const string Composition = """
        {
          "items": [
            { "repo": "a/b", "reportUrl": "/api/oss/a/b/report",
              "latest": { "brilliant": 70.8, "fine": 26.7, "slop": 2.5, "scoredFiles": 24 },
              "history": [
                { "at": "2026-05-01T00:00:00+00:00", "brilliant": 10, "fine": 80, "slop": 10, "scoredFiles": 20 },
                { "at": "2026-06-01T00:00:00+00:00", "brilliant": 45, "fine": 55, "slop": 0, "scoredFiles": 22 },
                { "at": "2026-07-01T00:00:00+00:00", "brilliant": 70.8, "fine": 26.7, "slop": 2.5, "scoredFiles": 24 }
              ] }
          ]
        }
        """;

    [Fact]
    public void The_mix_draws_a_column_PER_SCAN_because_the_trend_is_the_argument()
    {
        // ★ The section's copy promises "the trend shows it falling". A single latest figure cannot
        //   show a trend — the first replacement drew one bar per repository and said nothing was
        //   changing, which is the opposite of the argument the section makes.
        var html = CompositionTemplate.Render(Composition, Origin)!;

        Assert.Equal(3, Occurrences(html, "ip-mix-col"));
        Assert.Contains("ip-mix-chart", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Every_column_carries_its_numbers_in_the_markup()
    {
        // A stacked bar with no text is a picture of data. This is data with a picture over it.
        var html = CompositionTemplate.Render(Composition, Origin)!;

        Assert.Contains("1 May 2026: 10 % brilliant, 80 % fine, 10 % slop", html, StringComparison.Ordinal);
        Assert.Contains("1 Jul 2026: 70.8 % brilliant, 26.7 % fine, 2.5 % slop", html, StringComparison.Ordinal);
        Assert.Contains("sr-only", html, StringComparison.Ordinal);
    }

    [Fact]
    public void The_latest_mix_is_printed_in_words_because_that_is_what_a_reader_quotes()
    {
        var html = CompositionTemplate.Render(Composition, Origin)!;

        Assert.Contains("70.8 % brilliant", html, StringComparison.Ordinal);
        Assert.Contains("26.7 % fine", html, StringComparison.Ordinal);
        Assert.Contains("2.5 % slop", html, StringComparison.Ordinal);
        Assert.Contains("24 files scored", html, StringComparison.Ordinal);
    }

    [Fact]
    public void A_zero_band_draws_no_segment_at_all()
    {
        // A zero-height segment still shows as a sliver of a category that is not there.
        var html = CompositionTemplate.Render(Composition, Origin)!;

        // The middle scan reached 0 % slop; the other two did not.
        Assert.Equal(2, Occurrences(html, "ip-mix-seg-slop"));
        Assert.Equal(3, Occurrences(html, "ip-mix-seg-brilliant"));
    }

    /// <summary>
    /// ★★ THE CURATION, AND WHY IT IS HERE AS WELL AS IN THE PRODUCT. The framed view only ever
    /// showed repositories with all three bands above zero — "a repo with no slop proves nothing
    /// about measuring slop, and one with no brilliant reads as a hit piece". The first replacement
    /// dropped that rule and published a section answering "how much of your codebase is slop?" with
    /// four repositories at 0 %, which is worse than publishing nothing.
    /// </summary>
    [Fact]
    public void A_repository_with_no_slop_is_not_shown_because_it_proves_nothing()
    {
        const string flattering = """
            {"items":[
              {"repo":"all/brilliant","history":[{"brilliant":100,"fine":0,"slop":0}]},
              {"repo":"no/brilliant","history":[{"brilliant":0,"fine":80,"slop":20}]},
              {"repo":"a/real-mix","history":[{"brilliant":60,"fine":30,"slop":10}]}]}
            """;

        var html = CompositionTemplate.Render(flattering, Origin)!;

        Assert.Contains("a/real-mix", html, StringComparison.Ordinal);
        Assert.DoesNotContain("all/brilliant", html, StringComparison.Ordinal);
        Assert.DoesNotContain("no/brilliant", html, StringComparison.Ordinal);
    }

    [Fact]
    public void A_repository_with_no_history_is_not_drawn_as_an_empty_chart()
    {
        Assert.Null(CompositionTemplate.Render(
            "{\"items\":[{\"repo\":\"a/b\",\"history\":[]}]}", Origin));
        Assert.Null(CompositionTemplate.Render("{\"items\":[]}", Origin));
        Assert.Null(CompositionTemplate.Render(null, Origin));
    }

    [Fact]
    public void A_style_block_is_removed_from_the_drawing_not_carried_into_the_page()
    {
        // ★★ A <style> inside an SVG that is INLINED into the page is not scoped to the drawing — it
        //   is ordinary document CSS, so a figure fetched from another service could restyle, hide or
        //   impersonate anything on the page. The product's map really does ship one, and every
        //   selector in it happens to start with .wd-c4 — but that is a fact about today's payload,
        //   not something a guard can rely on. Remove the element; the site's own sheet paints the
        //   classes.
        const string svg = """
            <svg xmlns="http://www.w3.org/2000/svg" class="wd-c4"><style>body{display:none}</style>
            <title>Map</title><text class="name">Gateway</text></svg>
            """;

        var html = ArchitectureSvgTemplate.Render(svg)!;

        Assert.DoesNotContain("<style", html, StringComparison.Ordinal);
        Assert.DoesNotContain("display:none", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<title", html, StringComparison.Ordinal);
        Assert.Contains(">Gateway<", html, StringComparison.Ordinal);
        Assert.Contains("class=\"name\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public void An_svg_that_is_unsafe_for_any_other_reason_is_still_refused()
    {
        // Stripping two known-bad elements must not be mistaken for sanitising: everything else still
        // goes through the guard exactly as before.
        Assert.Null(ArchitectureSvgTemplate.Render(
            """<svg xmlns="http://www.w3.org/2000/svg"><script>alert(1)</script></svg>"""));
        Assert.Null(ArchitectureSvgTemplate.Render(
            """<svg xmlns="http://www.w3.org/2000/svg"><text onclick="alert(1)">x</text></svg>"""));
        Assert.Null(ArchitectureSvgTemplate.Render(
            """<svg xmlns="http://www.w3.org/2000/svg"><foreignObject><b>x</b></foreignObject></svg>"""));
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
