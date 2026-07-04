using System.Text;
using Imprint.Authoring.Domain.Pages;

namespace ContentSeeder;

/// <summary>
/// A deterministic, faithful converter from the docs' markdown body to a sequence of
/// Imprint nodes. The docs use only: ATX headings (<c>## …</c>), unordered lists
/// (<c>- …</c>), ordered lists (<c>1. …</c>), paragraphs, and the CMS inline subset
/// (<c>**bold**</c>, <c>`code`</c>, <c>[l](h)</c>) — plus one GFM pipe table in
/// security.md. Nothing is invented; unsupported constructs are flagged, not guessed.
///
/// Headings become <see cref="HeadingNode"/>s (canonical HTML has no in-body headings);
/// paragraphs and lists become canonical <see cref="RichTextNode"/>s (<c>&lt;p&gt;</c> /
/// <c>&lt;ul&gt;</c> / <c>&lt;ol&gt;</c>). A pipe table is rendered faithfully as a
/// <c>&lt;ul&gt;</c> of rows (every cell verbatim) because the canonical subset has no
/// <c>&lt;table&gt;</c> — recorded as a flagged transform.
/// </summary>
public static class Markdown
{
    public sealed record Flag(string Rel, string Note);

    public static IReadOnlyList<Node> ToNodes(string markdown, string origin, string rel, List<Flag> flags)
    {
        var nodes = new List<Node>();
        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        var i = 0;

        while (i < lines.Length)
        {
            var line = lines[i];
            var trimmed = line.TrimEnd();

            // blank line
            if (trimmed.Trim().Length == 0)
            {
                i++;
                continue;
            }

            // ATX heading: #..###### text
            if (trimmed.StartsWith('#'))
            {
                var level = 0;
                while (level < trimmed.Length && trimmed[level] == '#')
                {
                    level++;
                }

                var headingText = trimmed[level..].Trim();
                // Doc bodies start at h2; clamp to canonical 1–4 with a floor of 2.
                var canonicalLevel = Math.Clamp(level, 2, 4);
                nodes.Add(Nodes.Heading(canonicalLevel, InlineToPlain(headingText)));
                i++;
                continue;
            }

            // pipe table (GFM): a header row of pipes followed by a --- separator row
            if (IsTableRow(trimmed) && i + 1 < lines.Length && IsTableSeparator(lines[i + 1]))
            {
                var (rich, consumed) = TableToList(lines, i, origin, rel, flags);
                nodes.Add(rich);
                i += consumed;
                continue;
            }

            // unordered list block
            if (IsUnordered(trimmed))
            {
                var (rich, consumed) = ListBlock(lines, i, origin, ordered: false);
                nodes.Add(rich);
                i += consumed;
                continue;
            }

            // ordered list block
            if (IsOrdered(trimmed))
            {
                var (rich, consumed) = ListBlock(lines, i, origin, ordered: true);
                nodes.Add(rich);
                i += consumed;
                continue;
            }

            // paragraph: consecutive non-blank, non-structural lines
            var para = new StringBuilder();
            while (i < lines.Length)
            {
                var l = lines[i].TrimEnd();
                if (l.Trim().Length == 0 || l.StartsWith('#') || IsUnordered(l) || IsOrdered(l) ||
                    (IsTableRow(l) && i + 1 < lines.Length && IsTableSeparator(lines[i + 1])))
                {
                    break;
                }

                if (para.Length > 0)
                {
                    para.Append(' ');
                }

                para.Append(l.Trim());
                i++;
            }

            var inline = Inline.ToCanonicalInline(para.ToString(), origin);
            if (inline.Length > 0)
            {
                nodes.Add(Nodes.RichHtml($"<p>{inline}</p>"));
            }
        }

        return nodes;
    }

    private static (RichTextNode Node, int Consumed) ListBlock(string[] lines, int start, string origin, bool ordered)
    {
        var items = new List<string>();
        var i = start;
        while (i < lines.Length)
        {
            var l = lines[i].TrimEnd();
            var isItem = ordered ? IsOrdered(l) : IsUnordered(l);
            if (!isItem)
            {
                break;
            }

            items.Add(ItemText(l, ordered));
            i++;
        }

        var sb = new StringBuilder();
        sb.Append(ordered ? "<ol>" : "<ul>");
        foreach (var item in items)
        {
            sb.Append("<li>").Append(Inline.ToCanonicalInline(item, origin)).Append("</li>");
        }

        sb.Append(ordered ? "</ol>" : "</ul>");
        return (Nodes.RichHtml(sb.ToString()), i - start);
    }

