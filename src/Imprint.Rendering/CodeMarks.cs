namespace Imprint.Rendering;

/// <summary>One run of a code sample: its text, and the class that marks it (null for plain text).</summary>
public readonly record struct CodeRun(string Text, string? Class);

/// <summary>
/// Marks the keys and the punctuation of a JSON or YAML sample, so a product site can draw keys in its accent and
/// punctuation muted, as the CCA brand document asks (section 5.3: titanium keys, muted punctuation, values in text
/// colour).
/// </summary>
/// <remarks>
/// ★ MARKS, NOT A HIGHLIGHTER. Two classes only, <c>ip-code-key</c> and <c>ip-code-punct</c>, and no colour here: a
/// site whose sheet does not style them looks exactly as before. The runs concatenate to the author's text unchanged,
/// and the view writes each one as escaped content, never markup. Any other language, or none, is one plain run.
/// </remarks>
public static class CodeMarks
{
    public const string Key = "ip-code-key";
    public const string Punct = "ip-code-punct";

    public static IReadOnlyList<CodeRun> Of(string text, string? language) => language?.ToLowerInvariant() switch
    {
        "json" => Json(text),
        "yaml" or "yml" => Yaml(text),
        _ => [new CodeRun(text, null)],
    };

    private static List<CodeRun> Json(string text)
    {
        var runs = new List<CodeRun>();
        var plain = 0;
        var i = 0;
        while (i < text.Length)
        {
            var c = text[i];
            if (c == '"')
            {
                var end = StringEnd(text, i);
                var after = end;
                while (after < text.Length && char.IsWhiteSpace(text[after]))
                {
                    after++;
                }

                if (after < text.Length && text[after] == ':')
                {
                    Flush(runs, text, plain, i);
                    runs.Add(new CodeRun(text[i..end], Key));
                    plain = end;
                }

                i = end;
            }
            else if (c is '{' or '}' or '[' or ']' or ',' or ':')
            {
                Flush(runs, text, plain, i);
                runs.Add(new CodeRun(c.ToString(), Punct));
                plain = ++i;
            }
            else
            {
                i++;
            }
        }

        Flush(runs, text, plain, text.Length);
        return runs;
    }

    // The index just past a JSON string's closing quote, or the end of the text when it never closes.
    private static int StringEnd(string text, int open)
    {
        for (var i = open + 1; i < text.Length; i++)
        {
            if (text[i] == '\\')
            {
                i++;
            }
            else if (text[i] == '"')
            {
                return i + 1;
            }
        }

        return text.Length;
    }

    // A YAML key is a line's first word (after its indent and any "- ") when a colon follows it directly.
    private static List<CodeRun> Yaml(string text)
    {
        var runs = new List<CodeRun>();
        var plain = 0;
        var line = 0;
        while (line < text.Length)
        {
            var next = text.IndexOf('\n', line);
            var end = next < 0 ? text.Length : next;
            var i = line;
            while (i < end && text[i] == ' ')
            {
                i++;
            }

            if (i + 1 < end && text[i] == '-' && text[i + 1] == ' ')
            {
                i += 2;
            }

            var key = i;
            while (key < end && (char.IsLetterOrDigit(text[key]) || text[key] is '_' or '-' or '.'))
            {
                key++;
            }

            if (key > i && key < end && text[key] == ':' && (key + 1 == end || text[key + 1] == ' '))
            {
                Flush(runs, text, plain, i);
                runs.Add(new CodeRun(text[i..key], Key));
                runs.Add(new CodeRun(":", Punct));
                plain = key + 1;
            }

            line = end + 1;
        }

        Flush(runs, text, plain, text.Length);
        return runs;
    }

    private static void Flush(List<CodeRun> runs, string text, int from, int to)
    {
        if (to > from)
        {
            runs.Add(new CodeRun(text[from..to], null));
        }
    }
}
