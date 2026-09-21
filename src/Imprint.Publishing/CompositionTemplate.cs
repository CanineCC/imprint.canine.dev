using System.Text;
using System.Text.Json;
using static Imprint.Publishing.TemplateJson;

namespace Imprint.Publishing;

/// <summary>
/// The file-quality mix — brilliant / fine / slop — per published repository, rendered from
/// <c>/api/public/insights</c> into this site's own markup.
/// </summary>
/// <remarks>
/// <para>★ A BAR IS NOT A PICTURE HERE. The three shares are printed as words beside it, so the
/// section says what it means with the stylesheet switched off, to a crawler, and to a screen reader.
/// The bar is decoration over text, which is the opposite way round from the frame it replaces.</para>
/// <para><b>The last entry, not the first.</b> <c>fileQuality</c> is a SERIES over the repository's
/// scans; the current mix is its last element. Reading element zero would publish the mix the
/// repository had when it was first surveyed — which for a repository that improved is precisely the
/// number it would least like shown, and it would look right.</para>
/// </remarks>
public static class CompositionTemplate
{
    public const string Name = "composition";

    /// <summary>Repositories shown. The point is the spread between them, which four carries.</summary>
    private const int Repositories = 4;

    public static string? Render(string? json, string? origin)
    {
        if (Root(json) is not { } root)
        {
            return null;
        }

        var rows = new List<(string Display, double Brilliant, double Fine, double Slop, string? Href)>();
        foreach (var item in Array(root, "items"))
        {
            var series = Array(item, "fileQuality");
            if (series.Count == 0 || Str(item, "display").Length == 0)
            {
                continue;
            }

            var current = series[^1];
            var brilliant = Number(current, "brilliant");
            var fine = Number(current, "fine");
            var slop = Number(current, "slop");
            if (brilliant is null || fine is null || slop is null)
            {
                continue;
            }

            rows.Add((Str(item, "display"), brilliant.Value, fine.Value, slop.Value,
                Link(origin, Str(item, "reportUrl"))));

            if (rows.Count == Repositories)
            {
                break;
            }
        }

        if (rows.Count == 0)
        {
            return null;
        }

        var html = new StringBuilder();
        html.Append("<div class=\"ip-stack ip-mix-list\">");
        foreach (var (display, brilliant, fine, slop, href) in rows)
        {
            html.Append("<div class=\"ip-mix\">");
            html.Append("<p class=\"ip-mix-name\">");
            if (href is not null)
            {
                html.Append("<a href=\"").Append(Esc(href)).Append("\">").Append(Esc(display)).Append("</a>");
            }
            else
            {
                html.Append(Esc(display));
            }

            html.Append("</p>");

            html.Append("<div class=\"ip-mix-track\" role=\"img\" aria-label=\"")
                .Append(Esc($"{Score(brilliant)} % brilliant, {Score(fine)} % fine, {Score(slop)} % slop"))
                .Append("\">");
            Segment(html, "brilliant", brilliant);
            Segment(html, "fine", fine);
            Segment(html, "slop", slop);
            html.Append("</div>");

            html.Append("<p class=\"ip-mix-legend\">")
                .Append("<span class=\"ip-mix-key ip-mix-key-brilliant\">").Append(Esc(Score(brilliant))).Append(" % brilliant</span>")
                .Append("<span class=\"ip-mix-key ip-mix-key-fine\">").Append(Esc(Score(fine))).Append(" % fine</span>")
                .Append("<span class=\"ip-mix-key ip-mix-key-slop\">").Append(Esc(Score(slop))).Append(" % slop</span>")
                .Append("</p>");

            html.Append("</div>");
        }

        return html.Append("</div>").ToString();
    }

    /// <summary>
    /// One share of the bar. A zero share emits nothing rather than a zero-width span — an empty
    /// element still takes the border and reads as a sliver of a category that is not there.
    /// </summary>
    private static void Segment(StringBuilder html, string kind, double share)
    {
        if (share <= 0)
        {
            return;
        }

        html.Append("<span class=\"ip-mix-seg ip-mix-seg-").Append(kind)
            .Append("\" style=\"width:").Append(Esc(Score(Math.Clamp(share, 0, 100)))).Append("%\"></span>");
    }
}
