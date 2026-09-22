using Imprint.Publishing;

namespace Imprint.Publishing.Tests;

/// <summary>
/// The richest card on the estate — the one Assay's home page leads with — and every one of its
/// facts used to live in a shadow root.
/// </summary>
public sealed class SurveyDetailTemplateTests
{
    private const string Origin = "https://app.watchdog.canine.dev";

    private const string Payload = """
        {
          "cohort": null,
          "items": [
            { "owner": "ruben-rasmussen", "name": "auth", "display": "ruben-rasmussen/auth",
              "band": "Exemplary", "bandHex": "#0E5C3A", "publishedAt": "2026-06-15T09:12:00+00:00",
              "bestScore": 97.6, "firstScore": 62, "delta": 35.6, "scanCount": 18,
              "codeHealth": 98.2, "architecture": 99.3, "maturity": 98.9,
              "productionReadiness": 96.2, "securityCompliance": 100,
              "domainModelling": 100, "eventDriven": null, "eventSourcing": null,
              "costApprox": "~€380,000", "busFactor": 1, "authorCount": 3, "productionLoc": 35954,
              "reportUrl": "/api/oss/ruben-rasmussen/auth/report?run=abc" }
          ]
        }
        """;

    [Fact]
    public void The_score_its_band_and_every_measured_lens_are_text()
    {
        var html = SurveyDetailTemplate.Render(Payload, Origin)!;

        Assert.Contains("<span class=\"ip-survey-repo\">auth</span>", html, StringComparison.Ordinal);
        Assert.Contains("<span class=\"ip-survey-by\">by ruben-rasmussen</span>", html, StringComparison.Ordinal);
        // ★★ 98, NOT 97.6. This feed carries `bestScore: 97.6` where /api/public/reports carries
        //    `score: 98` for the same run, and the strip and the hero sat one section apart printing
        //    both. The published figure is the whole number.
        Assert.Contains(">98</span>", html, StringComparison.Ordinal);
        // Not ">97.6<": the tenth survives in the bar GEOMETRY, which is a drawing rather than a
        // figure. What must not appear is a printed 97.6 anywhere a reader reads a number.
        Assert.DoesNotContain(">97.6", html, StringComparison.Ordinal);
        Assert.DoesNotContain("97.6 out of", html, StringComparison.Ordinal);
        Assert.Contains("Exemplary", html, StringComparison.Ordinal);
        // ★ A BAR AND ITS NUMBER, not a number alone. The bar is what makes "one lens is dragging
        //   this down" visible without reading five figures — which is why the widget drew them and
        //   why the bake looked so much poorer than the island it replaced.
        Assert.Contains("<th scope=\"row\">Code health</th>", html, StringComparison.Ordinal);
        Assert.Contains(">98</td>", html, StringComparison.Ordinal);
        Assert.Contains("<th scope=\"row\">Security</th>", html, StringComparison.Ordinal);
        Assert.Contains(">100</td>", html, StringComparison.Ordinal);
        Assert.Contains("Domain modelling", html, StringComparison.Ordinal);

        // Every measured lens gets a bar, and the fill is the score.
        Assert.Equal(6, Occurrences(html, "ip-lens-fill"));
        Assert.Contains("width:98.2%", html, StringComparison.Ordinal);
    }

    /// <summary>The same payload with the band table the verdicts feed now carries.</summary>
    private static string Banded => Payload.Replace(
        "\"cohort\": null,",
        """
        "cohort": null,
        "bands": [
          { "label": "Critical", "key": "critical", "floor": 0 },
          { "label": "Weak", "key": "poor", "floor": 25 },
          { "label": "Adequate", "key": "fair", "floor": 50 },
          { "label": "Strong", "key": "healthy", "floor": 70 },
          { "label": "Exemplary", "key": "exemplary", "floor": 90 }
        ],
        """,
        StringComparison.Ordinal);

    [Fact]
    public void Each_lens_is_inked_in_its_own_band_rather_than_the_headlines()
    {
        // ★ SIX GREEN BARS UNDER A GREEN SCORE SAY ONLY "this repository is green". A fair lens
        //   among five strong ones is the entire reason the table is drawn rather than listed, and
        //   it is invisible if every bar takes the headline's colour.
        var html = SurveyDetailTemplate.Render(
            Banded.Replace("\"productionReadiness\": 96.2", "\"productionReadiness\": 61", StringComparison.Ordinal),
            Origin)!;

        Assert.Contains("ip-lens-fill fill-exemplary", html, StringComparison.Ordinal);
        Assert.Contains("ip-lens-fill fill-fair", html, StringComparison.Ordinal);
        Assert.Contains("ip-lens-value ink-fair", html, StringComparison.Ordinal);
    }

    [Fact]
    public void With_no_band_table_no_bar_claims_a_band()
    {
        // ★★ THE CUTLINES ARE SCORING DATA AND THEY COME FROM THE PAYLOAD. Deciding that a 96 is
        //    "Strong" needs the LINES; with no band table the honest answer is the accent, not a
        //    band word inferred from numbers remembered in this repository.
        var html = SurveyDetailTemplate.Render(Payload, Origin)!;

        Assert.DoesNotContain("ip-lens-fill fill-", html, StringComparison.Ordinal);
        Assert.DoesNotContain("ip-lens-value ink-", html, StringComparison.Ordinal);
        Assert.DoesNotContain("ip-cai-ladder", html, StringComparison.Ordinal);
    }

