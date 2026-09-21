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
            var tint = key is not null ? $" ink-{key}" : "";
            var style = hex is not null ? $" style=\"color:{hex}\"" : "";

            html.Append("<div class=\"ip-stack ip-survey\">");
            html.Append("<h3>").Append(Esc(Str(report, "display"))).Append("</h3>");

            html.Append("<p class=\"ip-cai\"><span class=\"ip-cai-score").Append(tint).Append('"').Append(style)
                .Append('>').Append(Esc(Score(score))).Append("</span>")
                .Append("<span class=\"ip-cai-unit\">/ 100</span>");
            if (band.Length > 0)
            {
                html.Append("<span class=\"ip-band").Append(key is not null ? $" ip-band-{key}" : "").Append('"')
                    .Append(style).Append('>').Append(Esc(band)).Append("</span>");
            }

            html.Append("</p>");

            // The bar is 0–100 flat, not the banded ladder: the cutlines are SCORING data and this site
            // is not given them. Drawing a ladder from remembered numbers is how a fourth copy of 90/70/
            // 50/25 gets born — and it would be wrong for any repository pinned to a rubric that moves
            // them. A pin on a plain scale says exactly what the payload said and nothing more.
            html.Append("<div class=\"ip-cai-track\" role=\"img\" aria-label=\"")
                .Append(Esc(Score(score))).Append(" out of 100\">")
                .Append("<span class=\"ip-cai-fill").Append(key is not null ? $" fill-{key}" : "")
                .Append("\" style=\"width:").Append(Esc(Score(Math.Clamp(score, 0, 100)))).Append("%")
                .Append(hex is not null ? $";background:{hex}" : "").Append("\"></span></div>");

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
        if (Number(root, "matched") is { } matched && matched > reports.Count)
        {
            html.Append("<div class=\"ip-prose\"><p>Showing ")
                .Append(Esc(Group(reports.Count))).Append(" of <strong>")
                .Append(Esc(Group(matched)))
                .Append("</strong> published surveys — every one of them readable in full.</p></div>");
        }

        return html.ToString();
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
