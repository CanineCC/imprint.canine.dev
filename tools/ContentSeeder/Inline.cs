using System.Text;
using System.Text.RegularExpressions;

namespace ContentSeeder;

/// <summary>
/// Converts the CMS's tiny inline-markup subset (<c>**bold**</c>, <c>`code`</c>,
/// <c>[label](href)</c> — see packages/ui/src/inline.tsx) into Imprint's canonical
/// inline HTML grammar (<c>&lt;strong&gt; &lt;em&gt; &lt;a href&gt; &lt;br&gt;</c> plus the
/// five entities — see Imprint.Authoring.Domain.Pages.CanonicalHtml). This is the ONE
/// place copy is transformed, and every rule is faithful:
/// <list type="bullet">
///   <item><c>**bold**</c> → <c>&lt;strong&gt;…&lt;/strong&gt;</c> (verbatim to the source renderer).</item>
///   <item><c>`code`</c> → <c>&lt;strong&gt;…&lt;/strong&gt;</c>. Imprint's canonical subset has NO
///   <c>&lt;code&gt;</c>; per the migration spec we render inline code as <c>&lt;strong&gt;</c>
///   (bold), the closest faithful in-subset emphasis. Copy is unchanged.</item>
///   <item><c>[label](href)</c> → <c>&lt;a href="…"&gt;label&lt;/a&gt;</c>. CanonicalHtml only accepts
///   https/http/mailto/page: hrefs, so a SITE-RELATIVE href (e.g. <c>/pricing</c>) is
///   resolved against the site's own public origin (e.g. <c>https://watchdog.canine.dev/pricing</c>) —
///   the real deployed destination, so the link target stays truthful. An anchor/query
///   fragment (<c>#…</c>) with no path is dropped to plain text (no valid target).</item>
/// </list>
/// All literal <c>&amp; &lt; &gt;</c> in text are escaped to entities.
/// </summary>
public static class Inline
{
    // One-pass tokenizer identical to inline.tsx's regex (bold | code | link).
    private static readonly Regex Token = new(
        @"(\*\*[^*]+\*\*|`[^`]+`|\[[^\]]+\]\([^)]+\))",
        RegexOptions.Compiled);

    private static readonly Regex Link = new(@"^\[([^\]]+)\]\(([^)]+)\)$", RegexOptions.Compiled);

    /// <summary>Render inline markup to canonical inline HTML (no block wrapper).</summary>
    public static string ToCanonicalInline(string? text, string siteOrigin)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(text.Length + 16);
        var last = 0;
        foreach (Match m in Token.Matches(text))
        {
            if (m.Index > last)
            {
                sb.Append(Escape(text[last..m.Index]));
            }

            var tok = m.Value;
            if (tok.StartsWith("**", StringComparison.Ordinal))
            {
                sb.Append("<strong>").Append(Escape(tok[2..^2])).Append("</strong>");
            }
            else if (tok.StartsWith('`'))
            {
                // No <code> in the canonical subset — faithful fallback to <strong>.
                sb.Append("<strong>").Append(Escape(tok[1..^1])).Append("</strong>");
            }
            else
            {
                var lm = Link.Match(tok);
                if (lm.Success)
                {
                    var label = lm.Groups[1].Value;
                    var href = ResolveHref(lm.Groups[2].Value.Trim(), siteOrigin);
                    if (href is not null)
                    {
                        sb.Append("<a href=\"").Append(EscapeAttr(href)).Append("\">")
                          .Append(Escape(label)).Append("</a>");
                    }
                    else
                    {
                        // Un-resolvable target (bare #fragment): keep the label as plain text.
                        sb.Append(Escape(label));
                    }
                }
                else
                {
                    sb.Append(Escape(tok));
                }
            }

            last = m.Index + tok.Length;
        }

        if (last < text.Length)
        {
            sb.Append(Escape(text[last..]));
        }

        return sb.ToString();
    }

    /// <summary>Wrap inline markup as a single canonical <c>&lt;p&gt;</c> block (empty → empty string).</summary>
    public static string ToParagraph(string? text, string siteOrigin)
    {
        var inline = ToCanonicalInline(text, siteOrigin);
        return inline.Length == 0 ? string.Empty : $"<p>{inline}</p>";
    }

    /// <summary>
    /// Resolve a CMS href to a CanonicalHtml-allowed href, or null when there is no valid
    /// target. https/http/mailto pass through; a site-relative path is made absolute against
    /// the site origin; a bare fragment/query has no path so it is dropped.
    /// </summary>
    public static string? ResolveHref(string href, string siteOrigin)
    {
        if (href.Length == 0)
        {
            return null;
        }

        if (href.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            href.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
        {
            return href;
        }

        // Site-relative ("/pricing", "/reports/tender", "/contact?topic=onprem") →
        // absolute against the site's own public origin: the real deployed destination.
        if (href.StartsWith('/'))
        {
            return siteOrigin.TrimEnd('/') + href;
        }

        // A pure in-page anchor ("#faq") has no page target in a static export — drop it.
        if (href.StartsWith('#'))
        {
            return null;
        }

        // Anything else (a bare relative "about") — resolve against origin defensively.
        return siteOrigin.TrimEnd('/') + "/" + href;
    }

    /// <summary>Escape text for canonical HTML body (only &amp; &lt; &gt; are meaningful there).</summary>
    public static string Escape(string s) => s
        .Replace("&", "&amp;", StringComparison.Ordinal)
        .Replace("<", "&lt;", StringComparison.Ordinal)
        .Replace(">", "&gt;", StringComparison.Ordinal);

    /// <summary>Escape a value destined for an <c>href="…"</c> attribute (adds quote escaping).</summary>
    public static string EscapeAttr(string s) => Escape(s)
        .Replace("\"", "&quot;", StringComparison.Ordinal);
}
