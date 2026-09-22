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

    /// <summary>The owner and name out of <c>/api/public/oss/{owner}/{name}/evidence</c>, or empties.</summary>
    /// <remarks>
    /// ★ Read POSITIONALLY from the end, not by a fixed index: the path is built by the widget's own
    /// prerender pattern, and an added prefix segment would silently shift a fixed index onto the
    /// wrong part of it. The two segments before <c>evidence</c> are the pair, wherever they sit.
    /// </remarks>
    private static (string Owner, string Name) RepositoryIn(string? url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return ("", "");
        }

        var parts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        // ★ System.Array, spelled out: `using static TemplateJson` brings an `Array(...)` METHOD
        //   into scope and the bare name resolves to that one.
        var at = System.Array.LastIndexOf(parts, "evidence");
        return at >= 2
            ? (Uri.UnescapeDataString(parts[at - 2]), Uri.UnescapeDataString(parts[at - 1]))
            : ("", "");
    }

    public static string? Render(string? json, string? origin, string? url = null)
    {
        if (Root(json) is not { } root || Number(root, "headlineScore") is not { } score)
        {
            return null;
        }

        var html = new StringBuilder();
        html.Append("<div class=\"ip-stack ip-survey ip-survey-card\">");

        // ★★ THE CARD LEADS WITH THE REPOSITORY, LIKE EVERY OTHER CARD ON THE ESTATE — and this one
        //    did not, which is the first thing a reader notices when two sites show "the same" card.
        //    The payload is the measurement and does not name what it measured; the URL does, and the
        //    URL is what the bake is keyed by, so it cannot disagree with the body it fetched.
        //    ★ Still no band chip: this payload carries no cutlines, and a band is a cutline question.
        //    Inferring one from a remembered 90/70/50/25 is exactly what BandScaleTemplate refuses.
        if (RepositoryIn(url) is var (owner, name) && name.Length > 0)
        {
            html.Append(ScoreVisuals.Head(name, owner, owner.Length > 0 ? $"{owner}/{name}" : name, "", null, ""));
        }

        // ★★ THE SAME BODY AS EVERY OTHER CARD. This one drew a headline and a flat bar while the
        //    card it sits beside on the same estate drew a ladder, lens bars and the facts — four
        //    templates, four drawings of one object, which reads as four products rather than one
        //    measurement shown four times. What differs here is only what the payload can support:
        //    no owner/name (the page names the repository), no band word and no series.
        html.Append("<p class=\"ip-cai\"><span class=\"ip-cai-kicker\">CAI</span>")
            .Append("<span class=\"ip-cai-score\">").Append(Esc(Cai(score)))
            .Append("</span><span class=\"ip-cai-unit\">/ 100</span></p>");

        // No bands in this payload, so this is the flat bar — the honest degradation, not a design.
        html.Append(ScoreVisuals.Ladder(root, score, null, null));

        // ★ THE LENS SCORES ARE IN THIS PAYLOAD AND WERE PRINTED AS A LIST OF NUMBERS. They are the
        //   same six readings the island card draws as bars; drawing them as bars is what makes a
        //   weak lens visible beside five strong ones.
        var byLens = Array(root, "lenses")
            .Select(l => (Key: Str(l, "lens"), Score: Number(l, "score")))
            .Where(l => l.Score is not null)
            .ToDictionary(l => l.Key, l => l.Score!.Value, StringComparer.Ordinal);

        var measured = Lenses
            .Where(l => byLens.ContainsKey(l.Key))
            .Select(l => (l.Label, byLens[l.Key]))
            .ToList();

        html.Append(ScoreVisuals.Lenses(root, measured, null, "This repository"));

        // What the run recorded. Each row is dropped when the payload does not carry it.
        var facts = new List<(string Label, string Html)>();
        if (Number(root, "productionLoc") is { } loc && loc > 0)
        {
            facts.Add(("Measured", Esc($"{Group(loc)} lines")));
        }

        if (Str(root, "rebuildCost") is { Length: > 0 } rebuild)
        {
            facts.Add(("Rebuild cost", Esc(rebuild)));
        }

        if (Number(root, "analyzableProjects") is { } projects && projects > 0)
        {
            facts.Add(("Projects", Esc(Group(projects))));
        }

        if (Str(root, "rubricVersion") is { Length: > 0 } rubric)
        {
            // ★ The rubric a number was produced under travels WITH the number. A score quoted
            //   without it cannot be recomputed, and recomputability is the whole claim.
            facts.Add(("Rubric", Esc(rubric)));
        }

        html.Append(ScoreVisuals.Facts(facts, "This repository"));

        return html.Append("</div>").ToString();
    }
}
