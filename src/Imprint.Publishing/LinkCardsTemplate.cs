using System.Text;
using System.Text.Json;
using static Imprint.Publishing.TemplateJson;

namespace Imprint.Publishing;

/// <summary>
/// A row of reference links — label, note and destination — rendered from the widget's own
/// <c>links</c> prop.
/// </summary>
/// <remarks>
/// <para>★ THESE ARE LINKS, AND LINKS ARE THE ONE THING A SHADOW ROOT MUST NOT SWALLOW. Eight guide
/// pages end with a card of further reading — the public reports, the dimension catalogue, the spec,
/// the methodology page — and every one of those hrefs was inside a shadow root. A crawler following
/// the site could not see them, so the pages they point at lost the only internal link that named
/// what they were for.</para>
/// <para><b>Rendered from props, not from a fetch.</b> The links are authored on the node, so there
/// is no URL to bake and nothing to share between instances — see
/// <c>WidgetDescriptor.PropTemplate</c> for why that decides WHERE this runs.</para>
/// <para>★ Every href is checked before it is emitted. The value is author-supplied JSON inside an
/// attribute, and an <c>href</c> is a script sink: <c>javascript:</c> in one runs on click and
/// HTML-escaping does not touch it.</para>
/// </remarks>
public static class LinkCardsTemplate
{
    public const string Name = "link-cards";

    /// <summary>The prop carrying the links, as a JSON array.</summary>
    private const string LinksProp = "links";

    public static string? Render(Func<string, string?> props)
    {
        ArgumentNullException.ThrowIfNull(props);

        if (Root(props(LinksProp)) is not { ValueKind: JsonValueKind.Array } root)
        {
            return null;
        }

        var links = root.EnumerateArray()
            .Where(l => Str(l, "label").Length > 0)
            .ToList();

        if (links.Count == 0)
        {
            return null;
        }

        var html = new StringBuilder();
        html.Append("<div class=\"ip-grid ip-grid-cards ip-linkcards\">");

        foreach (var link in links)
        {
            var href = Href(Str(link, "href"));
            var label = Esc(Str(link, "label"));
            var note = Str(link, "note");

            // A card whose destination did not survive the check is still worth printing: the LABEL
            // is the reference. A dead link would be worse than a line of text.
            html.Append(href is not null
                ? $"<a class=\"ip-linkcard\" href=\"{Esc(href)}\">"
                : "<span class=\"ip-linkcard\">");

            html.Append("<span class=\"ip-linkcard-label\">").Append(label).Append("</span>");
            if (note.Length > 0)
            {
                html.Append("<span class=\"ip-linkcard-note\">").Append(Esc(note)).Append("</span>");
            }

            html.Append(href is not null ? "</a>" : "</span>");
        }

        return html.Append("</div>").ToString();
    }

    /// <summary>An absolute http(s) URL, a site-relative path, or null. Nothing else is emitted.</summary>
    private static string? Href(string value)
    {
        if (value.Length == 0)
        {
            return null;
        }

        // Site-relative is the common case for internal reading lists and is safe by construction:
        // it names a path on this site and cannot carry a scheme.
        if (value.StartsWith('/') && !value.StartsWith("//", StringComparison.Ordinal))
        {
            return value;
        }

        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
            ? uri.ToString()
            : null;
    }
}
