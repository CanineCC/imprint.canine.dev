using System.Text;
using System.Text.RegularExpressions;

namespace Imprint.Authoring.Domain.Pages;

/// <summary>
/// The strict validator for the canonical inline subset — the only place stored
/// content resembles HTML (docs/domain-model.md §2.2). The editor's JS normalizes
/// contenteditable output to this grammar as a courtesy; this validator is the
/// guarantee. It REJECTS non-conforming input rather than fixing it: a validator that
/// rewrites is a parser differential waiting to happen.
///
/// Grammar (exact, case-sensitive):
///   document := ws (block ws)*
///   block    := &lt;p&gt; inline* &lt;/p&gt; | &lt;ul&gt; (ws item)+ ws &lt;/ul&gt; | &lt;ol&gt; …
///   item     := &lt;li&gt; inline* &lt;/li&gt;
///   inline   := text | entity | &lt;br&gt; | &lt;strong&gt; inline* &lt;/strong&gt;
///             | &lt;em&gt; inline* &lt;/em&gt; | &lt;a href="url"&gt; inline-no-a* &lt;/a&gt;
///   entity   := &amp;amp; | &amp;lt; | &amp;gt; | &amp;quot; | &amp;#39;
///   text     := any chars except '&lt;' and '&amp;'
/// Anchors: no nesting; href scheme ∈ { https, http, mailto, page:{guid} }.
/// </summary>
public static partial class CanonicalHtml
{
    public const int MaxLength = 20_000;
    private const int MaxInlineDepth = 6;

    private static readonly string[] Entities = ["&amp;", "&lt;", "&gt;", "&quot;", "&#39;"];

    public static bool TryValidate(string html, out string? error)
    {
        error = Validate(html);
        return error is null;
    }

    private static string? Validate(string html)
    {
        if (html.Length > MaxLength)
        {
            return $"Text is longer than {MaxLength} characters.";
        }

        var pos = 0;
        SkipWhitespace(html, ref pos);
        while (pos < html.Length)
        {
            var blockError = ParseBlock(html, ref pos);
            if (blockError is not null)
            {
                return blockError;
            }

            SkipWhitespace(html, ref pos);
        }

        return null;
    }

    private static string? ParseBlock(string html, ref int pos)
    {
        if (Consume(html, ref pos, "<p>"))
        {
            return ParseInlineUntil(html, ref pos, "</p>", allowAnchor: true, depth: 0);
        }

        if (Consume(html, ref pos, "<ul>"))
        {
            return ParseListItems(html, ref pos, "</ul>");
        }

        if (Consume(html, ref pos, "<ol>"))
        {
            return ParseListItems(html, ref pos, "</ol>");
        }

        return $"Expected <p>, <ul> or <ol> at position {pos}.";
    }

    private static string? ParseListItems(string html, ref int pos, string closing)
    {
        var items = 0;
        while (true)
        {
            SkipWhitespace(html, ref pos);
            if (Consume(html, ref pos, closing))
            {
                return items > 0 ? null : $"Empty list before position {pos}.";
            }

            if (!Consume(html, ref pos, "<li>"))
            {
                return $"Expected <li> or {closing} at position {pos}.";
            }

            var itemError = ParseInlineUntil(html, ref pos, "</li>", allowAnchor: true, depth: 0);
            if (itemError is not null)
            {
                return itemError;
            }

            items++;
        }
    }

