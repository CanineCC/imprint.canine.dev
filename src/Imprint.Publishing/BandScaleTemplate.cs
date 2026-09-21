using System.Text;
using System.Text.Json;
using static Imprint.Publishing.TemplateJson;

namespace Imprint.Publishing;

/// <summary>
/// How to read the number: the fixed five-band scale, with a real published survey pinned to each
/// band that has one. Rendered from <c>/api/public/reports</c>.
/// </summary>
/// <remarks>
/// <para>★★ THE CUTLINES COME FROM THE PAYLOAD AND MUST NEVER BE WRITTEN HERE. The five words and the
/// five hues are a shared vocabulary this site already carries — they are presentation. The LINES
/// that decide which word a score gets are scoring data: they belong to the rubric, a repository can
/// in principle be pinned to a rubric that moves them, and Kennel's own <c>CaiBands</c> records that
/// hand-written copies of 90/70/50/25 were the mistake. A marketing page holding a fourth copy would
/// be the same mistake where nobody would notice it drift. No cutline is spelled out in this file,
/// and if the payload carries no band table this renders nothing at all.</para>
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

        // Worst → best as the product publishes them; this page reads best first.
        var bands = Array(root, "bands").Where(b => Str(b, "label").Length > 0).ToList();
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
            var band = bands[i];
            var label = Str(band, "label");
            var key = Str(band, "key");
            var floor = Number(band, "floor");

            html.Append("<div class=\"ip-rung\">");
            html.Append("<p class=\"ip-rung-band\"><span class=\"ip-band")
                .Append(key.Length > 0 ? $" ip-band-{Esc(key)}" : "").Append("\">")
                .Append(Esc(label)).Append("</span>");

            if (floor is { } f)
            {
                html.Append("<span class=\"ip-rung-range\">")
                    .Append(Esc(i == 0 && f <= 0 && Number(bands[1], "floor") is { } next
                        ? $"under {Score(next)}"
                        : $"from {Score(f)}"))
                    .Append("</span>");
            }

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
                html.Append("<p class=\"ip-rung-example ip-rung-empty\">")
                    .Append("No published survey sits here — a published survey is a repository's best run, ")
                    .Append("and a regression is never published.</p>");
            }

            html.Append("</div>");
        }

        return html.Append("</div>").ToString();
    }
}
