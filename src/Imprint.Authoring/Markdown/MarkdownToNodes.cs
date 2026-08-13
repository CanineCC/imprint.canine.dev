using System.Text;
using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;

namespace Imprint.Authoring.Markdown;

/// <summary>One thing in the source this converter will not represent, and where it is.</summary>
/// <param name="Line">1-based line in the markdown source.</param>
/// <param name="Message">What is wrong, in the author's terms — it is shown in the editor.</param>
public sealed record MarkdownProblem(int Line, string Message);

/// <summary>The result of converting markdown: the nodes, and everything refused.</summary>
/// <remarks><see cref="Nodes"/> holds what DID convert even when <see cref="Problems"/> is
/// non-empty, so an editor can show a preview and the errors together rather than a blank pane.
/// A caller that is about to persist should refuse unless <see cref="Ok"/>.</remarks>
public sealed record MarkdownConversion(NodeList Nodes, IReadOnlyList<MarkdownProblem> Problems)
{
    public bool Ok => Problems.Count == 0;
}

/// <summary>
/// Converts a closed subset of markdown into the page node vocabulary.
///
/// <para><b>Why a subset, and why hand-written.</b> The node union is closed and the inline
/// grammar (<see cref="CanonicalHtml"/>) is closed with it, so most of CommonMark has nowhere
/// to land: there is no node for a table, a blockquote or a nested list, and no inline element
/// for code. A general parser would therefore spend its time producing constructs this system
/// must then drop — and silently dropping an author's table is worse than refusing it. So the
/// grammar here is exactly what the vocabulary can hold, and everything else is REPORTED at its
/// line rather than fixed or discarded. That is the same rule <see cref="CanonicalHtml"/>
/// follows, for the same reason: a converter that rewrites is a parser differential waiting to
/// happen. It also keeps the runtime dependency allowlist closed (docs/conventions.md).</para>
///
/// <para><b>The grammar.</b> ATX headings <c>#</c>–<c>######</c>; paragraphs; unordered
/// (<c>- * +</c>) and ordered (<c>1.</c>) lists, one level; fenced code with an optional
/// language; thematic breaks (<c>---</c>, <c>***</c>, <c>___</c>); an image as its own
/// paragraph. Inline: <c>**strong**</c>, <c>*em*</c>, <c>[text](href)</c>, a hard break from
/// two trailing spaces or a trailing backslash, and <c>\</c> escapes.</para>
///
/// <para><b>Links and images are addresses, not URLs.</b> An internal link is written
/// <c>page:{guid}</c> and an image <c>media:{guid}</c>, because those are what resolve at render
/// — the first to the reader's own locale, the second to the WebP variant set. A relative path
/// would publish a link that breaks on every translation and an image with no <c>srcset</c>, so
/// both are refused with a message saying what to write instead.</para>
/// </summary>
public static class MarkdownToNodes
{
    /// <summary>Convert <paramref name="markdown"/> for <paramref name="locale"/>.</summary>
    /// <param name="newId">Node id source. Injectable so a test (or a re-import that wants stable
    /// ids) is not at the mercy of <see cref="NodeId.New"/>.</param>
    public static MarkdownConversion Convert(string markdown, Locale locale, Func<NodeId>? newId = null)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        var id = newId ?? NodeId.New;
        var problems = new List<MarkdownProblem>();
        var nodes = new List<Node>();
        var lines = markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