    private static string? ParseInlineUntil(string html, ref int pos, string closing, bool allowAnchor, int depth)
    {
        if (depth > MaxInlineDepth)
        {
            return "Formatting is nested too deeply.";
        }

        while (true)
        {
            if (pos >= html.Length)
            {
                return $"Missing {closing}.";
            }

            var current = html[pos];
            if (current == '<')
            {
                if (Consume(html, ref pos, closing))
                {
                    return null;
                }

                if (Consume(html, ref pos, "<br>"))
                {
                    continue;
                }

                if (Consume(html, ref pos, "<strong>"))
                {
                    var nested = ParseInlineUntil(html, ref pos, "</strong>", allowAnchor, depth + 1);
                    if (nested is not null)
                    {
                        return nested;
                    }

                    continue;
                }

                if (Consume(html, ref pos, "<em>"))
                {
                    var nested = ParseInlineUntil(html, ref pos, "</em>", allowAnchor, depth + 1);
                    if (nested is not null)
                    {
                        return nested;
                    }

                    continue;
                }

                if (Consume(html, ref pos, "<a href=\""))
                {
                    if (!allowAnchor)
                    {
                        return "Links cannot contain links.";
                    }

                    var hrefError = ParseHref(html, ref pos);
                    if (hrefError is not null)
                    {
                        return hrefError;
                    }

                    var nested = ParseInlineUntil(html, ref pos, "</a>", allowAnchor: false, depth + 1);
                    if (nested is not null)
                    {
                        return nested;
                    }

                    continue;
                }

                return $"Disallowed tag at position {pos}.";
            }

            if (current == '&')
            {
                if (!ConsumeAnyEntity(html, ref pos, out _))
                {
                    return $"Disallowed entity at position {pos} (only &amp; &lt; &gt; &quot; &#39;).";
                }

                continue;
            }

            pos++;
        }
    }

    private static string? ParseHref(string html, ref int pos)
    {
        var value = new StringBuilder();
        while (true)
        {
            if (pos >= html.Length)
            {
                return "Unterminated link address.";
            }

            var current = html[pos];
            if (current == '"')
            {
                pos++;
                if (!Consume(html, ref pos, ">"))
                {
                    return $"Links may only have an href attribute (position {pos}).";
                }

                break;
            }

            if (current == '<')
            {
                return "Invalid character in link address.";
            }

            if (current == '&')
            {
                if (!ConsumeAnyEntity(html, ref pos, out var matched))
                {
                    return "Invalid entity in link address.";
                }

                value.Append(DecodeEntity(matched));
                continue;
            }

            value.Append(current);
            pos++;
        }

        return IsAllowedHref(value.ToString())
            ? null
            : "Links must be https, http, mailto or a page reference.";
    }

    /// <summary>Scheme allowlist, checked on the entity-decoded value. Never a blocklist.</summary>
    public static bool IsAllowedHref(string href)
    {
        if (href.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            href.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            return href.Length > 8 && !href.Contains(' ', StringComparison.Ordinal);
        }

        if (href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
        {
            return href.Length > "mailto:".Length;
        }

        if (href.StartsWith("page:", StringComparison.OrdinalIgnoreCase))
        {
            return Guid.TryParse(href["page:".Length..], out _);
        }

        return false;
    }

    /// <summary>
    /// Tolerant tag-strip for previews and translation coverage. Only ever applied to
    /// content that already passed <see cref="TryValidate"/> — never a sanitizer.
    /// </summary>
    public static string ToPlainText(string html)
    {
        var text = StripTags().Replace(html, " ");
        text = text
            .Replace("&lt;", "<").Replace("&gt;", ">")
            .Replace("&quot;", "\"").Replace("&#39;", "'")
            .Replace("&amp;", "&");
        return CollapseWhitespace().Replace(text, " ").Trim();
    }

    private static void SkipWhitespace(string html, ref int pos)
    {
        while (pos < html.Length && char.IsWhiteSpace(html[pos]))
        {
            pos++;
        }
    }

    private static bool ConsumeAnyEntity(string html, ref int pos, out string matched)
    {
        foreach (var entity in Entities)
        {
            if (Consume(html, ref pos, entity))
            {
                matched = entity;
                return true;
            }
        }

        matched = string.Empty;
        return false;
    }

    private static bool Consume(string html, ref int pos, string expected)
    {
        if (html.AsSpan(pos).StartsWith(expected, StringComparison.Ordinal))
        {
            pos += expected.Length;
            return true;
        }

        return false;
    }

    private static string DecodeEntity(string entity) => entity switch
    {
        "&amp;" => "&",
        "&lt;" => "<",
        "&gt;" => ">",
        "&quot;" => "\"",
        "&#39;" => "'",
        _ => entity,
    };

    [GeneratedRegex("<[^>]*>")]
    private static partial Regex StripTags();

    [GeneratedRegex(@"\s+")]
    private static partial Regex CollapseWhitespace();
}