    /// <summary>Render a GFM pipe table as a canonical &lt;ul&gt;: header row then one &lt;li&gt; per data row, cells verbatim.</summary>
    private static (RichTextNode Node, int Consumed) TableToList(
        string[] lines, int start, string origin, string rel, List<Flag> flags)
    {
        var header = SplitCells(lines[start]);
        var i = start + 2; // skip header + separator
        var rows = new List<IReadOnlyList<string>>();
        while (i < lines.Length && IsTableRow(lines[i].TrimEnd()) && lines[i].Trim().Length > 0)
        {
            rows.Add(SplitCells(lines[i]));
            i++;
        }

        var sb = new StringBuilder();
        sb.Append("<ul>");
        // First li carries the column headers (verbatim), joined with a middot.
        sb.Append("<li><strong>")
          .Append(Inline.Escape(string.Join(" · ", header)))
          .Append("</strong></li>");
        foreach (var row in rows)
        {
            sb.Append("<li>");
            for (var c = 0; c < row.Count; c++)
            {
                if (c > 0)
                {
                    sb.Append(" — ");
                }

                sb.Append(Inline.ToCanonicalInline(row[c], origin));
            }

            sb.Append("</li>");
        }

        sb.Append("</ul>");
        flags.Add(new Flag(rel,
            "GFM pipe table has no equivalent in Imprint's canonical HTML subset (no <table>); " +
            "rendered faithfully as a bulleted list of rows (every cell verbatim, header row bolded)."));
        return (Nodes.RichHtml(sb.ToString()), i - start);
    }

    private static bool IsUnordered(string line)
    {
        var t = line.TrimStart();
        return t.StartsWith("- ", StringComparison.Ordinal) || t.StartsWith("* ", StringComparison.Ordinal);
    }

    private static bool IsOrdered(string line)
    {
        var t = line.TrimStart();
        var dot = t.IndexOf(". ", StringComparison.Ordinal);
        return dot > 0 && t[..dot].All(char.IsDigit);
    }

    private static string ItemText(string line, bool ordered)
    {
        var t = line.TrimStart();
        if (ordered)
        {
            var dot = t.IndexOf(". ", StringComparison.Ordinal);
            return t[(dot + 2)..].Trim();
        }

        return t[2..].Trim();
    }

    private static bool IsTableRow(string line)
    {
        var t = line.Trim();
        return t.StartsWith('|') && t.Count(ch => ch == '|') >= 2;
    }

    private static bool IsTableSeparator(string line)
    {
        var t = line.Trim();
        if (!t.StartsWith('|'))
        {
            return false;
        }

        return t.Replace("|", "").Replace("-", "").Replace(":", "").Replace(" ", "").Length == 0
               && t.Contains('-', StringComparison.Ordinal);
    }

    private static IReadOnlyList<string> SplitCells(string line)
    {
        var t = line.Trim();
        if (t.StartsWith('|'))
        {
            t = t[1..];
        }

        if (t.EndsWith('|'))
        {
            t = t[..^1];
        }

        return [.. t.Split('|').Select(c => c.Trim())];
    }

    /// <summary>Heading text is plain (no in-heading markup in the canonical model): strip the inline markers, keep the words.</summary>
    private static string InlineToPlain(string text)
    {
        // ** and ` are formatting markers; [l](h) → keep the label. Faithful to the words.
        var sb = new StringBuilder(text.Length);
        var i = 0;
        while (i < text.Length)
        {
            if (text[i] == '*' && i + 1 < text.Length && text[i + 1] == '*')
            {
                i += 2;
                continue;
            }

            if (text[i] == '`')
            {
                i++;
                continue;
            }

            if (text[i] == '[')
            {
                var close = text.IndexOf(']', i);
                var paren = close >= 0 && close + 1 < text.Length && text[close + 1] == '(' ? close + 1 : -1;
                if (close > i && paren > 0)
                {
                    sb.Append(text[(i + 1)..close]);
                    var end = text.IndexOf(')', paren);
                    i = end >= 0 ? end + 1 : text.Length;
                    continue;
                }
            }

            sb.Append(text[i]);
            i++;
        }

        return sb.ToString().Trim();
    }
}