        for (var i = 0; i < lines.Length;)
        {
            var line = lines[i];

            if (line.Trim().Length == 0)
            {
                i++;
                continue;
            }

            if (IndentOf(line) >= 4)
            {
                // An indented code block. Refused rather than converted because it is
                // indistinguishable from an over-indented paragraph, and the fence says what the
                // author meant.
                problems.Add(new MarkdownProblem(i + 1, "Indented code is not supported — fence it with ``` instead."));
                i = SkipWhile(lines, i, l => l.Trim().Length > 0);
                continue;
            }

            if (TryFence(line, out var fence, out var language))
            {
                i = ReadFencedCode(lines, i, fence, language, id, nodes, problems);
                continue;
            }

            if (IsThematicBreak(line))
            {
                nodes.Add(new DividerNode { Id = id() });
                i++;
                continue;
            }

            if (TryHeading(line, out var level, out var headingText))
            {
                nodes.Add(new HeadingNode
                {
                    Id = id(),
                    Level = level,
                    // A heading is plain text on the node, so inline markup in it has no home.
                    Text = LocalizedText.Empty.With(locale, Unescape(headingText)),
                });
                i++;
                continue;
            }

            if (RejectedBlockPrefix(line) is { } rejection)
            {
                problems.Add(new MarkdownProblem(i + 1, rejection));
                i = SkipWhile(lines, i, l => l.Trim().Length > 0);
                continue;
            }

            if (IsListItem(line, out _))
            {
                i = ReadList(lines, i, locale, id, nodes, problems);
                continue;
            }

            i = ReadParagraph(lines, i, locale, id, nodes, problems);
        }

