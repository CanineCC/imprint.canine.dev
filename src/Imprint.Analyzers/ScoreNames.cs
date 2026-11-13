using System;
using System.Collections.Generic;

namespace Imprint.Analyzers;

/// <summary>
/// ADR-0028: when a NAME says it holds a score. A name is split into its words (<c>HeadlineScoreZeroToTen</c> is
/// headline, score, zero, to, ten) and names a score when one word is <c>score</c>, <c>cai</c> or <c>headline</c> (or their
/// plurals) and its LAST word is not a different quantity: <c>scoreCount</c> counts scores, <c>caiVersion</c> is a
/// version, <c>headlineText</c> is already text. Whole words only, so <c>ScoredFiles</c> and <c>encaissement</c> are not scores.
/// </summary>
/// <remarks>Imprint's copy of <c>Kennel.Analyzers.ScoreNames</c> (ADR-0028), used by IMP0001/IMP0002. Change the copies together.</remarks>
internal static class ScoreNames
{
    private static readonly HashSet<string> ScoreWords = new(StringComparer.Ordinal)
    {
        "score", "scores", "cai", "cais", "headline", "headlines",
    };

    /// <summary>A last word that makes the name some other quantity, or text that is already written.</summary>
    private static readonly HashSet<string> OtherLastWords = new(StringComparer.Ordinal)
    {
        // a different quantity: a count of things, a share, a position, a size
        "count", "counts", "total", "totals", "index", "indices", "id", "ids", "rank", "ranks", "version", "versions",
        "weight", "weights", "length", "size", "number", "numbers", "percentile", "percentiles", "seq", "ordinal",
        "position", "x", "y", "width", "height", "offset", "left", "top", "px", "radius", "angle", "seed", "hash",
        "pct", "percent", "percentage", "share", "shares", "rate", "rates", "ratio", "findings", "finding", "runs",
        "repos", "repositories", "files", "rows", "items", "surveys", "scans", "deliveries",
        // a line on the scale rather than a reading of a codebase (the standard writes them "Strong (70 ≤ score < 90)")
        "threshold", "thresholds", "gate", "cap", "caps", "bar", "bars", "floor", "ceiling", "target", "targets", "goal",
        // already text, or a band's key rather than its number
        "text", "texts", "band", "bands", "key", "keys", "class", "classes", "label", "labels", "url", "hex", "anchor", "kind",
        "name", "names", "html", "title", "glyph", "colour", "color", "css", "word", "words", "note", "notes", "line",
        "lines", "shown", "tone", "ink", "move", "moves", "direction",
    };

    /// <summary>Whether the name says it holds a score.</summary>
    /// <param name="name">A symbol's or a script identifier's name.</param>
    public static bool IsScoreName(string? name)
    {
        var words = Words(name);
        if (words.Count == 0 || OtherLastWords.Contains(words[words.Count - 1]))
        {
            return false;
        }

        foreach (var word in words)
        {
            if (ScoreWords.Contains(word))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The lower-case words of a name: split at '_', '$', digits and case changes (<c>CAIScore</c> is cai, score).</summary>
    /// <param name="name">The name.</param>
    public static List<string> Words(string? name)
    {
        var words = new List<string>();
        if (string.IsNullOrEmpty(name))
        {
            return words;
        }

        var current = new System.Text.StringBuilder();
        void Flush()
        {
            if (current.Length > 0)
            {
                words.Add(current.ToString().ToLowerInvariant());
                current.Clear();
            }
        }

        for (var i = 0; i < name!.Length; i++)
        {
            var c = name[i];
            if (!char.IsLetter(c))
            {
                Flush();
                continue;
            }

            if (char.IsUpper(c) && current.Length > 0)
            {
                var previous = name[i - 1];
                var next = i + 1 < name.Length ? name[i + 1] : '\0';
                // "headlineScore": a capital after a lower-case letter starts a word; "CAIScore": the last capital of a
                // run that is followed by a lower-case letter starts the next word.
                if (char.IsLower(previous) || (char.IsUpper(previous) && char.IsLower(next)))
                {
                    Flush();
                }
            }

            current.Append(c);
        }

        Flush();
        return words;
    }
}
