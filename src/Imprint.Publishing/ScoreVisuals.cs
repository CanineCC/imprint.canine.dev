using System.Text;
using System.Text.Json;
using static Imprint.Publishing.TemplateJson;

namespace Imprint.Publishing;

/// <summary>
/// The two drawings a score card makes: the banded ladder it sits on, and the line it got there by.
/// </summary>
/// <remarks>
/// <para>★★ ONE IMPLEMENTATION, BECAUSE TWO CARDS THAT DRAW THE SAME SCALE DIFFERENTLY ARE A BUG
/// NOBODY REPORTS. The strip, the hero and the full survey all show a score against the same rubric;
/// when the ladder lived inside one template the others drew a flat bar instead, and the difference
/// read as two different measurements rather than one drawn twice.</para>
/// <para>★★ NO CUTLINE IS WRITTEN HERE, EVER. Every rung is sized from a floor in the payload. The
/// words and hues are presentation this site already carries; the LINES are scoring data and belong
/// to the rubric. A payload with no band table gets the flat bar, which is the honest degradation.</para>
/// </remarks>
public static class ScoreVisuals
{
    /// <summary>
    /// The banded ladder with the score pinned on it, or a flat bar when the payload has no bands.
    /// </summary>
    public static string Ladder(JsonElement root, double score, string? hex, string? key)
    {
        var pin = Math.Clamp(score, 0, 100);
        var label = $"{Score(score)} out of 100";

        var bands = Array(root, "bands")
            .Select(b => (Key: Str(b, "key"), Floor: Number(b, "floor")))
            .Where(b => b.Floor is not null)
            .OrderBy(b => b.Floor!.Value)
            .ToList();

        if (bands.Count < 2)
        {
            return "<div class=\"ip-cai-track\" role=\"img\" aria-label=\"" + Esc(label) + "\">"
                 + "<span class=\"ip-cai-fill" + (key is not null ? $" fill-{key}" : "") + "\" style=\"width:"
                 + Esc(Score(pin)) + "%" + (hex is not null ? $";background:{hex}" : "") + "\"></span></div>";
        }

        var html = new StringBuilder();
        html.Append("<div class=\"ip-cai-ladder\" role=\"img\" aria-label=\"").Append(Esc(label)).Append("\">");

        for (var i = 0; i < bands.Count; i++)
        {
            var span = (i + 1 < bands.Count ? bands[i + 1].Floor!.Value : 100d) - bands[i].Floor!.Value;
            if (span <= 0)
            {
                continue;
            }

            html.Append("<span class=\"ip-cai-rung")
                .Append(bands[i].Key.Length > 0 ? " ip-cai-rung-" + Esc(bands[i].Key) : "")
                .Append("\" style=\"flex:").Append(Esc(Score(span))).Append("\"></span>");
        }

        html.Append("<span class=\"ip-cai-pin\" style=\"left:").Append(Esc(Score(pin))).Append('%')
            .Append(hex is not null ? ";--pin:" + hex : "").Append("\"></span>");

        return html.Append("</div>").ToString();
    }

    /// <summary>
    /// The score's history as a line, or nothing when there is not enough history to draw one.
    /// </summary>
    /// <remarks>
    /// <para>★ IT DOES NOT REPLACE THE SENTENCE, AND THE NOTE THAT SAID IT WOULD WAS WRONG. "A
    /// sparkline is a picture of this fact and cannot be read" was the reason the bake shipped the
    /// numbers alone — but the shape is a different fact from the endpoints: 62 to 98 in one jump and
    /// 62 to 98 by steady work are the same two numbers and not the same story. The widget this
    /// replaced drew both, so this draws both, and the line is aria-hidden because the sentence
    /// beside it already carries the reading.</para>
    /// <para>★ TWO POINTS ARE NOT A TREND. Below that it renders nothing rather than a flat stub
    /// that would read as "no change".</para>
    /// </remarks>
    public static string Sparkline(JsonElement item, string? hex)
    {
        var series = Array(item, "series")
            .Where(v => v.ValueKind == JsonValueKind.Number)
            .Select(v => v.GetDouble())
            .ToList();

        if (series.Count < 3)
        {
            return "";
        }

        var min = series.Min();
        var max = series.Max();
        var range = max - min;

        // A flat series still draws, centred: the point is that it did not move.
        double Y(double v) => range < 1e-9 ? 16 : 30 - ((v - min) / range * 28);
        double X(int i) => i * 100.0 / (series.Count - 1);

        var points = new StringBuilder();
        for (var i = 0; i < series.Count; i++)
        {
            points.Append(i > 0 ? " " : "")
                  .Append(X(i).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture))
                  .Append(',')
                  .Append(Y(series[i]).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture));
        }

        return "<div class=\"ip-cai-spark\"><svg viewBox=\"0 0 100 32\" preserveAspectRatio=\"none\" "
             + "aria-hidden=\"true\" focusable=\"false\"><polyline points=\"" + points
             + "\" fill=\"none\" stroke=\"" + (hex is not null ? Esc(hex) : "currentColor")
             + "\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\" "
             + "vector-effect=\"non-scaling-stroke\" /></svg></div>";
    }
}