        return new MarkdownConversion(NodeList.Of(nodes), problems);
    }

    // ------------------------------------------------------------------ block readers

    private static int ReadFencedCode(
        string[] lines, int start, string fence, string? language,
        Func<NodeId> id, List<Node> nodes, List<MarkdownProblem> problems)
    {
        if (language is not null && !CodeNode.IsValidLanguage(language))
        {
            problems.Add(new MarkdownProblem(start + 1, $"'{language}' is not a usable language tag (letters, digits, + # - only)."));
            language = null;
        }

        var body = new List<string>();
        var i = start + 1;
        for (; i < lines.Length; i++)
        {
            if (lines[i].TrimEnd().StartsWith(fence, StringComparison.Ordinal) && lines[i].Trim().All(c => c == fence[0]))
            {
                nodes.Add(new CodeNode { Id = id(), Text = string.Join('\n', body), Language = language });
                return i + 1;
            }

            body.Add(lines[i]);
        }

        // Running off the end means the fence was never closed. The text is kept — losing an
        // author's code because they forgot three backticks would be the worse failure — but the
        // problem is reported so it cannot be saved in that state.
        problems.Add(new MarkdownProblem(start + 1, "This code fence is never closed."));
        nodes.Add(new CodeNode { Id = id(), Text = string.Join('\n', body), Language = language });
        return i;
    }

    private static int ReadList(
        string[] lines, int start, Locale locale, Func<NodeId> id,
        List<Node> nodes, List<MarkdownProblem> problems)
    {
        IsListItem(lines[start], out var ordered);
        var items = new List<string>();
        var i = start;

        for (; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line.Trim().Length == 0)
            {
                break;
            }

            if (IsListItem(line, out var itemOrdered))
            {
                if (IndentOf(line) >= 2)
                {
                    problems.Add(new MarkdownProblem(i + 1, "Nested lists are not supported — keep list items at one level."));
                    return SkipWhile(lines, i, l => l.Trim().Length > 0);
                }

                if (itemOrdered != ordered)
                {
                    break;  // a bulleted list touching a numbered one: two lists, not one
                }

                items.Add(ItemText(line));
                continue;
            }

            if (items.Count > 0)
            {
                items[^1] += " " + line.Trim();   // a continuation line belongs to the item above
                continue;
            }

            break;
        }

        var html = new StringBuilder();
        var tag = ordered ? "ol" : "ul";
        html.Append('<').Append(tag).Append('>');
        foreach (var item in items)
        {
            html.Append("<li>").Append(Inline(item, start + 1, problems)).Append("</li>");
        }

        html.Append("</").Append(tag).Append('>');
        AddRichText(html.ToString(), locale, start + 1, id, nodes, problems);
        return i;
    }

    private static int ReadParagraph(
        string[] lines, int start, Locale locale, Func<NodeId> id,
        List<Node> nodes, List<MarkdownProblem> problems)
    {
        var i = start;
        var text = new StringBuilder();

        for (; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line.Trim().Length == 0 || IsThematicBreak(line) || IsListItem(line, out _) ||
                TryHeading(line, out _, out _) || TryFence(line, out _, out _))
            {
                break;
            }

            if (IsSetextUnderline(line) && text.Length > 0)
            {
                // `Title` + `===` underneath. Refused rather than guessed at: the `---` form is
                // also a thematic break, and silently turning a paragraph into a heading (or a
                // heading into a rule) on the strength of the next line is exactly the kind of
                // reinterpretation this converter does not do.
                problems.Add(new MarkdownProblem(i + 1, "Underlined headings are not supported — write '## Title' instead."));
                return i + 1;
            }

            if (text.Length > 0)
            {
                text.Append(HasHardBreak(lines[i - 1]) ? "<br>" : " ");
            }

            text.Append(line.Trim());
        }

        var source = text.ToString();
        if (TryImageOnly(source, out var alt, out var assetRef))
        {
            AddImage(alt, assetRef, locale, start + 1, id, nodes, problems);
            return i;
        }

        AddRichText("<p>" + Inline(source, start + 1, problems) + "</p>", locale, start + 1, id, nodes, problems);
        return i;
    }

    private static void AddImage(
        string alt, string assetRef, Locale locale, int line,
        Func<NodeId> id, List<Node> nodes, List<MarkdownProblem> problems)
    {
        if (!assetRef.StartsWith("media:", StringComparison.Ordinal) ||
            !Guid.TryParse(assetRef["media:".Length..], out var guid))
        {
            problems.Add(new MarkdownProblem(line,
                "An image must reference the media library as 'media:{id}' so it publishes with a srcset — a path or URL cannot."));
            return;
        }

        nodes.Add(new ImageNode
        {
            Id = id(),
            AssetId = AssetId.From(guid),
            Alt = LocalizedText.Empty.With(locale, Unescape(alt)),
        });
    }

    /// <summary>Adds a rich-text node after checking the html we just produced against the
    /// canonical grammar. The converter is the only writer that builds this html by hand, so
    /// validating its own output is what stops a subset bug from reaching the store as content the
    /// aggregate would reject (or worse, accept).</summary>
    private static void AddRichText(
        string html, Locale locale, int line, Func<NodeId> id,
        List<Node> nodes, List<MarkdownProblem> problems)
    {
        if (!CanonicalHtml.TryValidate(html, out var error))
        {
            problems.Add(new MarkdownProblem(line, $"This text could not be represented: {error}"));
            return;
        }

        nodes.Add(new RichTextNode { Id = id(), Html = LocalizedText.Empty.With(locale, html) });
    }

    // ----------------------------------------------------------------- inline conversion

    private static string Inline(string source, int line, List<MarkdownProblem> problems)
    {
        var html = new StringBuilder();
        var strong = false;
        var em = false;

        for (var i = 0; i < source.Length; i++)
        {
            var c = source[i];

            if (c == '\\' && i + 1 < source.Length && IsEscapable(source[i + 1]))
            {
                Append(html, source[++i]);
                continue;
            }

            if (c == '`')
            {
                problems.Add(new MarkdownProblem(line, "Inline code is not supported — use a fenced ``` block."));
                return html.ToString();
            }

            // Checked BEFORE the raw-HTML guard below, which would otherwise reject it: this <br>
            // is ours, spliced in by the paragraph reader for a hard break, not something the
            // author typed.
            if (source.AsSpan(i).StartsWith("<br>", StringComparison.Ordinal))
            {
                html.Append("<br>");
                i += 3;
                continue;
            }

            if (c == '<' && i + 1 < source.Length && (char.IsAsciiLetter(source[i + 1]) || source[i + 1] is '/' or '!'))
            {
                problems.Add(new MarkdownProblem(line, "Raw HTML is not supported — write markdown, or use a widget."));
                return html.ToString();
            }

            if (c == '!' && i + 1 < source.Length && source[i + 1] == '[')
            {
                problems.Add(new MarkdownProblem(line, "An image must be its own paragraph — there is no inline image."));
                return html.ToString();
            }

            if (c == '[' && TryLink(source, i, out var text, out var href, out var length))
            {
                if (LinkProblem(href) is { } problem)
                {
                    problems.Add(new MarkdownProblem(line, problem));
                    return html.ToString();
                }

                html.Append("<a href=\"").Append(href).Append("\">").Append(EscapeAll(text)).Append("</a>");
                i += length - 1;
                continue;
            }

            if (source.AsSpan(i).StartsWith("**", StringComparison.Ordinal) || source.AsSpan(i).StartsWith("__", StringComparison.Ordinal))
            {
                html.Append(strong ? "</strong>" : "<strong>");
                strong = !strong;
                i++;
                continue;
            }

            if (c is '*' or '_')
            {
                html.Append(em ? "</em>" : "<em>");
                em = !em;
                continue;
            }

            Append(html, c);
        }

        // An unmatched marker is reported at the author's line — far more use than the validator's
        // "unexpected </p>" — and then CLOSED, so the preview still renders and they get one clear
        // message instead of that message plus a grammar error describing the same mistake.
        if (strong || em)
        {
            problems.Add(new MarkdownProblem(line, "Unmatched emphasis marker (* or _) — close it, or escape it as \\*."));
            if (em) { html.Append("</em>"); }
            if (strong) { html.Append("</strong>"); }
        }

        return html.ToString();
    }

    private static string? LinkProblem(string href) =>
        href.StartsWith("https://", StringComparison.Ordinal) ||
        href.StartsWith("http://", StringComparison.Ordinal) ||
        href.StartsWith("mailto:", StringComparison.Ordinal) ||
        href.StartsWith('#') ||
        (href.StartsWith("page:", StringComparison.Ordinal) && Guid.TryParse(href["page:".Length..].Split('#')[0], out _))
            ? null
            : $"'{href}' is not a link this site can resolve — use https:, mailto:, #section, or page:{{id}} for another page here.";

    // -------------------------------------------------------------------------- scanning

    private static bool TryLink(string source, int start, out string text, out string href, out int length)
    {
        text = href = "";
        length = 0;
        var close = source.IndexOf(']', start + 1);
        if (close < 0 || close + 1 >= source.Length || source[close + 1] != '(')
        {
            return false;
        }

        var end = source.IndexOf(')', close + 2);
        if (end < 0)
        {
            return false;
        }

        text = source[(start + 1)..close];
        href = source[(close + 2)..end].Trim();
        length = end - start + 1;
        return text.IndexOf('[', StringComparison.Ordinal) < 0;   // no nested anchors, per the grammar
    }

    private static bool TryImageOnly(string source, out string alt, out string assetRef)
    {
        alt = assetRef = "";
        var trimmed = source.Trim();
        if (!trimmed.StartsWith("![", StringComparison.Ordinal) || !trimmed.EndsWith(')'))
        {
            return false;
        }

        var close = trimmed.IndexOf(']', 2);
        if (close < 0 || close + 1 >= trimmed.Length || trimmed[close + 1] != '(')
        {
            return false;
        }

        alt = trimmed[2..close];
        assetRef = trimmed[(close + 2)..^1].Trim();
        return true;
    }

    private static bool TryHeading(string line, out int level, out string text)
    {
        level = 0;
        text = "";
        var trimmed = line.TrimStart();
        while (level < trimmed.Length && trimmed[level] == '#')
        {
            level++;
        }

        if (level is 0 or > 6 || level >= trimmed.Length || trimmed[level] != ' ')
        {
            return false;
        }

        text = trimmed[(level + 1)..].Trim().TrimEnd('#').Trim();
        return true;
    }

    private static bool TryFence(string line, out string fence, out string? language)
    {
        fence = "";
        language = null;
        var trimmed = line.TrimStart();
        var marker = trimmed.StartsWith("```", StringComparison.Ordinal) ? "```"
            : trimmed.StartsWith("~~~", StringComparison.Ordinal) ? "~~~"
            : null;
        if (marker is null)
        {
            return false;
        }

        fence = marker;
        var info = trimmed[marker.Length..].Trim();
        language = info.Length == 0 ? null : info.Split(' ')[0];
        return true;
    }

    private static string? RejectedBlockPrefix(string line)
    {
        var trimmed = line.TrimStart();
        return trimmed.StartsWith('>') ? "Blockquotes are not supported."
            : trimmed.StartsWith('|') ? "Tables are not supported — a table needs a node type this vocabulary does not have."
            : null;
    }

    private static bool IsListItem(string line, out bool ordered)
    {
        ordered = false;
        var trimmed = line.TrimStart();
        if (trimmed.Length >= 2 && trimmed[0] is '-' or '*' or '+' && trimmed[1] == ' ')
        {
            return !IsThematicBreak(line);
        }

        var digits = 0;
        while (digits < trimmed.Length && char.IsAsciiDigit(trimmed[digits]))
        {
            digits++;
        }

        if (digits > 0 && digits + 1 < trimmed.Length && trimmed[digits] is '.' or ')' && trimmed[digits + 1] == ' ')
        {
            ordered = true;
            return true;
        }

        return false;
    }

    private static string ItemText(string line)
    {
        var trimmed = line.TrimStart();
        var space = trimmed.IndexOf(' ', StringComparison.Ordinal);
        return space < 0 ? "" : trimmed[(space + 1)..].Trim();
    }

    private static bool IsThematicBreak(string line)
    {
        var compact = line.Trim().Replace(" ", "", StringComparison.Ordinal);
        return compact.Length >= 3 &&
            (compact.All(c => c == '-') || compact.All(c => c == '*') || compact.All(c => c == '_'));
    }

    private static bool IsSetextUnderline(string line)
    {
        var trimmed = line.Trim();
        return trimmed.Length >= 2 && (trimmed.All(c => c == '=') || trimmed.All(c => c == '-'));
    }

    private static bool HasHardBreak(string line) =>
        line.EndsWith("  ", StringComparison.Ordinal) || line.EndsWith('\\');

    private static int IndentOf(string line)
    {
        var n = 0;
        foreach (var c in line)
        {
            if (c == ' ') { n++; }
            else if (c == '\t') { n += 4; }
            else { break; }
        }

        return n;
    }

    private static int SkipWhile(string[] lines, int from, Func<string, bool> predicate)
    {
        var i = from;
        while (i < lines.Length && predicate(lines[i]))
        {
            i++;
        }

        return i;
    }

    private static bool IsEscapable(char c) => c is '\\' or '`' or '*' or '_' or '[' or ']' or '(' or ')' or '#' or '!' or '<' or '>' or '&' or '"' or '\'';

    private static void Append(StringBuilder html, char c)
    {
        switch (c)
        {
            case '&': html.Append("&amp;"); break;
            case '<': html.Append("&lt;"); break;
            case '>': html.Append("&gt;"); break;
            case '"': html.Append("&quot;"); break;
            case '\'': html.Append("&#39;"); break;
            default: html.Append(c); break;
        }
    }

    private static string EscapeAll(string text)
    {
        var html = new StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '\\' && i + 1 < text.Length && IsEscapable(text[i + 1]))
            {
                Append(html, text[++i]);
                continue;
            }

            Append(html, text[i]);
        }

        return html.ToString();
    }

    /// <summary>Drops backslash escapes from text that lands on a node as PLAIN text (a heading, an
    /// alt) — those fields are not html, so they must not carry html escaping either.</summary>
    private static string Unescape(string text)
    {
        var output = new StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '\\' && i + 1 < text.Length && IsEscapable(text[i + 1]))
            {
                output.Append(text[++i]);
                continue;
            }

            output.Append(text[i]);
        }

        return output.ToString();
    }
}
