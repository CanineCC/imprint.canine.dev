using Imprint.Authoring.Domain.Posts;

namespace Imprint.Authoring.Tests.Domain.Posts;

/// <summary>
/// The editorial sequence number read off the front of a title. The rule is narrow on purpose:
/// a bracket in the wrong place must not silently reorder somebody's queue.
/// </summary>
public sealed class PostSequenceTests
{
    [Theory]
    [InlineData("(1) Findings count is not analysis depth", 1)]
    [InlineData("(13) Survey clarity is not language quality", 13)]
    [InlineData("  (7) Leading whitespace is still a number", 7)]
    [InlineData("(0) Zero is a number somebody may well use", 0)]
    public void NumberIn_reads_a_leading_parenthesised_number(string title, int expected) =>
        Assert.Equal(expected, PostSequence.NumberIn(title));

    [Theory]
    [InlineData("Findings count is not analysis depth")]
    [InlineData("A post that ends in a number (3)")]
    [InlineData("(one) spelled out is not a number")]
    [InlineData("(12 unclosed")]
    [InlineData("() empty")]
    [InlineData("(1x) not all digits")]
    [InlineData("")]
    [InlineData(null)]
    public void NumberIn_ignores_everything_else(string? title) =>
        Assert.Null(PostSequence.NumberIn(title));

    [Fact]
    public void SortKey_orders_numerically_not_lexicographically()
    {
        // The whole reason this is not a string sort: "(10)" sorts before "(2)" as text, and an
        // author who numbered twenty posts would find them in an order nobody chose.
        var titles = new[] { "(10) ten", "(2) two", "(1) one", "(20) twenty" };

        Assert.Equal(
            ["(1) one", "(2) two", "(10) ten", "(20) twenty"],
            titles.OrderBy(PostSequence.SortKey).ToArray());
    }

    [Fact]
    public void SortKey_puts_unnumbered_posts_after_numbered_ones()
    {
        var titles = new[] { "an unnumbered draft", "(2) two", "another unnumbered one", "(1) one" };

        var ordered = titles.OrderBy(PostSequence.SortKey).ToArray();

        Assert.Equal(["(1) one", "(2) two"], ordered[..2]);
        // …and the unnumbered ones keep the order they arrived in, rather than being sorted by
        // title: their order is whatever the caller's own ordering said it was.
        Assert.Equal(["an unnumbered draft", "another unnumbered one"], ordered[2..]);
    }
}
