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

        Assert.Contains("from 90", html, StringComparison.Ordinal);
        Assert.Contains("from 70", html, StringComparison.Ordinal);
        Assert.Contains("under 25", html, StringComparison.Ordinal);
        Assert.DoesNotContain("from 0", html, StringComparison.Ordinal);
        Assert.DoesNotContain("–", html, StringComparison.Ordinal);
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

        Assert.Contains("from 93", html, StringComparison.Ordinal);
        Assert.Contains("under 31", html, StringComparison.Ordinal);
        foreach (var remembered in new[] { "90", "70", "50", "25" })
        {
            Assert.DoesNotContain(remembered, html, StringComparison.Ordinal);
        }
    }

    private static int Count(string haystack, string needle)
    {
        var n = 0;
        for (var i = haystack.IndexOf(needle, StringComparison.Ordinal); i >= 0;
             i = haystack.IndexOf(needle, i + needle.Length, StringComparison.Ordinal)) { n++; }

        return n;
    }
}
