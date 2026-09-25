using System.Text;
using Imprint.Publishing;

namespace Imprint.Publishing.Tests;

/// <summary>
/// The fixed five-band scale, pinned with real published surveys — the section whose whole job is to
/// teach a reader what the number means.
/// </summary>
public sealed class BandScaleTemplateTests
{
    private const string Origin = "https://app.watchdog.canine.dev";

    private const string Payload = """
        {
          "bands": [
            { "label": "Critical", "key": "critical", "floor": 0 },
            { "label": "Weak", "key": "poor", "floor": 25 },
            { "label": "Adequate", "key": "fair", "floor": 50 },
            { "label": "Strong", "key": "healthy", "floor": 70 },
            { "label": "Exemplary", "key": "exemplary", "floor": 90 }
          ],
          "reports": [
            { "display": "a/top", "score": 98, "band": "Exemplary", "reportPath": "/api/oss/a/top/report" },
            { "display": "b/mid", "score": 83, "band": "Strong", "reportPath": "/api/oss/b/mid/report" },
            { "display": "c/ok", "score": 61, "band": "Adequate", "reportPath": "/api/oss/c/ok/report" }
          ]
        }
        """;

    [Fact]
    public void Every_band_gets_a_rung_best_first_even_when_nothing_landed_in_it()
    {
        // ★ The published set is each repository's PEAK run, so the bottom of the scale is usually
        //   empty. Dropping those rungs would turn a five-band scale into a three-band one and
        //   quietly flatter the corpus.
        var html = BandScaleTemplate.Render(Payload, Origin)!;

        foreach (var word in new[] { "Exemplary", "Strong", "Adequate", "Weak", "Critical" })
        {
            Assert.Contains($">{word}</span>", html, StringComparison.Ordinal);
        }

        var order = new[] { "Exemplary", "Strong", "Adequate", "Weak", "Critical" }
            .Select(w => html.IndexOf($">{w}</span>", StringComparison.Ordinal)).ToList();
        Assert.Equal(order.OrderBy(i => i), order);

        Assert.Equal(2, Count(html, "ip-rung-empty"));
    }

    [Fact]
    public void A_bound_is_stated_the_way_the_payload_states_it()
    {
        // "from 90" is exactly what a floor means and cannot be wrong at the boundary. "90–100" or
        // "70–89" would be inventing a top end the data does not carry.
        var html = BandScaleTemplate.Render(Payload, Origin)!;

        Assert.Contains("from 90", Text(html), StringComparison.Ordinal);
        Assert.Contains("from 70", Text(html), StringComparison.Ordinal);
        Assert.Contains("under 25", Text(html), StringComparison.Ordinal);
        Assert.DoesNotContain("from 0", Text(html), StringComparison.Ordinal);
        Assert.DoesNotContain("–", html, StringComparison.Ordinal);
    }

    [Fact]
    public void The_threshold_is_an_element_of_its_own_so_it_can_be_drawn_large()
    {
        // ★ The lines between the bands are what this graphic is for, so the number is styled on
        //   its own; the word stays plain text beside it, and the rung still reads "from 90".
        var html = BandScaleTemplate.Render(Payload, Origin)!;

        Assert.Contains("from <span class=\"ip-rung-floor\">90</span>", html, StringComparison.Ordinal);
        Assert.Contains("under <span class=\"ip-rung-floor\">25</span>", html, StringComparison.Ordinal);
        Assert.Equal(5, Count(html, "ip-rung-floor"));
    }

    [Fact]
    public void The_pinned_example_is_a_real_survey_you_can_open()
    {
        var html = BandScaleTemplate.Render(Payload, Origin)!;

        Assert.Contains("https://app.watchdog.canine.dev/api/oss/a/top/report", html, StringComparison.Ordinal);
        Assert.Contains("a/top", html, StringComparison.Ordinal);
        Assert.Contains(">98</span>", html, StringComparison.Ordinal);
    }

    [Fact]
    public void No_band_table_means_no_scale_rather_than_a_remembered_one()
    {
        // ★★ The cutlines are SCORING data. If the payload does not carry them this renders nothing
        //    at all — the one thing it must never do is fall back to a copy written into this file.
        Assert.Null(BandScaleTemplate.Render("{\"reports\":[{\"display\":\"a/b\",\"score\":90}]}", Origin));
        Assert.Null(BandScaleTemplate.Render("{\"bands\":[]}", Origin));
        Assert.Null(BandScaleTemplate.Render(null, Origin));
    }

