using System.Text;
using System.Text.Json;
using static Imprint.Publishing.TemplateJson;

namespace Imprint.Publishing;

/// <summary>
/// How to read the number: the fixed five-band scale, with a real published survey pinned to each
/// band that has one, when the payload carries examples.
/// </summary>
/// <remarks>
/// <para>★★ THE CUTLINES COME FROM THE PAYLOAD AND ARE NEVER WRITTEN HERE. The LINES that decide
/// which word a score gets are scoring data: they belong to the rubric, a repository can in principle
/// be pinned to one that moves them, and Kennel's own <c>CaiBands</c> records that hand-written
/// copies of 90/70/50/25 were the mistake. No cutline is spelled out in this file, and with no
/// cutlines in the payload this renders nothing at all.</para>
/// <para>★ THE WORDS ARE OURS, AND THAT IS THE PRODUCT'S OWN SPLIT, NOT A LIBERTY TAKEN HERE.
/// <c>CaiBands</c> states it exactly: "the display words and hues stay here because they are
/// presentation; the LINES are scoring data". This site already carries the five hues keyed by the
/// same css keys; the five words are the same category of thing.</para>
/// <para>★★ TWO ENVELOPES, BECAUSE I MIS-SCOPED THE DEPENDENCY ONCE ALREADY. This first read only
/// <c>/api/public/reports.bands</c>, which reaches prod at a nightly promote — so the last iframe on
/// the estate sat there for a day waiting for a deploy. The cutlines were public the whole time, in
/// <c>/api/public/procurement-checklist.json</c>, which llms.txt advertises as machine-readable. It
/// now reads EITHER: the checklist's <c>caiFloor.bandCutlines</c>, or the reports feed's band table
/// (which also carries the published examples to pin).</para>
/// <para><b>Bounds are stated as the payload states them.</b> Each band gets "from {floor}", because
/// that is exactly what a floor means and it cannot be wrong at the boundary; the bottom band, whose
/// floor is zero, reads "under {the next floor}" instead, which is the same fact said the way a
/// reader would say it. Writing "70–89" would be inventing a top end the data does not carry.</para>
/// <para><b>A band with no example says so.</b> The published set is each repository's PEAK run — a
/// regression is never published — so the bottom of the scale is usually empty. Silently dropping
/// those rungs would turn a five-band scale into a three-band one and quietly flatter the corpus.</para>
/// </remarks>
public static class BandScaleTemplate
{
    public const string Name = "band-scale";

    public static string? Render(string? json, string? origin)
    {
        if (Root(json) is not { } root)
        {
            return null;
        }

        var bands = ReadBands(root);
        if (bands.Count == 0)
        {
            return null;
        }

        var reports = Array(root, "reports")
            .Where(r => Str(r, "display").Length > 0 && Number(r, "score") is not null)
            .ToList();

        var html = new StringBuilder();
        html.Append("<div class=\"ip-stack ip-ladder\">");

        for (var i = bands.Count - 1; i >= 0; i--)
        {
            var (label, key, floor) = bands[i];

            html.Append("<div class=\"ip-rung\">");
            html.Append("<p class=\"ip-rung-band\"><span class=\"ip-band")
                .Append(key.Length > 0 ? $" ip-band-{Esc(key)}" : "").Append("\">")
                .Append(Esc(label)).Append("</span>");

            html.Append("<span class=\"ip-rung-range\">")
                .Append(Esc(i == 0 && floor <= 0 && bands.Count > 1
                    ? $"under {Score(bands[1].Floor)}"
                    : $"from {Score(floor)}"))
                .Append("</span>");

            html.Append("</p>");

            // The best published survey that actually landed in this band.
            var example = reports.FirstOrDefault(r => Str(r, "band") == label);
            if (example.ValueKind == JsonValueKind.Object)
            {
                var score = Number(example, "score") ?? 0;
                html.Append("<p class=\"ip-rung-example\">");
                if (Link(origin, Str(example, "reportPath")) is { } href)
                {
                    html.Append("<a href=\"").Append(Esc(href)).Append("\">")
                        .Append(Esc(Str(example, "display"))).Append("</a>");
                }
                else
                {
                    html.Append(Esc(Str(example, "display")));
                }

                // Not tinted: the band word beside it already carries the colour, and `.ink-*` would
                // be silently outranked by `.ip-cai-score` anyway (same specificity, defined later).
                html.Append("<span class=\"ip-cai-score ip-rung-score\">")
                    .Append(Esc(Score(score))).Append("</span></p>");
            }
            else
            {
                // ★ The REASON is stated once, in the section's lede. Repeating it on every empty
                //   rung printed the same two-line sentence twice under the ladder and read as a
                //   fault rather than as a fact about the corpus.
                html.Append("<p class=\"ip-rung-example ip-rung-empty\">No published survey in this band.</p>");
            }

            html.Append("</div>");
        }

        return html.Append("</div>").ToString();
    }

    /// <summary>One rung: the product's word, this site's css key, and the rubric's floor.</summary>
    private readonly record struct Rung(string Label, string Key, double Floor);

    /// <summary>
    /// The five display words, worst → best, with the css keys this site already styles.
    /// </summary>
    /// <remarks>
    /// ★ Presentation, by the product's own division — see the type remarks. The FLOORS are not here
    /// and never will be; they are read from whichever payload arrived.
    /// </remarks>
    private static readonly (string Label, string Key)[] Words =
    [
        ("Critical", "critical"),
        ("Weak", "poor"),
        ("Adequate", "fair"),
        ("Strong", "healthy"),
        ("Exemplary", "exemplary"),
    ];

    /// <summary>The scale, worst → best, from either payload — or empty, which renders nothing.</summary>
    private static IReadOnlyList<Rung> ReadBands(JsonElement root)
    {
        // The reports feed carries the product's own table: words, keys and floors together, so when
        // it is there it wins — those words came from the rubric rather than from the list below.
        var published = Array(root, "bands")
            .Where(b => Str(b, "label").Length > 0 && Number(b, "floor") is not null)
            .Select(b => new Rung(Str(b, "label"), Str(b, "key"), Number(b, "floor")!.Value))
            .ToList();

        if (published.Count > 0)
        {
            return published;
        }

        // The procurement checklist carries the CUTLINES — the scoring half — best-first. Four lines
        // describe five bands: the lowest band's floor is zero, which is not a cutline at all.
        if (!root.TryGetProperty("caiFloor", out var floor))
        {
            return [];
        }

        var cutlines = Array(floor, "bandCutlines")
            .Where(c => c.ValueKind == JsonValueKind.Number)
            .Select(c => c.GetDouble())
            .OrderBy(c => c)
            .ToList();

        // ★ Only draw a scale the words actually fit. Anything other than four cutlines means the
        //   rubric has changed shape, and drawing it against five remembered words would be
        //   inventing a scale rather than publishing one.
        if (cutlines.Count != Words.Length - 1)
        {
            return [];
        }

        return [.. Words.Select((w, i) => new Rung(w.Label, w.Key, i == 0 ? 0 : cutlines[i - 1]))];
    }
}
