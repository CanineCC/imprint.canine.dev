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
    /// A survey card's head: the repository over its owner, with the band as a tinted chip.
    /// </summary>
    /// <remarks>
    /// <para>★★ THE REPOSITORY IS TWO FACTS. Printed as one <c>owner/name</c> token it wraps
    /// mid-word — this sheet refuses <c>break-word</c> sheet-wide, with a measured reason — so a long
    /// repository took three lines in a 280px card and pushed that card's score, ladder and link
    /// below its neighbours'. Two ellipsised lines are a fixed height whatever the name is.</para>
    /// <para>★ THE BAND IS A CHIP HERE, NOT A WORD. As bare uppercase text beside the score it read
    /// as a caption on the number; the pill beside the name is the verdict, said before the reader
    /// has parsed a digit. The band-scale ladder still uses the bare word, where the word IS the row:
    /// two treatments of one vocabulary, not a rename.</para>
    /// </remarks>
    public static string Head(JsonElement item, string band, string? key, string style) =>
        Head(Str(item, "name"), Str(item, "owner"), Str(item, "display"), band, key, style);

    /// <summary>The same head for a payload that does not name the repository it describes.</summary>
    /// <remarks>
    /// ★ THE EVIDENCE BUNDLE CARRIES NO OWNER AND NO NAME — it is the measurement, not the card —
    /// so the card it feeds had no title at all while every other card on the estate led with one.
    /// The url names the repository (<c>/api/public/oss/{owner}/{name}/evidence</c>) and the url is
    /// what the bake is keyed by, so it is the one place that cannot disagree with the payload.
    /// </remarks>
    public static string Head(string name, string owner, string display, string band, string? key, string style)
    {

        var html = new StringBuilder();
        html.Append("<div class=\"ip-survey-top\"><h3 class=\"ip-survey-id\">");
        if (name.Length > 0)
        {
            html.Append("<span class=\"ip-survey-repo\">").Append(Esc(name)).Append("</span>");
            if (owner.Length > 0)
            {
                html.Append("<span class=\"ip-survey-by\">by ").Append(Esc(owner)).Append("</span>");
            }
        }
        else
        {
            html.Append("<span class=\"ip-survey-repo\">").Append(Esc(display)).Append("</span>");
        }

        html.Append("</h3>");

        if (band.Length > 0)
        {
            html.Append("<span class=\"ip-chip").Append(key is not null ? $" ip-chip-{key}" : "").Append('"')
                .Append(style).Append('>').Append(Esc(band)).Append("</span>");
        }

        return html.Append("</div>").ToString();
    }

    /// <summary>The score line: a quiet CAI kicker, the number inked in its band, and the unit.</summary>
    /// <remarks>
    /// ★★ BOTH CLASSES ON THE INK, ALWAYS. <c>.ink-exemplary</c> is an existing (0,1,0) utility
    /// defined ~870 lines before <c>.ip-cai-score</c> in the site sheet, so written alone it ties on
    /// specificity and loses on ORDER — the tint renders nothing. That is how it was lost once and
    /// recorded as a decision rather than as a bug; the sheet now carries the (0,2,0) pair.
    /// </remarks>
    public static string ScoreLine(double score, string? key, string style) =>
        "<p class=\"ip-cai\"><span class=\"ip-cai-kicker\">CAI</span>"
        + "<span class=\"ip-cai-score" + (key is not null ? $" ink-{key}" : "") + "\"" + style + ">"
        + Esc(Cai(score)) + "</span><span class=\"ip-cai-unit\">/ 100</span></p>";

    /// <summary>
    /// The banded ladder with the score pinned on it, or a flat bar when the payload has no bands.
    /// </summary>
    public static string Ladder(JsonElement root, double score, string? hex, string? key)
    {
        var pin = Math.Clamp(score, 0, 100);
        // ★ THE LABEL SAYS WHAT THE PAGE SAYS. It read "97.6 out of 100" beside a printed 98, so a
        //   screen-reader user and a sighted one were given two different numbers for one score.
        var label = $"{Cai(score)} out of 100";

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
    /// The lens table: one bar per measured lens, each inked in its own band, with the value beside it.
    /// </summary>
    /// <remarks>
    /// ★★ THIS IS THE HALF THAT MADE FOUR CARDS LOOK LIKE FOUR PRODUCTS. The lens scores were drawn
    /// by one template out of four; the other three printed "6 lenses measured" — a COUNT where the
    /// card they were copied from showed the six numbers. A count says the survey happened; the bars
    /// say what it found, and they are the reason a reader can see that one lens is dragging the
    /// score down without reading six figures.
    /// </remarks>
    public static string Lenses(JsonElement root, IReadOnlyList<(string Label, double Value)> measured,
        string? hex, string caption)
    {
        if (measured.Count == 0)
        {
            return "";
        }

        var html = new StringBuilder();
        html.Append("<table class=\"ip-price-table ip-lens-table\"><caption class=\"sr-only\">")
            .Append(Esc(caption)).Append(" — score by lens</caption><tbody>");

        foreach (var (label, value) in measured)
        {
            var v = Math.Clamp(value, 0, 100);
            var key = BandKeyOf(root, v);
            html.Append("<tr><th scope=\"row\">").Append(Esc(label)).Append("</th>")
                .Append("<td class=\"ip-lens-barcell\"><span class=\"ip-lens-bar\"><span class=\"ip-lens-fill")
                .Append(key is not null ? $" fill-{key}" : "").Append("\" style=\"width:")
                .Append(Esc(Score(v))).Append('%')
                .Append(key is null && hex is not null ? $";background:{hex}" : "").Append("\"></span></span></td>")
                .Append("<td class=\"ip-lens-value").Append(key is not null ? $" ink-{key}" : "")
                .Append("\">").Append(Esc(Cai(value))).Append("</td></tr>");
        }

        return html.Append("</tbody></table>").ToString();
    }

    /// <summary>The arc: where the repository started, where it is now, and the distance travelled.</summary>
    /// <remarks>
    /// ★ THE DELTA IS THE DIFFERENCE BETWEEN THE TWO NUMBERS THE READER CAN SEE. Taking it from the
    /// raw values printed "62 → 98 … up 35.6", which is an arithmetic error to anyone who checks it —
    /// and the arc is printed precisely so that it will be checked.
    /// </remarks>
    public static string Trend(JsonElement item, double score, string? key)
    {
        if (Number(item, "firstScore") is not { } first
            || Number(item, "scanCount") is not { } scans
            || scans <= 1)
        {
            return "";
        }

        var delta = Math.Round(score, MidpointRounding.AwayFromZero) - Math.Round(first, MidpointRounding.AwayFromZero);
        return "<p class=\"ip-trend\"><span class=\"ip-trend-from\">" + Esc(Cai(first))
             + "</span><span class=\"ip-trend-arrow\" aria-hidden=\"true\">→</span>"
             + "<span class=\"ip-trend-to" + (key is not null ? " ink-" + key : "") + "\">"
             + Esc(Cai(score)) + "</span><span class=\"ip-trend-note\">"
             + Esc(delta >= 0
                 ? $"up {Cai(delta)} over {Group(scans)} scans"
                 : $"down {Cai(Math.Abs(delta))} over {Group(scans)} scans")
             + "</span></p>";
    }

    /// <summary>What the run recorded about the codebase. Already-escaped values.</summary>
    /// <remarks>
    /// ★ A row renders only when the payload carries it. A placeholder row is worse than a missing
    /// one, because it looks measured.
    /// </remarks>
    public static string Facts(IReadOnlyList<(string Label, string Html)> facts, string caption)
    {
        if (facts.Count == 0)
        {
            return "";
        }

        var html = new StringBuilder();
        html.Append("<table class=\"ip-price-table ip-fact-table\"><caption class=\"sr-only\">")
            .Append(Esc(caption)).Append(" — what the run recorded</caption><tbody>");
        foreach (var (label, value) in facts)
        {
            html.Append("<tr><th scope=\"row\">").Append(Esc(label)).Append("</th><td>")
                .Append(value).Append("</td></tr>");
        }

        return html.Append("</tbody></table>").ToString();
    }

    /// <summary>
    /// The css key for the band a value falls in, read from the payload's floors — or null.
    /// </summary>
    /// <remarks>
    /// ★★ THE CUTLINES COME FROM THE PAYLOAD HERE TOO. A lens bar inked by its own band is the
    /// thing that makes "one lens is dragging this down" visible without reading six figures, and
    /// deciding which band a 96 is in needs the LINES — scoring data. With no band table in the
    /// payload this returns null and every bar draws in the accent, which is the honest degradation
    /// and the same rule <see cref="Ladder"/> follows.
    /// </remarks>
    public static string? BandKeyOf(JsonElement root, double value)
    {
        string? key = null;
        var best = double.NegativeInfinity;
        foreach (var band in Array(root, "bands"))
        {
            if (Number(band, "floor") is { } floor && floor <= value && floor >= best
                && Str(band, "key") is { Length: > 0 } k)
            {
                best = floor;
                key = k;
            }
        }

        return key;
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
    public static string Sparkline(JsonElement item, string? hex, string? key = null)
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

        // ★ THE LINE TAKES THE BAND'S COLOUR, not the muted default it shipped with. A grey line
        //   under a green score reads as a different, quieter fact; it is the SAME fact, drawn.
        //   `currentColor` is the fallback, and the class sets it, so a payload hex still wins.
        return "<div class=\"ip-cai-spark" + (key is not null ? " ink-" + key : "") + "\"><svg viewBox=\"0 0 100 32\" preserveAspectRatio=\"none\" "
             + "aria-hidden=\"true\" focusable=\"false\"><polyline points=\"" + points
             + "\" fill=\"none\" stroke=\"" + (hex is not null ? Esc(hex) : "currentColor")
             + "\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\" "
             + "vector-effect=\"non-scaling-stroke\" /></svg></div>";
    }
}
