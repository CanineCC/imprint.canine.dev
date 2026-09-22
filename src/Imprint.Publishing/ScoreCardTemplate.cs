using System.Text;
using System.Text.Json;
using static Imprint.Publishing.TemplateJson;

namespace Imprint.Publishing;

/// <summary>
/// Published surveys — repository, CAI, band — rendered from <c>/api/public/reports</c> into this
/// site's own markup.
/// </summary>
/// <remarks>
/// <para>★ WHAT THIS REPLACES. These cards were an iframe of a view the product rendered. The numbers
/// were real and no crawler, no language model and no reader without JavaScript ever saw one of them:
/// eight frames across the home page and the public-reports page, on the two pages whose entire
/// argument is "here are real scores, go and check them".</para>
/// <para><b>Two names, one endpoint.</b> The bake is keyed by (url, template), so two widgets reading
/// the same URL must differ by TEMPLATE or one silently overwrites the other's fragment — that
/// already happened once, with the two pricing widgets. The home page wants a short strip and the
/// gallery page wants the whole curated set, so the count lives in the name rather than in a prop
/// that the bake key cannot see.</para>
/// <para><b>Band colour comes from the LABEL, not the payload hex.</b> The payload carries one hex,
/// and this site is themed light and dark; the light hex on a dark card is the kind of thing that
/// passes review and fails a contrast check. The site already defines the five band hues as
/// light/dark pairs, so the label picks the class and the payload hex is only the fallback for a band
/// word this site has not been taught. ★ That fallback is a <c>style</c> attribute fed from another
/// service's JSON, which is a CSS injection sink — <see cref="TemplateJson.HexColour"/> is what makes
/// it safe, not the HTML escaping.</para>
/// </remarks>
public static class ScoreCardTemplate
{
    /// <summary>A short strip of the best published surveys.</summary>
    public const string Name = "score-cards";

    /// <summary>The whole curated feed, for the page that exists to list it.</summary>
    public const string FullName = "score-cards-full";

    /// <summary>
    /// One card. The hero puts the copy on the left and a single card on the right, so a strip of
    /// four there is not "more evidence", it is a broken layout.
    /// </summary>
    public const string OneName = "score-card";

    /// <summary>How many cards the short strip shows. Four fills a row and leaves none dangling.</summary>
    private const int StripCount = 4;

    public static string? Render(string? json, string? origin) => Render(json, origin, StripCount);

    public static string? RenderFull(string? json, string? origin) => Render(json, origin, int.MaxValue);

    public static string? RenderOne(string? json, string? origin) => Render(json, origin, 1);

