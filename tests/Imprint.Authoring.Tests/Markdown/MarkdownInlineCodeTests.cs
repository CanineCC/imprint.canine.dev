using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Markdown;

namespace Imprint.Authoring.Tests.Markdown;

/// <summary>
/// Inline code: `x` becomes &lt;code&gt;x&lt;/code&gt;, by CommonMark's rule (a run of n backticks closes
/// at the next run of exactly n; one space each side is stripped when both are there). The content is
/// TEXT: nothing inside is markdown, and it is escaped. An unmatched backtick is just a backtick.
/// The canonical grammar admits &lt;code&gt; with text and entities only, never a tag inside.
/// </summary>
public sealed class MarkdownInlineCodeTests
{
    private static readonly Locale En = new("en");

    private static string Html(string markdown)
    {
        var result = MarkdownToNodes.Convert(markdown, En);
        Assert.True(result.Ok, string.Join("; ", result.Problems.Select(p => $"line {p.Line}: {p.Message}")));
        return ((RichTextNode)Assert.Single(result.Nodes)).Html.Get(En) ?? "";
    }

    [Fact]
    public void A_backtick_span_becomes_code() =>
        Assert.Equal("<p>Call <code>next_task</code> first.</p>", Html("Call `next_task` first."));

    [Fact]
    public void Markdown_inside_code_is_text_and_html_is_escaped() =>
        Assert.Equal("<p><code>**a** &lt;b&gt; &amp; [c](d)</code></p>", Html("`**a** <b> & [c](d)`"));

    [Fact]
    public void A_double_backtick_span_can_hold_a_backtick() =>
        Assert.Equal("<p><code>a`b</code></p>", Html("`` a`b ``"));

    [Fact]
    public void An_unmatched_backtick_is_a_literal_backtick() =>
        Assert.Equal("<p>It costs 5` per line.</p>", Html("It costs 5` per line."));

    [Fact]
    public void Code_works_inside_strong_list_items_and_table_cells()
    {
        Assert.Equal("<p><strong>Use <code>x</code></strong></p>", Html("**Use `x`**"));
        Assert.Contains("<li><code>src/CryptKeyInterface.php</code></li>", Html("- `src/CryptKeyInterface.php`"), StringComparison.Ordinal);

        var table = MarkdownToNodes.Convert("| a |\n|---|\n| `b` |", En);
        Assert.True(table.Ok);
        Assert.Equal("<code>b</code>", ((TableNode)Assert.Single(table.Nodes)).Rows[0][0].Get(En));
    }

    // Found in the resource center's copies (2026-10-28): a link whose words are a file path or a commit
    // showed its backticks, because a link's words were escaped as plain text.
    [Fact]
    public void Code_works_inside_a_links_words() =>
        Assert.Equal("<p>See <a href=\"https://x.test/a\"><code>src/a.php</code></a> and <a href=\"https://x.test/b\">b <code>c</code></a>.</p>",
            Html("See [`src/a.php`](https://x.test/a) and [b `c`](https://x.test/b)."));

    [Theory]
    [InlineData("<p><a href=\"https://x.test\"><code>x</code></a></p>", true)]
    [InlineData("<p><code>x</code></p>", true)]
    [InlineData("<p><code>a &amp; b</code></p>", true)]
    [InlineData("<p><code><strong>x</strong></code></p>", false)]
    [InlineData("<p><code><a href=\"https://x.test\">x</a></code></p>", false)]
    [InlineData("<p><code>x</p>", false)]
    public void The_canonical_grammar_admits_code_with_text_only(string html, bool valid) =>
        Assert.Equal(valid, CanonicalHtml.TryValidate(html, out _));
}