    [Fact]
    public void The_band_table_turns_the_flat_bar_into_the_real_ladder()
    {
        var html = SurveyDetailTemplate.Render(Banded, Origin)!;

        Assert.Contains("ip-cai-ladder", html, StringComparison.Ordinal);
        Assert.Contains("ip-cai-rung-exemplary", html, StringComparison.Ordinal);
        Assert.Contains("ip-cai-pin", html, StringComparison.Ordinal);
        Assert.DoesNotContain("ip-cai-track", html, StringComparison.Ordinal);
    }

    [Fact]
    public void A_lens_the_run_did_not_measure_renders_no_row_at_all()
    {
        // A placeholder row is worse than a missing one, because it looks measured.
        var html = SurveyDetailTemplate.Render(Payload, Origin)!;

        Assert.DoesNotContain("Event-driven", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Event sourcing", html, StringComparison.Ordinal);
    }

    [Fact]
    public void The_trend_is_a_sentence_because_a_sparkline_cannot_be_read()
    {
        var html = SurveyDetailTemplate.Render(Payload, Origin)!;

        // ★ THE DELTA IS THE DIFFERENCE BETWEEN THE TWO NUMBERS THE READER CAN SEE. Taking it from
        //   the raw values printed "62 → 98 … up 35.6", which is an arithmetic error to anyone who
        //   checks it, and the arc is printed precisely so that it will be checked.
        Assert.Contains(">62</span>", html, StringComparison.Ordinal);
        Assert.Contains("up 36 over 18 scans", html, StringComparison.Ordinal);
    }

    [Fact]
    public void A_single_scan_gets_no_trend_because_there_is_none()
    {
        var html = SurveyDetailTemplate.Render(
            Payload.Replace("\"scanCount\": 18", "\"scanCount\": 1", StringComparison.Ordinal), Origin)!;

        Assert.DoesNotContain("ip-trend", html, StringComparison.Ordinal);
    }

    [Fact]
    public void A_falling_score_says_down_rather_than_up_a_negative_number()
    {
        var html = SurveyDetailTemplate.Render(
            Payload.Replace("\"firstScore\": 62", "\"firstScore\": 99", StringComparison.Ordinal), Origin)!;

        Assert.Contains("down 1 over 18 scans", html, StringComparison.Ordinal);
        Assert.DoesNotContain("up -", html, StringComparison.Ordinal);
    }

    [Fact]
    public void The_date_is_labelled_what_the_payload_calls_it()
    {
        // ★ The field is `publishedAt`. Relabelling somebody else's timestamp to the word that reads
        //   better — "Measured" — is how a page ends up making a claim nothing backs.
        var html = SurveyDetailTemplate.Render(Payload, Origin)!;

        Assert.Contains("<th scope=\"row\">Published</th><td>15 June 2026 · 35,954 lines</td>",
            html, StringComparison.Ordinal);
        Assert.DoesNotContain("Measured", html, StringComparison.Ordinal);
    }

    [Fact]
    public void What_the_run_recorded_is_shown_and_what_it_did_not_is_omitted()
    {
        var html = SurveyDetailTemplate.Render(Payload, Origin)!;
        Assert.Contains("~€380,000", html, StringComparison.Ordinal);
        Assert.Contains("1 of 3 developers", html, StringComparison.Ordinal);

        var bare = SurveyDetailTemplate.Render(
            Payload.Replace("\"costApprox\": \"~€380,000\"", "\"costApprox\": null", StringComparison.Ordinal)
                   .Replace("\"busFactor\": 1", "\"busFactor\": 0", StringComparison.Ordinal), Origin)!;

        Assert.DoesNotContain("Rebuild cost", bare, StringComparison.Ordinal);
        Assert.DoesNotContain("Bus factor", bare, StringComparison.Ordinal);
        Assert.Contains("Published", bare, StringComparison.Ordinal);
    }

    [Fact]
    public void An_unparseable_date_drops_the_row_rather_than_printing_a_timestamp()
    {
        var html = SurveyDetailTemplate.Render(
            Payload.Replace("\"2026-06-15T09:12:00+00:00\"", "\"whenever\"", StringComparison.Ordinal), Origin)!;

        Assert.DoesNotContain("whenever", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Published", html, StringComparison.Ordinal);
    }

    [Fact]
    public void The_report_link_is_absolute_against_the_origin_it_came_from()
    {
        var html = SurveyDetailTemplate.Render(Payload, Origin)!;

        Assert.Contains("https://app.watchdog.canine.dev/api/oss/ruben-rasmussen/auth/report?run=abc",
            html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("{\"items\":[]}")]
    [InlineData("{\"items\":[{\"display\":\"a/b\"}]}")]
    public void A_payload_with_no_usable_survey_renders_nothing(string? payload) =>
        Assert.Null(SurveyDetailTemplate.Render(payload, Origin));

    [Fact]
    public void Text_from_the_payload_is_escaped()
    {
        var html = SurveyDetailTemplate.Render(
            Payload.Replace("~€380,000", "<img src=x>", StringComparison.Ordinal), Origin)!;

        Assert.DoesNotContain("<img", html, StringComparison.Ordinal);
        Assert.Contains("&lt;img", html, StringComparison.Ordinal);
    }

    private static int Occurrences(string haystack, string needle)
    {
        var n = 0;
        for (var i = haystack.IndexOf(needle, StringComparison.Ordinal); i >= 0;
             i = haystack.IndexOf(needle, i + needle.Length, StringComparison.Ordinal)) { n++; }

        return n;
    }
}
