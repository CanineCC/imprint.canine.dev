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

    public static string? Render(string? json, string? origin)
    {
        if (Root(json) is not { } root)
        {
            return null;
        }

        var item = Array(root, "items").FirstOrDefault(i => Str(i, "display").Length > 0);
        if (item.ValueKind != JsonValueKind.Object || Number(item, "bestScore") is not { } score)
        {
            return null;
        }

        var band = Str(item, "band");
        var key = BandKey(band);
        var hex = key is null ? HexColour(Str(item, "bandHex")) : null;
        var style = hex is not null ? $" style=\"color:{hex}\"" : "";

        var html = new StringBuilder();
        html.Append("<div class=\"ip-stack ip-survey ip-survey-detail\">");
        html.Append(ScoreVisuals.Head(item, band, key, style));
        html.Append(ScoreVisuals.ScoreLine(score, key, style));

        html.Append(ScoreVisuals.Ladder(root, score, hex, key));

        // ★ THE LINE AND THE SENTENCE, not one or the other. The note that used to stand here said
        //   "a sparkline is a picture of this fact and cannot be read" and shipped the numbers alone.
        //   The shape is a DIFFERENT fact from the endpoints: 62 to 98 in one jump and 62 to 98 by
        //   steady work are the same two numbers and not the same story.
        html.Append(ScoreVisuals.Sparkline(item, hex, key));

        if (Number(item, "firstScore") is { } first
            && Number(item, "scanCount") is { } scans
            && scans > 1)
        {
            // ★ THE ARC IS STATED IN THE PUBLISHED FIGURES, so the delta is the difference between
            //   the two numbers the reader can see. Taking it from the raw values instead printed
            //   "62 → 98 … up 35.6", which is an arithmetic error to every reader who checks it.
            var from = Math.Round(first, MidpointRounding.AwayFromZero);
            var to = Math.Round(score, MidpointRounding.AwayFromZero);
            var delta = to - from;
            html.Append("<p class=\"ip-trend\"><span class=\"ip-trend-from\">").Append(Esc(Cai(first)))
                .Append("</span><span class=\"ip-trend-arrow\" aria-hidden=\"true\">→</span>")
                .Append("<span class=\"ip-trend-to").Append(key is not null ? $" ink-{key}" : "").Append("\">")
                .Append(Esc(Cai(score))).Append("</span>")
                .Append("<span class=\"ip-trend-note\">")
                .Append(Esc(delta >= 0
                    ? $"up {Cai(delta)} over {Group(scans)} scans"
                    : $"down {Cai(Math.Abs(delta))} over {Group(scans)} scans"))
                .Append("</span></p>");
        }

        var measured = Lenses
            .Select(l => (l.Label, Value: Number(item, l.Key)))
            .Where(l => l.Value is not null)
            .ToList();

        if (measured.Count > 0)
        {
            // ★ A BAR PER LENS, not a column of numbers. The number is kept beside it — the bar
            //   is what makes "one lens is dragging this down" visible without reading five figures,
            //   which is the whole reason the widget drew them.
            html.Append("<table class=\"ip-price-table ip-lens-table\"><caption class=\"sr-only\">")
                .Append(Esc(Str(item, "display"))).Append(" — score by lens</caption><tbody>");
            foreach (var (label, value) in measured)
            {
                var v = Math.Clamp(value!.Value, 0, 100);

                // ★ EACH LENS IN ITS OWN BAND, not all six in the headline's colour. Six green bars
                //   under a green score say only "this repository is green"; a fair one among five
                //   strong ones is the whole reason the table is drawn rather than listed. The key
                //   comes from the payload's floors (ScoreVisuals.BandKeyOf) — no cutline is written
                //   here — and with no band table every bar falls back to the accent.
                var lensKey = ScoreVisuals.BandKeyOf(root, v);
                html.Append("<tr><th scope=\"row\">").Append(Esc(label)).Append("</th>")
                    .Append("<td class=\"ip-lens-barcell\"><span class=\"ip-lens-bar\"><span class=\"ip-lens-fill")
                    .Append(lensKey is not null ? $" fill-{lensKey}" : "").Append("\" style=\"width:")
                    .Append(Esc(Score(v))).Append('%')
                    .Append(lensKey is null && hex is not null ? $";background:{hex}" : "").Append("\"></span></span></td>")
                    .Append("<td class=\"ip-lens-value").Append(lensKey is not null ? $" ink-{lensKey}" : "")
                    .Append("\">").Append(Esc(Cai(value.Value))).Append("</td></tr>");
            }

            html.Append("</tbody></table>");
        }

        // The facts about the codebase itself. Each one renders only when the payload carries it —
        // a placeholder row is worse than a missing one, because it looks measured.
        // ★ ALREADY-ESCAPED HTML, not raw text. HtmlEncode turns every Latin-1 character into a
        //   numeric entity, so escaping a joined string encodes the "·" separator with it — the
        //   markup then says something other than what it renders. Escape the PARTS, join with the
        //   literal. (This is the second template to trip over it, which is why it is spelled out.)
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

        if (facts.Count > 0)
        {
            html.Append("<table class=\"ip-price-table\"><caption class=\"sr-only\">")
                .Append(Esc(Str(item, "display"))).Append(" — what the run recorded</caption><tbody>");
            foreach (var (label, value) in facts)
            {
                html.Append("<tr><th scope=\"row\">").Append(Esc(label)).Append("</th><td>")
                    .Append(value).Append("</td></tr>");
            }

            html.Append("</tbody></table>");
        }

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
