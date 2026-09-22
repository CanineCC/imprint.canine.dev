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

            html.Append("<span class=\"ip-linkcard-ico\" aria-hidden=\"true\">")
                .Append(Mark(Str(link, "icon"), href))
                .Append("</span>");

            html.Append("<span class=\"ip-linkcard-label\">").Append(label).Append("</span>");

            // The widget carries these two optional rows and the grid reserves a place for them.
            if (Str(link, "figure") is { Length: > 0 } figure)
            {
                html.Append("<span class=\"ip-linkcard-figure\">").Append(Esc(figure)).Append("</span>");
            }

            if (note.Length > 0)
            {
                html.Append("<span class=\"ip-linkcard-note\">").Append(Esc(note)).Append("</span>");
            }

            if (Str(link, "go") is { Length: > 0 } go)
            {
                html.Append("<span class=\"ip-linkcard-go\">").Append(Esc(go)).Append(" \u2192</span>");
            }

            html.Append(href is not null ? "</a>" : "</span>");
        }

        return html.Append("</div>").ToString();
    }


    /// <summary>
    /// The marks, taken VERBATIM from <c>widgets/_src/cai-link-cards.js</c>.
    /// </summary>
    /// <remarks>
    /// ★★ THE BAKE DROPPED THESE ENTIRELY AND I DID NOT NOTICE. Every card carries an
    /// <c>icon</c> and the first version of this template never read the field, so six cards on
    /// every survey page lost their mark and became two lines of text in a box. The template
    /// rendered, the tests passed, and the page was plainly worse than the island it replaced.
    /// They are copied rather than redrawn so a card cannot disagree with the widget about what
    /// GitHub looks like.
    /// </remarks>
    private static readonly Dictionary<string, string> Icons = new(StringComparer.Ordinal)
    {
        ["github"] =
            """
            <svg viewBox="0 0 16 16" fill="currentColor" aria-hidden="true"><path d="M8 0C3.58 0 0 3.58 0 8c0 3.54 2.29 6.53 5.47 7.59.4.07.55-.17.55-.38 0-.19-.01-.82-.01-1.49-2.01.37-2.53-.49-2.69-.94-.09-.23-.48-.94-.82-1.13-.28-.15-.68-.52-.01-.53.63-.01 1.08.58 1.23.82.72 1.21 1.87.87 2.33.66.07-.52.28-.87.51-1.07-1.78-.2-3.64-.89-3.64-3.95 0-.87.31-1.59.82-2.15-.08-.2-.36-1.02.08-2.12 0 0 .67-.21 2.2.82.64-.18 1.32-.27 2-.27.68 0 1.36.09 2 .27 1.53-1.04 2.2-.82 2.2-.82.44 1.1.16 1.92.08 2.12.51.56.82 1.27.82 2.15 0 3.07-1.87 3.75-3.65 3.95.29.25.54.73.54 1.48 0 1.07-.01 1.93-.01 2.2 0 .21.15.46.55.38A8.01 8.01 0 0 0 16 8c0-4.42-3.58-8-8-8Z"/></svg>
            """,
        ["gitlab"] =
            """
            <svg viewBox="0 0 16 16" fill="currentColor" aria-hidden="true"><path d="m15.73 6.49-.02-.06-2.17-5.66a.57.57 0 0 0-.22-.27.58.58 0 0 0-.88.27L10.98 5.2H5.03L3.57.77a.58.58 0 0 0-.88-.27.57.57 0 0 0-.22.27L.3 6.43l-.02.06a4.03 4.03 0 0 0 1.34 4.65l.01.01.02.01 3.3 2.47 1.63 1.23.99.75a.68.68 0 0 0 .82 0l.99-.75 1.64-1.23 3.32-2.48.01-.01a4.03 4.03 0 0 0 1.34-4.65Z"/></svg>
            """,
        ["html"] =
            """
            <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true"><path d="M1.5 0h21l-1.91 21.563L11.977 24l-8.564-2.438L1.5 0zm7.031 9.75l-.232-2.718 10.059.003.23-2.622L5.412 4.41l.698 8.01h9.126l-.326 3.426-2.91.804-2.955-.81-.188-2.11H6.248l.33 4.171L12 19.351l5.379-1.443.744-8.157H8.531z"/></svg>
            """,
        ["bitbucket"] =
            """
            <svg viewBox="0 0 16 16" fill="currentColor" aria-hidden="true"><path d="M.78 1.02a.63.63 0 0 0-.63.73l2.18 13.2c.05.3.31.53.62.53h10.4c.23 0 .43-.17.47-.4l2.18-13.33a.63.63 0 0 0-.63-.73H.78Zm9.1 9.4H6.16l-1-5.23h5.64l-.92 5.23Z"/></svg>
            """,
        ["azure"] =
            """
            <svg viewBox="0 0 16 16" fill="currentColor" aria-hidden="true"><path d="M15.5 3.62v8.55l-3.5 2.87-5.43-1.98v1.96l-3.07-4.01 8.99.7V4.32L15.5 3.62ZM12.6 4.4 7.2 1.02v2.15L2.24 4.63.5 6.87v4.9l2.06.9V6.42L12.6 4.4Z"/></svg>
            """,
        ["doc"] =
            """
            <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.3" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M9.2 1.4H4a1.6 1.6 0 0 0-1.6 1.6v10A1.6 1.6 0 0 0 4 14.6h8a1.6 1.6 0 0 0 1.6-1.6V5.8Z"/><path d="M9.2 1.4v4.4h4.4"/><path d="M5.6 8.4h4.8M5.6 11h4.8"/></svg>
            """,
    };

    /// <summary>The site's own badge, tinted by mask rather than redrawn — one shape, one mark.</summary>
    private const string Badge = "/brand/canine-badge.svg";

    /// <summary>The Code Assurance Index's own mark — five band chips and the marker on them.</summary>
    /// <remarks>
    /// ★★ NOT THE CANINE BADGE TINTED GREEN. The island this was ported from masks one shared badge
    /// and tints it per product, and that is what shipped here — so a card reading "Verify this score
    /// yourself", pointing at codeassuranceindex.info, wore the studio's badge. The standard has a
    /// logo of its own, and a link to the standard should carry it. It is full colour by design (the
    /// five band hues ARE the mark), so unlike the badge it is not masked.
    /// </remarks>
    private const string CaiMark = "/brand/cai-mark.svg";

    /// <summary>
    /// The mark for a card: our own badge for the two products, an SVG otherwise.
    /// </summary>
    /// <remarks>
    /// ★ An unknown icon falls back to the document, exactly as the widget does. A card with no
    /// mark at all would be a different shape from the ones beside it, and the grid pins every text
    /// row to column 2 on the assumption that column 1 is always filled.
    /// </remarks>
    private static string Mark(string icon, string? href)
    {
        var key = icon.Trim().ToLowerInvariant();
        if (key.Length == 0 || key == "doc")
        {
            // ★★ THE MARK FOLLOWS WHERE THE CARD GOES. Two cards on every survey page point at the
            //    corpus on codeassuranceindex.info and were generated with icon "doc", so they wore
            //    a generic page glyph beside a card for the same site that wore its badge. The
            //    caller's word is honoured when it says something; when it says "a document", the
            //    destination is a better answer than the default, and it cannot go stale the way a
            //    second hand-maintained list of icons would.
            key = FromHref(href) ?? key;
        }

        if (key == "cai")
        {
            return $"<img class=\"ip-linkcard-mark\" src=\"{CaiMark}\" alt=\"\" width=\"26\" height=\"26\" />";
        }

        if (key == "watchdog" || key == "assay")
        {
            return $"<span class=\"ip-linkcard-badge is-{key}\" style=\"--badge:url('{Badge}')\"></span>";
        }

        return Icons.TryGetValue(key, out var svg) ? svg : Icons["doc"];
    }

    /// <summary>Which mark a destination asks for, or null when it does not say.</summary>
    /// <remarks>
    /// ★ HOST FIRST, then the path's own extension. Matched on the host SUFFIX so a preprod or local
    /// address resolves the same as the live one — a mark that is right on prod and wrong everywhere
    /// it is reviewed is a mark nobody trusts.
    /// </remarks>
    private static string? FromHref(string? href)
    {
        if (href is not { Length: > 0 })
        {
            return null;
        }

        if (Uri.TryCreate(href, UriKind.Absolute, out var uri))
        {
            var host = uri.Host.ToLowerInvariant();
            if (Hosts.FirstOrDefault(h => host == h.Host || host.EndsWith("." + h.Host, StringComparison.Ordinal)) is { Mark.Length: > 0 } hit)
            {
                return hit.Mark;
            }
        }

        // A site-relative href has no host, so the PAGE's own site is the only thing it can mean —
        // which the template does not know. Fall back to the extension, and otherwise say nothing.
        var path = (Uri.TryCreate(href, UriKind.Absolute, out var abs) ? abs.AbsolutePath : href).ToLowerInvariant();
        return path.EndsWith(".html", StringComparison.Ordinal) || path.EndsWith(".htm", StringComparison.Ordinal)
            ? "html"
            : null;
    }

    /// <summary>The hosts whose own mark a card should carry.</summary>
    private static readonly (string Host, string Mark)[] Hosts =
    [
        ("codeassuranceindex.info", "cai"),
        ("watchdog.canine.dev", "watchdog"),
        ("assay.canine.dev", "assay"),
        ("github.com", "github"),
        ("gitlab.com", "gitlab"),
        ("bitbucket.org", "bitbucket"),
        ("dev.azure.com", "azure"),
        ("visualstudio.com", "azure"),
    ];

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
