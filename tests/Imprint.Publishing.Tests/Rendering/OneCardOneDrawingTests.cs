using Imprint.Publishing;

namespace Imprint.Publishing.Tests.Rendering;

/// <summary>
/// The published survey card is ONE object, and every template that draws it draws the same one.
/// </summary>
/// <remarks>
/// <para>★★ THIS TEST EXISTS BECAUSE FOUR TEMPLATES DREW FOUR CARDS. The owner found it by looking
/// at the estate: watchdog's hero and its "real reports" strip printed a name, a score and the words
/// "6 lenses measured"; assay's card printed the six numbers, the line they moved along and what the
/// run recorded; canine.dev's printed a headline and a flat bar; the island on codeassuranceindex.info
/// printed all of it. One measurement, four drawings — which reads as four products.</para>
/// <para>The shared body lives in <see cref="ScoreVisuals"/>. This asserts that the templates fed a
/// payload carrying the fields actually USE it, so a future template cannot quietly go back to
/// printing a count where the card shows the numbers.</para>
/// </remarks>
public sealed class OneCardOneDrawingTests
{
    private const string Origin = "https://app.watchdog.canine.dev";

    private const string Verdicts = """
        {
          "cohort": null,
          "total": 3149,
          "bands": [
            { "label": "Critical", "key": "critical", "floor": 0 },
            { "label": "Weak", "key": "poor", "floor": 25 },
            { "label": "Adequate", "key": "fair", "floor": 50 },
            { "label": "Strong", "key": "healthy", "floor": 70 },
            { "label": "Exemplary", "key": "exemplary", "floor": 90 }
          ],
          "items": [
            { "owner": "ruben-rasmussen", "name": "auth", "display": "ruben-rasmussen/auth",
              "band": "Exemplary", "bandHex": "#0E5C3A", "publishedAt": "2026-06-15T09:12:00+00:00",
              "bestScore": 97.6, "firstScore": 62, "scanCount": 18,
              "series": [62, 70, 81, 88, 92, 97.6],
              "codeHealth": 98.2, "architecture": 99.3, "maturity": 98.9,
              "productionReadiness": 96.2, "securityCompliance": 100,
              "costApprox": "~€380,000", "busFactor": 1, "authorCount": 3, "productionLoc": 35954,
              "reportUrl": "/api/oss/ruben-rasmussen/auth/report?run=a" },
            { "owner": "acme", "name": "api", "display": "acme/api",
              "band": "Strong", "bandHex": "#3C8F59", "publishedAt": "2026-06-01T09:12:00+00:00",
              "bestScore": 74, "firstScore": 51, "scanCount": 7,
              "series": [51, 60, 68, 74],
              "codeHealth": 80, "architecture": 61, "maturity": 77,
              "productionReadiness": 70, "securityCompliance": 90,
              "costApprox": "~€90,000", "busFactor": 2, "authorCount": 5, "productionLoc": 12000,
              "reportUrl": "/api/oss/acme/api/report?run=b" }
          ]
        }
        """;

    /// <summary>Every part of the card a reader recognises it by.</summary>
    private static readonly (string Marker, string What)[] TheCard =
    [
        ("ip-survey-repo", "the repository name"),
        ("ip-survey-by", "the owner, as a byline"),
        ("ip-chip", "the band, as a chip"),
        ("ip-cai-kicker", "the CAI kicker"),
        ("ip-cai-score", "the score"),
        ("ip-cai-ladder", "the banded ladder"),
        ("ip-cai-spark", "the trend line"),
        ("ip-trend", "the arc, as a sentence"),
        ("ip-lens-fill", "a bar per lens"),
        ("ip-lens-value", "each lens's own number"),
    ];

    [Fact]
    public void The_hero_draws_the_whole_card()
    {
        var html = PrerenderTemplates.Render(SurveyDetailTemplate.Name, Verdicts, Origin + "/api/public/verdicts")!;

        foreach (var (marker, what) in TheCard)
        {
            Assert.True(html.Contains(marker, StringComparison.Ordinal), $"the hero card is missing {what}");
        }
    }

    [Fact]
    public void The_strip_draws_the_same_card_not_a_thinner_one()
    {
        // ★ THIS IS THE ONE THAT WAS WRONG. The strip read a feed with no lens values and no series,
        //   so it printed "6 lenses measured" — a COUNT — where the hero showed the six numbers.
        var html = PrerenderTemplates.Render(SurveyDetailTemplate.StripName, Verdicts, Origin + "/api/public/verdicts")!;

        foreach (var (marker, what) in TheCard)
        {
            Assert.True(html.Contains(marker, StringComparison.Ordinal), $"a strip card is missing {what}");
        }

        Assert.DoesNotContain("lenses measured", html, StringComparison.Ordinal);
    }

    [Fact]
    public void The_strip_frames_itself_with_the_size_of_the_set()
    {
        // Four cards with no denominator read as "four surveys exist".
        var html = PrerenderTemplates.Render(SurveyDetailTemplate.StripName, Verdicts, Origin + "/api/public/verdicts")!;

        Assert.Contains("Showing 2 of <strong>3,149</strong> published surveys", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Each_card_in_the_strip_is_its_own_repository()
    {
        var html = PrerenderTemplates.Render(SurveyDetailTemplate.StripName, Verdicts, Origin + "/api/public/verdicts")!;

        Assert.Contains(">auth</span>", html, StringComparison.Ordinal);
        Assert.Contains(">api</span>", html, StringComparison.Ordinal);
        Assert.Contains("by ruben-rasmussen", html, StringComparison.Ordinal);
        Assert.Contains("by acme", html, StringComparison.Ordinal);
    }

    [Fact]
    public void A_weak_lens_is_inked_differently_from_a_strong_one()
    {
        // The whole reason the lenses are bars: acme/api's architecture is 61 among scores of 70-90,
        // and a reader should see that without reading five figures.
        var html = PrerenderTemplates.Render(SurveyDetailTemplate.StripName, Verdicts, Origin + "/api/public/verdicts")!;

        Assert.Contains("ip-lens-fill fill-exemplary", html, StringComparison.Ordinal);
        Assert.Contains("ip-lens-fill fill-fair", html, StringComparison.Ordinal);
    }
}
