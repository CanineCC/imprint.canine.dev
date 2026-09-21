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
        html.Append("<h3>").Append(Esc(Str(item, "display"))).Append("</h3>");

        html.Append("<p class=\"ip-cai\"><span class=\"ip-cai-score\"").Append(style).Append('>')
            .Append(Esc(Score(score))).Append("</span><span class=\"ip-cai-unit\">/ 100</span>");
        if (band.Length > 0)
        {
            html.Append("<span class=\"ip-band").Append(key is not null ? $" ip-band-{key}" : "").Append('"')
                .Append(style).Append('>').Append(Esc(band)).Append("</span>");
        }

        html.Append("</p>");

        html.Append("<div class=\"ip-cai-track\" role=\"img\" aria-label=\"")
            .Append(Esc(Score(score))).Append(" out of 100\">")
            .Append("<span class=\"ip-cai-fill").Append(key is not null ? $" fill-{key}" : "")
            .Append("\" style=\"width:").Append(Esc(Score(Math.Clamp(score, 0, 100)))).Append("%")
            .Append(hex is not null ? $";background:{hex}" : "").Append("\"></span></div>");

        // The trend as a SENTENCE. A sparkline is a picture of this fact and cannot be read; the
        // numbers either side of the arrow are the fact itself.
        if (Number(item, "firstScore") is { } first
            && Number(item, "scanCount") is { } scans
            && scans > 1)
        {
            var delta = score - first;
            html.Append("<p class=\"ip-trend\"><span class=\"ip-trend-from\">").Append(Esc(Score(first)))
                .Append("</span><span class=\"ip-trend-arrow\" aria-hidden=\"true\">→</span>")
                .Append("<span class=\"ip-trend-to\">").Append(Esc(Score(score))).Append("</span>")
                .Append("<span class=\"ip-trend-note\">")
                .Append(Esc(delta >= 0
                    ? $"up {Score(delta)} over {Group(scans)} scans"
                    : $"down {Score(Math.Abs(delta))} over {Group(scans)} scans"))
                .Append("</span></p>");
        }

        var measured = Lenses
            .Select(l => (l.Label, Value: Number(item, l.Key)))
            .Where(l => l.Value is not null)
            .ToList();

        if (measured.Count > 0)
        {
            html.Append("<table class=\"ip-price-table ip-lens-table\"><caption class=\"sr-only\">")
                .Append(Esc(Str(item, "display"))).Append(" — score by lens</caption><tbody>");
            foreach (var (label, value) in measured)
            {
                html.Append("<tr><th scope=\"row\">").Append(Esc(label)).Append("</th><td>")
                    .Append(Esc(Score(value!.Value))).Append("</td></tr>");
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
