using System.Net;
using System.Text;
using Imprint.Authoring.Domain;

namespace Imprint.Rendering;

/// <summary>
/// Render-time link resolution over the canonical inline subset. The input already
/// passed <c>CanonicalHtml.TryValidate</c>, so anchors can only look like
/// <c>&lt;a href="…"&gt;</c> with a five-entity-encoded value — a strict scan over that
/// closed grammar is exact where a regex replace over attribute boundaries would be a
/// parser differential.
/// </summary>
public static class RichTextHtml
{
    private const string AnchorOpen = "<a href=\"";
    private const string AnchorClose = "</a>";

    public static string ResolveLinks(string canonicalHtml, Func<PageId, string?> resolvePagePath)
    {
        if (!canonicalHtml.Contains(AnchorOpen, StringComparison.Ordinal))
        {
            return canonicalHtml;
        }

        var result = new StringBuilder(canonicalHtml.Length + 64);
        var pos = 0;
        while (pos < canonicalHtml.Length)
        {
            var start = canonicalHtml.IndexOf(AnchorOpen, pos, StringComparison.Ordinal);
            if (start < 0)
            {
                result.Append(canonicalHtml, pos, canonicalHtml.Length - pos);
                break;
            }

            result.Append(canonicalHtml, pos, start - pos);
            var hrefStart = start + AnchorOpen.Length;
            var hrefEnd = canonicalHtml.IndexOf('"', hrefStart);
            if (hrefEnd < 0 || hrefEnd + 1 >= canonicalHtml.Length || canonicalHtml[hrefEnd + 1] != '>')
            {
                // Defensive: unreachable for validated content. Emitting the rest
                // verbatim keeps a corrupted stream visible instead of hiding it.
                result.Append(canonicalHtml, start, canonicalHtml.Length - start);
                break;
            }

            var rawHref = canonicalHtml[hrefStart..hrefEnd];
            pos = hrefEnd + 2;

            var decoded = DecodeEntities(rawHref);
            if (decoded.StartsWith("page:", StringComparison.OrdinalIgnoreCase) &&
                Guid.TryParse(decoded["page:".Length..], out var pageGuid))
            {
                var path = resolvePagePath(PageId.From(pageGuid));
                if (path is null)
                {
                    // Broken page reference: unwrap the anchor, keep its inline content
                    // — a dead link is worse than plain text. No nesting in the grammar,
                    // so the next </a> is always ours.
                    var close = canonicalHtml.IndexOf(AnchorClose, pos, StringComparison.Ordinal);
                    if (close < 0)
                    {
                        result.Append(canonicalHtml, pos, canonicalHtml.Length - pos);
                        break;
                    }

                    result.Append(canonicalHtml, pos, close - pos);
                    pos = close + AnchorClose.Length;
                    continue;
                }

                result.Append(AnchorOpen).Append(WebUtility.HtmlEncode(path)).Append("\">");
            }
            else
            {
                // External (https/http/mailto): the stored href is already canonically
                // encoded, so it is re-emitted verbatim; noopener because authors link
                // to hosts the site owner does not control.
                result.Append(AnchorOpen).Append(rawHref).Append("\" rel=\"noopener\">");
            }
        }

        return result.ToString();
    }

    private static string DecodeEntities(string value) =>
        // &amp; last, so double-encoded input cannot decode twice.
        value
            .Replace("&lt;", "<", StringComparison.Ordinal)
            .Replace("&gt;", ">", StringComparison.Ordinal)
            .Replace("&quot;", "\"", StringComparison.Ordinal)
            .Replace("&#39;", "'", StringComparison.Ordinal)
            .Replace("&amp;", "&", StringComparison.Ordinal);
}
