using System.Globalization;
using System.Text;
using System.Text.Json;
using static Imprint.Publishing.TemplateJson;

namespace Imprint.Publishing;

/// <summary>
/// One published survey in full — the score, its band, the trend, every lens it measured and what
/// the run says about the codebase — rendered from <c>/api/public/verdicts</c>.
/// </summary>
/// <remarks>
/// <para>★ THE RICHEST CARD ON THE ESTATE, AND ALL OF IT WAS IN A SHADOW ROOT. Assay's home page
/// leads with a real published survey — "this is what an appraisal looks like" — and every one of
/// its twenty-seven facts, from the lens scores to the rebuild cost, reached no crawler and no
/// language model. The page argued for evidence with evidence nobody outside a browser could read.
/// </para>
/// <para><b>One fetch, because the card is one thing.</b> The band, the date, the series and the
/// lenses now all come from the same payload. Reading a second endpoint to label a card the first
/// one had already described is how two numbers on one card come to disagree.</para>
/// <para>★ <b>The date is labelled "Published", not "Measured".</b> That is the field the payload
/// carries, and it is not obviously the same thing. Relabelling somebody else's timestamp to the
/// word that reads better is exactly how a page ends up making a claim nothing backs.</para>
/// <para>What the payload does not carry is not invented: a missing lens, a zero bus factor, an
/// absent cost estimate each render NOTHING rather than a placeholder or a dash.</para>
/// </remarks>
public static class SurveyDetailTemplate
{
    public const string Name = "survey-detail";

    /// <summary>The five lenses every survey measures, in the order the product shows them.</summary>
    private static readonly (string Key, string Label)[] Lenses =
    [
        ("codeHealth", "Code health"),
        ("architecture", "Architecture"),
        ("maturity", "Maturity"),
        ("productionReadiness", "Readiness"),
        ("securityCompliance", "Security"),
        ("domainModelling", "Domain modelling"),
        ("eventDriven", "Event-driven"),
        ("eventSourcing", "Event sourcing"),
    ];

    /// <summary>The whole card, for the page that leads with ONE published survey.</summary>
    public static string? Render(string? json, string? origin) => Render(json, origin, 1);

    /// <summary>
    /// The same card, four across, for a strip that says "here are real reports".
    /// </summary>
    /// <remarks>
    /// ★★ THE SAME CARD, NOT A SECOND DESIGN. The strip used to be drawn by score-cards off
    /// <c>/api/public/reports</c>, which carries no lens values and no series — so it printed
    /// "6 lenses measured" where the card it was copied from showed the six numbers and the line
    /// they moved along. Two templates drawing one object is how a reader comes to believe there
    /// are two products; the verdicts feed has every field, so the strip draws the card.
    /// </remarks>
    public const string StripName = "survey-details";

    public static string? RenderStrip(string? json, string? origin) => Render(json, origin, StripCount);

    /// <summary>Four fills a row and leaves none dangling.</summary>
    private const int StripCount = 4;

    private static string? Render(string? json, string? origin, int take)
    {
        if (Root(json) is not { } root)
        {
            return null;
        }

        var items = Array(root, "items")
            .Where(i => Str(i, "display").Length > 0 && Number(i, "bestScore") is not null)
            .Take(take)
            .ToList();

        if (items.Count == 0)
        {
            return null;
        }

        if (take == 1)
        {
            return Card(root, items[0], origin);
        }

        var html = new StringBuilder();
        html.Append("<div class=\"ip-grid ip-grid-4up\">");
        foreach (var item in items)
        {
            html.Append(Card(root, item, origin));
        }

        html.Append("</div>");

        // The corpus total is the honest frame around a curated strip — without it, four cards read
        // as "four surveys exist".
        if (Number(root, "total") is { } total && total > items.Count)
        {
            html.Append("<div class=\"ip-prose\"><p>Showing ")
                .Append(Esc(Group(items.Count))).Append(" of <strong>")
                .Append(Esc(Group(total)))
                .Append("</strong> published surveys — every one of them readable in full.</p></div>");
        }

        return html.ToString();
    }

    private static string Card(JsonElement root, JsonElement item, string? origin)
    {
        var score = Number(item, "bestScore") ?? 0;
        var band = Str(item, "band");
        var key = BandKey(band);
        var hex = key is null ? HexColour(Str(item, "bandHex")) : null;
        var style = hex is not null ? $" style=\"color:{hex}\"" : "";

        var html = new StringBuilder();
        html.Append("<div class=\"ip-stack ip-survey ip-survey-detail\">");
        html.Append(ScoreVisuals.Head(item, band, key, style));
        html.Append(ScoreVisuals.ScoreLine(score, key, style));
        html.Append(ScoreVisuals.Ladder(root, score, hex, key));
        html.Append(ScoreVisuals.Sparkline(item, hex, key));
        html.Append(ScoreVisuals.Trend(item, score, key));

        var measured = Lenses
            .Select(l => (l.Label, Value: Number(item, l.Key)))
            .Where(l => l.Value is not null)
            .Select(l => (l.Label, l.Value!.Value))
            .ToList();

        html.Append(ScoreVisuals.Lenses(root, measured, hex, Str(item, "display")));

        var facts = new List<(string Label, string Html)>();
        if (Date(Str(item, "publishedAt")) is { } published)
        {
            facts.Add(("Published", Number(item, "productionLoc") is { } loc && loc > 0
                ? $"{Esc(published)} · {Esc(Group(loc))} lines"
                : Esc(published)));
        }

        if (Str(item, "costApprox") is { Length: > 0 } cost)
        {
            facts.Add(("Rebuild cost", Esc(cost)));
        }

        if (Number(item, "busFactor") is { } bus && bus > 0)
        {
            facts.Add(("Bus factor", Number(item, "authorCount") is { } authors && authors > 0
                ? $"{Esc(Group(bus))} of {Esc(Group(authors))} developers"
                : Esc(Group(bus))));
        }

        html.Append(ScoreVisuals.Facts(facts, Str(item, "display")));

        if (Link(origin, Str(item, "reportUrl")) is { } href)
        {
            html.Append("<p class=\"ip-survey-link\"><a href=\"").Append(Esc(href))
                .Append("\">Read the whole survey →</a></p>");
        }

        return html.Append("</div>").ToString();
    }

    /// <summary>An ISO timestamp as a plain date, or null — never the raw string.</summary>
    private static string? Date(string value) =>
        DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var when)
            ? when.UtcDateTime.ToString("d MMMM yyyy", CultureInfo.InvariantCulture)
            : null;

    private static string? BandKey(string band) => band switch
    {
        "Exemplary" => "exemplary",
        "Strong" => "healthy",
        "Adequate" => "fair",
        "Weak" => "poor",
        "Critical" => "critical",
        _ => null,
    };
}
