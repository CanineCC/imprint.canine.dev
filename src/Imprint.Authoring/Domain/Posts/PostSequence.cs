namespace Imprint.Authoring.Domain.Posts;

/// <summary>
/// The editorial sequence number a post may carry at the front of its title — "(1) Findings
/// count is not analysis depth". It exists because a content plan has an order that has nothing
/// to do with dates: post 2 answers post 1, and a queue sorted by "most recently touched" hides
/// that completely.
///
/// <para>Parsed from the title rather than stored as a field, deliberately: the number is the
/// author's own notation, they can change it by typing, and a post that leaves the plan simply
/// stops having one. A field would need a command, a migration and a rule for what happens when
/// the two disagree.</para>
/// </summary>
public static class PostSequence
{
    /// <summary>
    /// The number in a leading "(n)", or null when the title does not open with one. Only a
    /// parenthesised number at the very front counts — "(1) Foo" is sequenced, "Foo (1)" is a
    /// title that happens to end in a number, and guessing at the difference would reorder
    /// somebody's blog on the strength of a bracket.
    /// </summary>
    public static int? NumberIn(string? title)
    {
        var text = title?.TrimStart();
        if (text is not { Length: > 2 } || text[0] != '(')
        {
            return null;
        }

        var close = text.IndexOf(')');
        if (close < 2)
        {
            return null;
        }

        var digits = text[1..close];
        return digits.Length > 0 && digits.All(char.IsAsciiDigit) && int.TryParse(digits, out var number)
            ? number
            : null;
    }

    /// <summary>
    /// The sort key for an editorial queue: numbered posts first in numeric order, everything
    /// else after. Numeric and not lexicographic, because "(10)" sorts before "(2)" as text and
    /// an author who numbered twenty posts would find them in an order nobody chose.
    /// </summary>
    public static (int Bucket, int Number) SortKey(string? title) =>
        NumberIn(title) is { } number ? (0, number) : (1, 0);
}
