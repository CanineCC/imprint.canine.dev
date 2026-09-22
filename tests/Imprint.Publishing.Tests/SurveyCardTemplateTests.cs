using Imprint.Publishing;

namespace Imprint.Publishing.Tests;

/// <summary>
/// One NAMED repository's survey — the card a company homepage leads with.
/// </summary>
public sealed class SurveyCardTemplateTests
{
    private const string Origin = "https://app.watchdog.canine.dev";

    /// <summary>The live payload's shape, trimmed to what the card reads.</summary>
    private const string Payload = """
        {
          "rubricVersion": "rubric-2026.09.15",
          "commit": "4a287a2b0ee8148c31324cf560a57941f2697b4a",
          "analyzableProjects": 6,
          "productionLoc": 13014,
          "rebuildCost": "~€550,000",
          "headlineScore": 86.81073415417612,
          "lenses": [
            { "lens": "codeHealth", "score": 94.77 },
            { "lens": "architecture", "score": 84.50 },
            { "lens": "securityCompliance", "score": 88.21 }
          ]
        }
        """;

    [Fact]
    public void The_score_the_lenses_and_what_the_run_recorded_are_all_in_the_markup()
    {
        var html = SurveyCardTemplate.Render(Payload, Origin)!;

        // ★★ 87, NOT 86.8 — the whole number the product publishes, the same on every card. This
        //    file is the third to assert a tenth that the gallery, the report and the island card all
        //    round away; a repository read 98 in one section and 97.6 in the next because of it.
        Assert.Contains(">87</span>", html, StringComparison.Ordinal);
        Assert.Contains("Code health", html, StringComparison.Ordinal);

        // ★ THE LENSES ARE BARS NOW, not a list of numbers — the same body every other card draws.
        //   A bar is what makes one weak lens visible beside five strong ones.
        Assert.Contains("ip-lens-fill", html, StringComparison.Ordinal);
        Assert.Contains(">95</td>", html, StringComparison.Ordinal);
        Assert.Contains("13,014 lines", html, StringComparison.Ordinal);
        Assert.Contains("~€550,000", html, StringComparison.Ordinal);
        Assert.Contains("rubric-2026.09.15", html, StringComparison.Ordinal);
    }

    /// <summary>
    /// ★★ NO BAND WORD, EVER. This payload carries no cutlines, and a band is a cutline question.
    /// Inferring one from a remembered 90/70/50/25 is exactly what BandScaleTemplate refuses to do,
    /// for the same reason: the lines are scoring data and a fourth hand-written copy is how a scale
    /// drifts. The score is printed; the word is not invented.
    /// </summary>
    [Fact]
    public void No_band_is_inferred_from_a_score()
    {
        var html = SurveyCardTemplate.Render(Payload, Origin)!;

        foreach (var word in new[] { "Exemplary", "Strong", "Adequate", "Weak", "Critical", "ip-band" })
        {
            Assert.DoesNotContain(word, html, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// ★ The lenses render in the PRODUCT's order, not the payload's. A card whose rows reorder
    /// because a scan happened to serialise differently reads as two different cards.
    /// </summary>
    [Fact]
    public void Lenses_render_in_the_products_order_whatever_order_the_payload_used()
    {
        const string shuffled = """
            {"headlineScore":50,
             "lenses":[{"lens":"securityCompliance","score":1},
                       {"lens":"codeHealth","score":2},
                       {"lens":"architecture","score":3}]}
            """;

        var html = SurveyCardTemplate.Render(shuffled, Origin)!;
        var order = new[] { "Code health", "Architecture", "Security" }
            .Select(l => html.IndexOf(l, StringComparison.Ordinal)).ToList();

        Assert.DoesNotContain(-1, order);
        Assert.Equal(order.OrderBy(i => i), order);
    }

    /// <summary>
    /// What the payload does not carry is not filled in. A zero prints nothing rather than reading
    /// as a measurement of zero.
    /// </summary>
    [Fact]
    public void A_missing_fact_drops_its_row_rather_than_printing_a_zero()
    {
        var html = SurveyCardTemplate.Render("{\"headlineScore\":72}", Origin)!;

        Assert.Contains(">72</span>", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Rebuild cost", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Projects", html, StringComparison.Ordinal);
        Assert.DoesNotContain("0 lines", html, StringComparison.Ordinal);
        Assert.DoesNotContain("ip-survey-lenses", html, StringComparison.Ordinal);
    }

    [Fact]
    public void No_score_means_no_card_rather_than_an_empty_one()
    {
        Assert.Null(SurveyCardTemplate.Render("{\"productionLoc\":13014}", Origin));
        Assert.Null(SurveyCardTemplate.Render("{}", Origin));
        Assert.Null(SurveyCardTemplate.Render(null, Origin));
    }

    /// <summary>The bar cannot run past its track, whatever the payload says.</summary>
    [Fact]
    public void The_fill_is_clamped_to_the_track()
    {
        Assert.Contains("width:100%", SurveyCardTemplate.Render("{\"headlineScore\":140}", Origin)!, StringComparison.Ordinal);
        Assert.Contains("width:0%", SurveyCardTemplate.Render("{\"headlineScore\":-5}", Origin)!, StringComparison.Ordinal);
    }

    /// <summary>
    /// ★★ THE REPOSITORY IS CHOSEN BY THE URL. A bake is cached by (url, template) and cannot see
    /// props, so two cards wanting two repositories MUST fetch two different URLs or they share one
    /// cache entry and one renders the other's survey. This pins the manifest's prerender shape.
    /// </summary>
    [Fact]
    public void The_widget_parameterises_the_repository_through_the_url()
    {
        var manifest = System.IO.File.ReadAllText(
            System.IO.Path.Combine(RepoRoot(), "widgets", "manifest.json"));

        using var doc = System.Text.Json.JsonDocument.Parse(manifest);
        // ★ The manifest's root is an ARRAY. TryGetProperty on an array THROWS rather than
        //   returning false, so the kind has to be asked first.
        var widgets = doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object
                      && doc.RootElement.TryGetProperty("widgets", out var w)
            ? w
            : doc.RootElement;
        var card = widgets.EnumerateArray().Single(x => x.GetProperty("tag").GetString() == "wd-survey-card");

        Assert.Equal("{base}/api/public/oss/{owner}/{name}/evidence", card.GetProperty("prerender").GetString());
        Assert.Equal("survey-card", card.GetProperty("prerenderTemplate").GetString());
        Assert.Equal("", card.GetProperty("bundle").GetString());

        var props = card.GetProperty("props").EnumerateArray().Select(p => p.GetProperty("name").GetString()).ToList();
        Assert.Contains("owner", props);
        Assert.Contains("name", props);
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "widgets", "manifest.json")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("could not find the repository root");
    }
}
