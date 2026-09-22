using System.Text;
using System.Text.Json;
using static Imprint.Publishing.TemplateJson;

namespace Imprint.Publishing;

/// <summary>
/// One NAMED repository's survey, rendered from <c>/api/public/oss/{owner}/{name}/evidence</c>.
/// </summary>
/// <remarks>
/// <para>★★ THE REPOSITORY IS CHOSEN BY THE URL, NOT BY THE TEMPLATE. A bake is cached by
/// <c>(url, template)</c> and cannot see a widget's props, so a card that showed a partner-chosen
/// repository could not be built by filtering a feed here: two nodes wanting two repositories would
/// share one cache entry and one of them would render the other's survey. The owner and name go into
/// the fetched URL, exactly as <c>wd-architecture</c> already does, and each node then has a cache
/// key of its own.</para>
/// <para>★★ NO BAND CHIP, DELIBERATELY. This payload carries no cutlines, and a band is a CUTLINE
/// question. Inferring one from a remembered 90/70/50/25 is precisely what <see cref="BandScaleTemplate"/>
/// refuses to do and for the same reason: the lines are scoring data, they belong to the rubric, and a
/// fourth hand-written copy is how a scale drifts. The score is printed; the word is not invented.</para>
/// <para>What the payload does not carry is not filled in. A missing rebuild cost, an absent project
/// count and an empty lens list each drop their row rather than printing a zero that reads as a
/// measurement.</para>
/// </remarks>
public static class SurveyCardTemplate
{
    public const string Name = "survey-card";

    /// <summary>The lenses, in the order the product shows them.</summary>
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
        if (Root(json) is not { } root || Number(root, "headlineScore") is not { } score)
        {
            return null;
        }

        var html = new StringBuilder();
        html.Append("<div class=\"ip-stack ip-survey ip-survey-card\">");

        html.Append("<p class=\"ip-survey-headline\"><span class=\"ip-cai-score\">")
            .Append(Esc(Score(score)))
            .Append("</span><span class=\"ip-cai-unit\"> / 100</span></p>");

        // The bar is the same track the other cards use, so one score reads the same everywhere.
        html.Append("<div class=\"ip-cai ip-cai-track\"><span class=\"ip-cai-fill\" style=\"width:")
            .Append(Esc(Score(Math.Clamp(score, 0, 100)))).Append("%\"></span></div>");

        var lenses = Array(root, "lenses")
            .Select(l => (Key: Str(l, "lens"), Score: Number(l, "score")))
            .Where(l => l.Score is not null)
            .ToDictionary(l => l.Key, l => l.Score!.Value, StringComparer.Ordinal);

        if (lenses.Count > 0)
        {
            html.Append("<ul class=\"ip-survey-lenses\">");
            foreach (var (key, label) in Lenses)
            {
                if (!lenses.TryGetValue(key, out var value))
                {
                    continue;
                }

                html.Append("<li><span class=\"ip-survey-lens\">").Append(Esc(label))
                    .Append("</span><span class=\"ip-survey-lens-score\">").Append(Esc(Score(value)))
                    .Append("</span></li>");
            }

            html.Append("</ul>");
        }

        // What the run recorded. Each row is dropped when the payload does not carry it.
        var facts = new List<(string Label, string Value)>();
        if (Number(root, "productionLoc") is { } loc && loc > 0)
        {
            facts.Add(("Measured", $"{Group(loc)} lines"));
        }

        if (Str(root, "rebuildCost") is { Length: > 0 } rebuild)
        {
            facts.Add(("Rebuild cost", rebuild));
        }

        if (Number(root, "analyzableProjects") is { } projects && projects > 0)
        {
            facts.Add(("Projects", Group(projects)));
        }

        if (Str(root, "rubricVersion") is { Length: > 0 } rubric)
        {
            // ★ The rubric a number was produced under travels WITH the number. A score quoted
            //   without it cannot be recomputed, and recomputability is the whole claim.
            facts.Add(("Rubric", rubric));
        }

        if (facts.Count > 0)
        {
            html.Append("<dl class=\"ip-survey-facts\">");
            foreach (var (label, value) in facts)
            {
                html.Append("<dt>").Append(Esc(label)).Append("</dt><dd>").Append(Esc(value)).Append("</dd>");
            }

            html.Append("</dl>");
        }

        return html.Append("</div>").ToString();
    }
}
