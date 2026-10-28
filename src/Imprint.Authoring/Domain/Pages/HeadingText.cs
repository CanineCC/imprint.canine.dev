namespace Imprint.Authoring.Domain.Pages;

/// <summary>
/// A heading's words, with at most one part marked <c>&lt;em&gt;…&lt;/em&gt;</c>: the accent run a product site
/// draws in its own colour ("Build as fast as you like, &lt;em&gt;with Assure looking after the quality.&lt;/em&gt;").
/// The mark is the only markup a heading carries and it is never emitted as markup: the renderer turns it into a
/// span, and everything that reads a heading as words (anchors, the contents list, llms.txt, word counts, the
/// editor's layers) reads <see cref="Plain"/>, so a marked heading is one heading and one sentence to a screen
/// reader, a search engine and an id alike.
/// ★ A mark inside the one text field, not a second field: the sessions and translators that rewrite a heading
/// replace the whole sentence at once, so a rewrite can never leave an old second half behind.
/// </summary>
public static class HeadingText
{
    public const string Open = "<em>";
    public const string Close = "</em>";

    /// <summary>The heading as words, with the mark removed.</summary>
    public static string Plain(string? text) =>
        string.IsNullOrEmpty(text) ? string.Empty : text.Replace(Open, string.Empty, StringComparison.Ordinal).Replace(Close, string.Empty, StringComparison.Ordinal);

    /// <summary>The heading in runs, in order; <c>Accent</c> is true for the marked run. An unmarked heading is one run.</summary>
    public static IReadOnlyList<(string Text, bool Accent)> Runs(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return [];
        }

        var open = text.IndexOf(Open, StringComparison.Ordinal);
        if (open < 0 || !TryValidate(text, out _))
        {
            return [(Plain(text), false)];
        }

        var close = text.IndexOf(Close, open, StringComparison.Ordinal);
        var runs = new List<(string, bool)>(3);
        if (open > 0)
        {
            runs.Add((text[..open], false));
        }

        runs.Add((text[(open + Open.Length)..close], true));
        if (close + Close.Length < text.Length)
        {
            runs.Add((text[(close + Close.Length)..], false));
        }

        return runs;
    }

    /// <summary>One mark at most, opened before it is closed, around some words.</summary>
    public static bool TryValidate(string? text, out string? error)
    {
        error = null;
        if (string.IsNullOrEmpty(text))
        {
            return true;
        }

        var opens = Count(text, Open);
        var closes = Count(text, Close);
        if (opens == 0 && closes == 0)
        {
            return true;
        }

        var open = text.IndexOf(Open, StringComparison.Ordinal);
        var close = text.IndexOf(Close, StringComparison.Ordinal);
        if (opens != 1 || closes != 1 || close < open)
        {
            error = "A heading can mark one part, written <em>…</em>, for the accent colour.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(text[(open + Open.Length)..close]))
        {
            error = "The marked part of a heading (<em>…</em>) needs some words in it.";
            return false;
        }

        return true;
    }

    private static int Count(string text, string token)
    {
        var count = 0;
        for (var i = text.IndexOf(token, StringComparison.Ordinal); i >= 0; i = text.IndexOf(token, i + token.Length, StringComparison.Ordinal))
        {
            count++;
        }

        return count;
    }
}