    private static string? Render(string? json, string? origin, int take)
    {
        if (Root(json) is not { } root)
        {
            return null;
        }

        var reports = Array(root, "reports")
            .Where(r => Str(r, "display").Length > 0 && Number(r, "score") is not null)
            .Take(take)
            .ToList();

        if (reports.Count == 0)
        {
            return null;
        }

        var html = new StringBuilder();
        html.Append("<div class=\"ip-grid ")
            .Append(take switch
            {
                1 => "ip-grid-1up",
                StripCount => "ip-grid-4up",
                _ => "ip-grid-cards",
            })
            .Append("\">");

        foreach (var report in reports)
        {
            var score = Number(report, "score") ?? 0;
            var band = Str(report, "band");
            var key = BandKey(band);
            var hex = key is null ? HexColour(Str(report, "bandHex")) : null;
            var style = hex is not null ? $" style=\"color:{hex}\"" : "";

            html.Append("<div class=\"ip-stack ip-survey\">");
            html.Append("<h3>").Append(Esc(Str(report, "display"))).Append("</h3>");

            // ★ The SCORE is not tinted and the band word is. Two coloured things side by side fight,
            //   and the earlier attempt to tint both was silently ignored anyway: `.ink-exemplary` is
            //   an existing (0,1,0) utility defined far earlier in this sheet, so `.ip-cai-score`
            //   tied on specificity and won on order. A class that renders nothing is worse than no
            //   class — it reads as a decision that was taken.
            html.Append("<p class=\"ip-cai\"><span class=\"ip-cai-score\"").Append(style)
                .Append('>').Append(Esc(Score(score))).Append("</span>")
                .Append("<span class=\"ip-cai-unit\">/ 100</span>");
            if (band.Length > 0)
            {
                html.Append("<span class=\"ip-band").Append(key is not null ? $" ip-band-{key}" : "").Append('"')
                    .Append(style).Append('>').Append(Esc(band)).Append("</span>");
            }

            html.Append("</p>");

            // ★★ THE BANDED LADDER, AND THE COMMENT THAT USED TO SIT HERE WAS WRONG. It said "the
            //    cutlines are SCORING data and this site is not given them", and drew a flat 0-100
            //    bar instead. The cutlines are in THIS payload: /api/public/reports carries a `bands`
            //    table with a floor per band, which is what BandScaleTemplate reads. So the card drew
            //    a plain bar where the widget it replaced drew the real ladder with a pin, and the
            //    reason recorded for it was a fact nobody had checked.
            //    Every number below still comes from the payload, which was the actual rule: no
            //    cutline is written in this file, and with no band table the flat bar is drawn.
            html.Append(Ladder(root, score, hex, key));

            var lenses = Strings(report, "lenses").Count;
            var language = Str(report, "primaryLanguage");
            var facts = new List<string>();
            if (lenses > 0)
            {
                facts.Add(lenses == 1 ? "1 lens measured" : $"{lenses} lenses measured");
            }

            if (language.Length > 0)
            {
                facts.Add(language);
            }

            if (facts.Count > 0)
            {
                // Escape the FACTS, join with a literal separator. HtmlEncode turns every non-ASCII
                // character into a numeric entity, so escaping the joined string encodes the "·" too —
                // harmless on screen, and it makes the markup say something other than what it renders.
                html.Append("<div class=\"ip-prose\"><p>")
                    .Append(string.Join(" · ", facts.Select(Esc))).Append("</p></div>");
            }

            if (Link(origin, Str(report, "reportPath")) is { } href)
            {
                html.Append("<p class=\"ip-survey-link\"><a href=\"").Append(Esc(href))
                    .Append("\">Read the survey →</a></p>");
            }

            html.Append("</div>");
        }

        html.Append("</div>");

        // The corpus total is the honest frame around a curated list — without it, a strip of four
        // reads as "four surveys exist".
        //
        // ★ AND IT DOES NOT SAY "BEST FIRST". It said so until the rendering was looked at: the feed
        //   runs 98, 86, 83 … 59, 51 and then 89, 81, 76 — a descending curated head with a tail the
        //   product appends for its own reasons. The sentence was a claim about the payload that the
        //   payload does not make, and it would have been published beside the numbers disproving it.
        // ★ Not under ONE card. "Showing 1 of 3,112 published surveys" beside a hero card is a
        //   sentence about the widget rather than about the product, and the hero's own copy has
        //   already said what the reader is looking at. The line exists to stop a STRIP reading as
        //   "four surveys exist"; a single card never reads that way.
        if (reports.Count > 1 && Number(root, "matched") is { } matched && matched > reports.Count)
        {
            html.Append("<div class=\"ip-prose\"><p>Showing ")
                .Append(Esc(Group(reports.Count))).Append(" of <strong>")
                .Append(Esc(Group(matched)))
                .Append("</strong> published surveys — every one of them readable in full.</p></div>");
        }

        return html.ToString();
    }

    /// <summary>
    /// The banded ladder with the score pinned on it, or a flat bar when the payload has no bands.
    /// </summary>
    /// <remarks>
    /// <para>★★ THE CUTLINES COME FROM THE PAYLOAD AND ARE NEVER WRITTEN HERE — the same rule
    /// <see cref="BandScaleTemplate"/> keeps, for the same reason: the lines belong to the rubric, a
    /// repository can be pinned to one that moves them, and a hand-written copy of 90/70/50/25 is how
    /// a scale drifts. Each rung is sized from the floor of the NEXT band, so the ladder is whatever
    /// the rubric says it is.</para>
    /// <para>★ No band table in the payload means the FLAT bar, not a remembered ladder. That is the
    /// honest degradation, and it is what an older feed gets.</para>
    /// </remarks>
    private static string Ladder(JsonElement root, double score, string? hex, string? key)
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
            var from = bands[i].Floor!.Value;
            var to = i + 1 < bands.Count ? bands[i + 1].Floor!.Value : 100d;
            var span = to - from;
            if (span <= 0)
            {
                continue;
            }

            html.Append("<span class=\"ip-cai-rung")
                .Append(bands[i].Key.Length > 0 ? " ip-cai-rung-" + Esc(bands[i].Key) : "")
                .Append("\" style=\"flex:").Append(Esc(Score(span))).Append("\"></span>");
        }

        // The pin is the reading, on the same 0-100 axis the rungs are laid out on.
        html.Append("<span class=\"ip-cai-pin\" style=\"left:").Append(Esc(Score(pin))).Append('%')
            .Append(hex is not null ? ";--pin:" + hex : "").Append("\"></span>");

        return html.Append("</div>").ToString();
    }

    /// <summary>
    /// The site's CSS key for a band word. Presentation only — the WORDS and HUES are a shared
    /// vocabulary this site already carries; the CUTLINES that decide which word a score gets are
    /// scoring data and stay with the product.
    /// </summary>
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
