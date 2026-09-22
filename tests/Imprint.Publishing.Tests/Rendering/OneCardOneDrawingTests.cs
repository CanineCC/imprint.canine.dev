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

    /// <summary>
    /// ★★ A CARD THAT NAMES A REPOSITORY NEVER DRAWS A DIFFERENT ONE. The feed filters on `?repo=`;
    /// a deployment that predates that parameter ignores it and answers with the whole cohort. Left
    /// to the server alone, a card for one repository would then silently show whichever survey
    /// happened to rank first — on a front page, under a heading naming the project.
    /// </summary>
    [Fact]
    public void A_url_naming_a_repository_draws_that_one_even_if_the_server_ignored_the_filter()
    {
        var html = PrerenderTemplates.Render(
            SurveyDetailTemplate.Name, Verdicts, Origin + "/api/public/verdicts?repo=acme/api")!;

        Assert.Contains(">api</span>", html, StringComparison.Ordinal);
        Assert.Contains("by acme", html, StringComparison.Ordinal);
        Assert.DoesNotContain("ruben-rasmussen", html, StringComparison.Ordinal);
    }

    [Fact]
    public void A_named_repository_the_payload_does_not_hold_renders_nothing_rather_than_the_wrong_one()
    {
        // Nothing is visible — the widget publishes its fallback and somebody notices. The wrong
        // survey under the right heading is not visible at all.
        Assert.Null(PrerenderTemplates.Render(
            SurveyDetailTemplate.Name, Verdicts, Origin + "/api/public/verdicts?repo=CanineCC/kennel.canine.dev"));
    }

    [Fact]
    public void A_url_naming_no_repository_still_leads_with_the_first_of_the_cohort()
    {
        var html = PrerenderTemplates.Render(
            SurveyDetailTemplate.Name, Verdicts, Origin + "/api/public/verdicts")!;

        Assert.Contains(">auth</span>", html, StringComparison.Ordinal);
    }

    /// <summary>The band table at its own address — what /api/public/reports publishes.</summary>
    private const string Context = """
        {
          "matched": 3149,
          "bands": [
            { "label": "Critical", "key": "critical", "floor": 0 },
            { "label": "Weak", "key": "poor", "floor": 25 },
            { "label": "Adequate", "key": "fair", "floor": 50 },
            { "label": "Strong", "key": "healthy", "floor": 70 },
            { "label": "Exemplary", "key": "exemplary", "floor": 90 }
          ],
          "reports": []
        }
        """;

    /// <summary>
    /// The same payload with NO band table and no total — which is exactly what prod serves today.
    /// </summary>
    /// <remarks>
    /// ★ SPELLED OUT, NOT DERIVED BY STRING SURGERY FROM THE OTHER FIXTURE. The first attempt
    /// `.Replace`d the band block out of <see cref="Verdicts"/>, the whitespace did not match, the
    /// replace did nothing and the test asserted the opposite of what it meant — a fixture that
    /// quietly fails to change anything is worse than no fixture.
    /// </remarks>
    private const string Bandless = """
        {
          "cohort": null,
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

    /// <summary>
    /// ★★ THE CUTLINES MAY COME FROM A SECOND ADDRESS, AND THAT IS WHY EVERY CARD CAN DRAW THE SAME
    /// SCALE. A card's own feed publishes the score, the history and the lenses; the lines deciding
    /// which word and which hue that score gets belong to the rubric and are published separately.
    /// Given one fetch, a card had to go without one half — and every baked card on the estate drew
    /// a flat grey bar for want of four numbers that were public at a different address the whole
    /// time. The rule is unchanged: no cutline is written in this repository.
    /// </summary>
    [Fact]
    public void A_feed_with_no_band_table_draws_the_real_ladder_from_the_context()
    {
        var withoutContext = PrerenderTemplates.Render(
            SurveyDetailTemplate.Name, Bandless, Origin + "/api/public/verdicts")!;
        var withContext = PrerenderTemplates.Render(
            SurveyDetailTemplate.Name, Bandless, Origin + "/api/public/verdicts", Context)!;

        Assert.Contains("ip-cai-track", withoutContext, StringComparison.Ordinal);
        Assert.DoesNotContain("ip-cai-ladder", withoutContext, StringComparison.Ordinal);

        Assert.Contains("ip-cai-ladder", withContext, StringComparison.Ordinal);
        Assert.Contains("ip-cai-rung-exemplary", withContext, StringComparison.Ordinal);
        Assert.Contains("ip-lens-fill fill-exemplary", withContext, StringComparison.Ordinal);
    }

    [Fact]
    public void The_strips_denominator_comes_from_whichever_feed_publishes_it()
    {
        // This feed calls it `total`; the reports feed calls the same fact `matched`.
        var html = PrerenderTemplates.Render(
            SurveyDetailTemplate.StripName, Bandless, Origin + "/api/public/verdicts", Context)!;

        Assert.Contains("Showing 2 of <strong>3,149</strong> published surveys", html, StringComparison.Ordinal);
    }

    /// <summary>
    /// ★ THE SUBJECT'S OWN TABLE WINS. It was computed for this reading; a context fetched
    /// separately could in principle be a revision ahead.
    /// </summary>
    [Fact]
    public void A_feed_that_publishes_its_own_bands_ignores_the_context()
    {
        var shifted = Context.Replace("\"floor\": 90", "\"floor\": 95", StringComparison.Ordinal);
        var html = PrerenderTemplates.Render(
            SurveyDetailTemplate.Name, Verdicts, Origin + "/api/public/verdicts", shifted)!;

        // 97.6 is Exemplary under the subject's own 90 floor, and the pin sits where that says.
        Assert.Contains("ip-cai-ladder", html, StringComparison.Ordinal);
        Assert.Contains("Exemplary", html, StringComparison.Ordinal);
    }

    /// <summary>
    /// ★★ AND THE EVIDENCE-FED CARD GETS ITS BAND WORD THE SAME WAY. That payload is served
    /// byte-exact so a reader can compare its digest against a signed delivery, so a band table can
    /// never be added to it — which is why canine.dev's cards printed a bare score and a flat bar
    /// while the card beside them on another site drew the real scale.
    /// </summary>
    [Fact]
    public void The_evidence_card_draws_the_same_scale_from_the_same_context()
    {
        const string evidence = """
            { "rubricVersion": "rubric-2026.09.15", "productionLoc": 13014, "rebuildCost": "~€550,000",
              "analyzableProjects": 6, "headlineScore": 86.81,
              "lenses": [{"lens":"codeHealth","score":94.8},{"lens":"architecture","score":84.5}] }
            """;

        var html = PrerenderTemplates.Render(
            SurveyCardTemplate.Name, evidence,
            Origin + "/api/public/oss/code-assurance-initiative/CodeAssuranceIndex/evidence", Context)!;

        Assert.Contains("ip-cai-ladder", html, StringComparison.Ordinal);
        Assert.Contains("ip-chip ip-chip-healthy", html, StringComparison.Ordinal);   // 86.81 sits above the 70 floor
        Assert.Contains("Strong", html, StringComparison.Ordinal);
        Assert.Contains("ip-cai-score ink-healthy", html, StringComparison.Ordinal);
        Assert.Contains(">CodeAssuranceIndex</span>", html, StringComparison.Ordinal);
    }

    [Fact]
    public void With_no_context_the_evidence_card_still_refuses_to_invent_a_band()
    {
        const string evidence = """{ "headlineScore": 86.81, "lenses": [] }""";

        var html = PrerenderTemplates.Render(
            SurveyCardTemplate.Name, evidence, Origin + "/api/public/oss/a/b/evidence")!;

        Assert.DoesNotContain("ip-chip", html, StringComparison.Ordinal);
        Assert.DoesNotContain("ip-cai-ladder", html, StringComparison.Ordinal);
        Assert.Contains("ip-cai-track", html, StringComparison.Ordinal);
    }
}