    [Fact]
    public void The_scale_carries_no_cutline_of_its_own()
    {
        // Rendered from a payload whose lines are NOT the familiar ones: if any 90/70/50/25 appears
        // in the output, it came from this file rather than from the product.
        const string moved = """
            {"bands":[{"label":"Critical","key":"critical","floor":0},
                      {"label":"Weak","key":"poor","floor":31},
                      {"label":"Adequate","key":"fair","floor":55},
                      {"label":"Strong","key":"healthy","floor":77},
                      {"label":"Exemplary","key":"exemplary","floor":93}],
             "reports":[]}
            """;

        var html = BandScaleTemplate.Render(moved, Origin)!;

        Assert.Contains("from 93", Text(html), StringComparison.Ordinal);
        Assert.Contains("under 31", Text(html), StringComparison.Ordinal);
        foreach (var remembered in new[] { "90", "70", "50", "25" })
        {
            Assert.DoesNotContain(remembered, html, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// The procurement checklist exactly as prod serves it, trimmed to the part this reads.
    /// </summary>
    /// <remarks>
    /// ★★ THIS ENVELOPE IS WHY THE LAST IFRAME STOOD FOR A DAY. The band table on
    /// <c>/api/public/reports</c> needs a promote; these cutlines have been public all along, in a
    /// document llms.txt advertises as machine-readable. The dependency was mis-scoped, not blocked.
    /// </remarks>
    private const string Checklist = """
        {
          "standard": "Code Assurance Index",
          "caiFloor": {
            "field": "minimum CAI 0-100",
            "suggestedFloor": 70,
            "bandCutlines": [ 90, 70, 50, 25 ],
            "note": "Band cutlines are 90/70/50/25; see codeassuranceindex.info/spec for the band names."
          }
        }
        """;

    [Fact]
    public void The_checklists_cutlines_draw_the_same_scale()
    {
        // Four cutlines describe five bands: the lowest band's floor is zero, and zero is not a
        // cutline. Read best-first in the payload, drawn best-first on the page.
        var html = BandScaleTemplate.Render(Checklist, Origin)!;

        foreach (var word in new[] { "Exemplary", "Strong", "Adequate", "Weak", "Critical" })
        {
            Assert.Contains($">{word}</span>", html, StringComparison.Ordinal);
        }

        Assert.Contains("from 90", Text(html), StringComparison.Ordinal);
        Assert.Contains("from 70", Text(html), StringComparison.Ordinal);
        Assert.Contains("from 50", Text(html), StringComparison.Ordinal);
        Assert.Contains("under 25", Text(html), StringComparison.Ordinal);

        // No examples in this envelope, so every rung says so rather than silently shrinking.
        Assert.Equal(5, Count(html, "ip-rung-empty"));
    }

    [Fact]
    public void The_checklist_path_carries_no_cutline_of_its_own_either()
    {
        // ★★ Same guard as the band-table path, through the other envelope: move the lines and the
        //    familiar ones must not survive anywhere in the output.
        var moved = Checklist.Replace("[ 90, 70, 50, 25 ]", "[ 93, 77, 55, 31 ]", StringComparison.Ordinal)
            .Replace("90/70/50/25", "elsewhere", StringComparison.Ordinal);

        var html = BandScaleTemplate.Render(moved, Origin)!;

        Assert.Contains("from 93", Text(html), StringComparison.Ordinal);
        Assert.Contains("under 31", Text(html), StringComparison.Ordinal);
        foreach (var remembered in new[] { "90", "70", "50", "25" })
        {
            Assert.DoesNotContain(remembered, html, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void A_rubric_of_a_different_shape_renders_nothing_rather_than_a_guess()
    {
        // ★ The five WORDS are ours; the LINES are the product's. When the count of lines stops
        //   agreeing with the count of words, the rubric has changed shape underneath us, and
        //   drawing it against five remembered words would be inventing a scale, not publishing one.
        foreach (var lines in new[] { "[ 90, 70, 50 ]", "[ 90, 70, 50, 25, 10 ]", "[]" })
        {
            var odd = Checklist.Replace("[ 90, 70, 50, 25 ]", lines, StringComparison.Ordinal);
            Assert.Null(BandScaleTemplate.Render(odd, Origin));
        }
    }

    [Fact]
    public void The_products_own_band_table_wins_when_both_are_present()
    {
        // The reports feed's words come from the rubric; the list in this template is a stand-in for
        // them. Where the real thing is available it must be what renders.
        var both = "{\"caiFloor\":{\"bandCutlines\":[90,70,50,25]},"
            + "\"bands\":[{\"label\":\"Bottom\",\"key\":\"critical\",\"floor\":0},"
            + "{\"label\":\"Top\",\"key\":\"exemplary\",\"floor\":42}],\"reports\":[]}";

        var html = BandScaleTemplate.Render(both, Origin)!;

        Assert.Contains(">Top</span>", html, StringComparison.Ordinal);
        Assert.Contains("from 42", Text(html), StringComparison.Ordinal);
        Assert.DoesNotContain("Exemplary", html, StringComparison.Ordinal);
    }

    /// <summary>What a reader sees: the markup with its tags removed.</summary>
    private static string Text(string html)
    {
        var text = new StringBuilder(html.Length);
        var inTag = false;
        foreach (var c in html)
        {
            if (c == '<') { inTag = true; }
            else if (c == '>') { inTag = false; }
            else if (!inTag) { text.Append(c); }
        }

        return text.ToString();
    }

    private static int Count(string haystack, string needle)
    {
        var n = 0;
        for (var i = haystack.IndexOf(needle, StringComparison.Ordinal); i >= 0;
             i = haystack.IndexOf(needle, i + needle.Length, StringComparison.Ordinal)) { n++; }

        return n;
    }
}
