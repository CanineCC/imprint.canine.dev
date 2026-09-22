using System.Text;
using static Imprint.Publishing.TemplateJson;

namespace Imprint.Publishing;

/// <summary>
/// Which languages the product models, how clearly each one surveys, and which lenses resolve —
/// rendered from <c>/api/public/language-support</c>.
/// </summary>
/// <remarks>
/// <para>★ THE WHOLE POINT OF THE PAGE WAS IN A SHADOW ROOT. <c>/languages/</c> exists to answer
/// "do you cover my stack?", and its answer — seventeen languages, each with a clarity band, a
/// one-line summary and the lenses that resolve for it — reached no crawler, no language model and
/// no reader without JavaScript. The claim ("multi-language") was indexable; the table proving it
/// was not. This is the single densest piece of buying information on the site.</para>
/// <para><b>FIT is survey clarity, not language quality, and the page must keep saying so.</b> The
/// payload carries that sentence in <c>note</c> and it is rendered, not paraphrased — a band called
/// LOW beside a language's name reads as a judgement on the language unless something says
/// otherwise, and that misreading costs a customer.</para>
/// <para>The band vocabulary comes from the payload's own <c>bandCss</c>, so the hue is this site's
/// and the WORD is the product's. Nothing here decides which band a language is in.</para>
/// </remarks>
public static class LanguageSupportTemplate
{
    public const string Name = "language-support";

    public static string? Render(string? json)
    {
        if (Root(json) is not { } root)
        {
            return null;
        }

        var languages = Array(root, "languages")
            .Where(l => Str(l, "displayName").Length > 0)
            .ToList();

        if (languages.Count == 0)
        {
            return null;
        }

        // ★ A LIST, NOT A GRID OF CARDS. These summaries run from eleven words to two hundred and
        //   fifty, and in a grid the row height is set by the tallest card in the row: the first
        //   rendering gave C# a mostly-empty box as tall as Dart's essay. A reference table lets a
        //   long row simply be long, and costs the reader nothing.
        var html = new StringBuilder();
        html.Append("<div class=\"ip-langs\">");

        foreach (var language in languages)
        {
            var band = Str(language, "bandLabel");
            var key = Str(language, "bandCss");

            html.Append("<div class=\"ip-langrow\">");

            html.Append("<div class=\"ip-langrow-head\">");
            html.Append("<h3>").Append(Esc(Str(language, "displayName"))).Append("</h3>");
            if (band.Length > 0)
            {
                html.Append("<span class=\"ip-band-strong")
                    .Append(key.Length > 0 ? $" ip-band-{Esc(key)}" : "").Append("\">")
                    .Append(Esc(band)).Append("</span>");
            }

            if (Str(language, "supportKind") is { Length: > 0 } kind)
            {
                html.Append("<span class=\"ip-cai-unit\">").Append(Esc(kind)).Append("</span>");
            }

            html.Append("</div>");

            html.Append("<div class=\"ip-langrow-body\">");
            if (Str(language, "summary") is { Length: > 0 } summary)
            {
                html.Append("<div class=\"ip-prose\"><p>").Append(Esc(summary)).Append("</p></div>");
            }

            var covered = Strings(language, "coveredLenses");
            if (covered.Count > 0)
            {
                html.Append("<p class=\"ip-lens-list\"><span class=\"ip-lens-label\">Lenses</span>")
                    .Append(string.Join("", covered.Select(l => $"<span class=\"ip-lens\">{Esc(l)}</span>")))
                    .Append("</p>");
            }

            // ★ What does NOT resolve is published beside what does. A coverage table that lists only
            //   the wins reads as a sales page; the reason this one is worth anything to a buyer is
            //   that it says where a language stops.
            var notApplicable = Strings(language, "notApplicableLenses");
            if (notApplicable.Count > 0)
            {
                html.Append("<p class=\"ip-lens-list ip-lens-list-off\"><span class=\"ip-lens-label\">Not applicable</span>")
                    .Append(string.Join("", notApplicable.Select(l => $"<span class=\"ip-lens\">{Esc(l)}</span>")))
                    .Append("</p>");
            }

            html.Append("</div></div>");
        }

        html.Append("</div>");

        if (Str(root, "note") is { Length: > 0 } note)
        {
            html.Append("<div class=\"ip-prose\"><p>").Append(Esc(note)).Append("</p></div>");
        }

        return html.ToString();
    }
}
