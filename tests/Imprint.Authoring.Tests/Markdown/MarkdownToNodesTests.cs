using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Markdown;

namespace Imprint.Authoring.Tests.Markdown;

/// <summary>
/// The converter's contract in both directions: what it represents, and what it REFUSES. The
/// refusals matter as much as the conversions — the whole design decision is that an author is
/// told their table has nowhere to go, rather than watching it disappear from the published page.
/// </summary>
public sealed class MarkdownToNodesTests
{
    private static readonly Locale En = new("en");

    private static MarkdownConversion Convert(string markdown) => MarkdownToNodes.Convert(markdown, En);

    private static string Html(Node node) => Assert.IsType<RichTextNode>(node).Html.Resolve(En, En);

    // ------------------------------------------------------------------ what it represents

    [Fact]
    public void A_paragraph_becomes_canonical_rich_text()
    {
        var result = Convert("Hello there.");

        Assert.True(result.Ok);
        Assert.Equal("<p>Hello there.</p>", Html(Assert.Single(result.Nodes)));
    }

    [Fact]
    public void Headings_keep_their_level_and_are_plain_text()
    {
        var result = Convert("# One\n\n### Three");

        Assert.True(result.Ok);
        Assert.Collection(
            result.Nodes,
            n => Assert.Equal((1, "One"), (((HeadingNode)n).Level, ((HeadingNode)n).Text.Resolve(En, En))),
            n => Assert.Equal((3, "Three"), (((HeadingNode)n).Level, ((HeadingNode)n).Text.Resolve(En, En))));
    }

    [Fact]
    public void Emphasis_and_links_become_the_canonical_inline_elements()
    {
        var result = Convert("A **bold** and *italic* [link](https://example.com/).");

        Assert.True(result.Ok);
        Assert.Equal(
            "<p>A <strong>bold</strong> and <em>italic</em> <a href=\"https://example.com/\">link</a>.</p>",
            Html(Assert.Single(result.Nodes)));
    }

    [Fact]
    public void Lists_become_one_rich_text_node_each()
    {
        var result = Convert("- one\n- two\n\n1. first\n2. second");

        Assert.True(result.Ok);
        Assert.Equal("<ul><li>one</li><li>two</li></ul>", Html(result.Nodes[0]));
        Assert.Equal("<ol><li>first</li><li>second</li></ol>", Html(result.Nodes[1]));
    }

    [Fact]
    public void A_fenced_block_becomes_a_code_node_with_its_language()
    {
        var result = Convert("```csharp\nvar x = 1;\nif (x < 2) { }\n```");

        Assert.True(result.Ok);
        var code = Assert.IsType<CodeNode>(Assert.Single(result.Nodes));
        Assert.Equal("csharp", code.Language);
        // Verbatim: indentation and the raw '<' are the author's, not markup to be escaped here.
        Assert.Equal("var x = 1;\nif (x < 2) { }", code.Text);
    }

    [Fact]
    public void A_thematic_break_becomes_a_divider()
    {
        var result = Convert("Above\n\n---\n\nBelow");

        Assert.True(result.Ok);
        Assert.IsType<DividerNode>(result.Nodes[1]);
    }

    [Fact]
    public void An_image_paragraph_becomes_an_image_node_bound_to_the_media_library()
    {
        var asset = Guid.NewGuid();

        var result = Convert($"![A diagram](media:{asset:D})");

        Assert.True(result.Ok);
        var image = Assert.IsType<ImageNode>(Assert.Single(result.Nodes));
        Assert.Equal(AssetId.From(asset), image.AssetId);
        Assert.Equal("A diagram", image.Alt.Resolve(En, En));
    }

    [Fact]
    public void An_internal_link_keeps_its_page_reference_so_it_resolves_per_locale()
    {
        var page = Guid.NewGuid();

        var result = Convert($"See [the manual](page:{page:D}#setup).");

        Assert.True(result.Ok);
        Assert.Contains($"href=\"page:{page:D}#setup\"", Html(Assert.Single(result.Nodes)), StringComparison.Ordinal);
    }

    [Fact]
    public void Two_trailing_spaces_are_a_hard_break_inside_one_paragraph()
    {
        var result = Convert("first line  \nsecond line");

        Assert.True(result.Ok);
        Assert.Equal("<p>first line<br>second line</p>", Html(Assert.Single(result.Nodes)));
    }

    [Fact]
    public void A_wrapped_paragraph_joins_into_one_node()
    {
        var result = Convert("one\ntwo\nthree");

        Assert.True(result.Ok);
        Assert.Equal("<p>one two three</p>", Html(Assert.Single(result.Nodes)));
    }

    // ------------------------------------------------------------------------- escaping

    [Fact]
    public void Html_characters_in_prose_are_entity_encoded()
    {
        var result = Convert("5 < 6 & \"quoted\" it's");

        Assert.True(result.Ok);
        Assert.Equal("<p>5 &lt; 6 &amp; &quot;quoted&quot; it&#39;s</p>", Html(Assert.Single(result.Nodes)));
    }

    [Fact]
    public void A_backslash_escapes_a_marker_instead_of_starting_emphasis()
    {
        var result = Convert(@"a \* b \_ c");

        Assert.True(result.Ok);
        Assert.Equal("<p>a * b _ c</p>", Html(Assert.Single(result.Nodes)));
    }

    [Fact]
    public void Everything_it_produces_passes_the_canonical_validator()
    {
        // The converter is the only writer that assembles this html by hand; if its output ever
        // failed the grammar the aggregate would reject a save the editor said was fine.
        var result = Convert(
            "# Title\n\nProse with **bold**, *em*, a [link](https://x.test/) and 5 < 6.\n\n- a\n- b\n\n1. c\n\n```sh\nls -la\n```\n\n---\n");

        Assert.True(result.Ok);
        foreach (var rich in result.Nodes.OfType<RichTextNode>())
        {
            Assert.True(CanonicalHtml.TryValidate(rich.Html.Resolve(En, En), out var error), error);
        }
    }

    // --------------------------------------------------------------------- what it refuses

    [Theory]
    [InlineData("> quoted", "Blockquotes")]
    [InlineData("| a | b |", "Tables")]
    [InlineData("Some `inline code` here", "Inline code")]
    [InlineData("A <span>tag</span>", "Raw HTML")]
    [InlineData("    indented code", "Indented code")]
    [InlineData("- one\n  - nested", "Nested lists")]
    [InlineData("Title\n=====", "Underlined headings")]
    [InlineData("text with an ![image](media:x) inline", "its own paragraph")]
    public void Constructs_the_vocabulary_cannot_hold_are_reported_not_dropped(string markdown, string expected)
    {
        var result = Convert(markdown);

        Assert.False(result.Ok);
        Assert.Contains(result.Problems, p => p.Message.Contains(expected, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void A_problem_carries_the_line_it_is_on()
    {
        var result = Convert("fine\n\nalso fine\n\n> quoted");

        Assert.Equal(5, Assert.Single(result.Problems).Line);
    }

    [Fact]
    public void An_image_that_is_not_a_media_reference_is_refused()
    {
        var result = Convert("![alt](/img/photo.png)");

        Assert.False(result.Ok);
        Assert.Contains("srcset", Assert.Single(result.Problems).Message, StringComparison.Ordinal);
        Assert.Empty(result.Nodes);
    }

    [Theory]
    [InlineData("[x](javascript:alert(1))")]
    [InlineData("[x](/relative/path)")]
    [InlineData("[x](ftp://files.test/a)")]
    public void A_link_the_site_cannot_resolve_is_refused(string markdown)
    {
        var result = Convert(markdown);

        Assert.False(result.Ok);
        Assert.Contains("not a link this site can resolve", Assert.Single(result.Problems).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_unmatched_emphasis_marker_is_reported_rather_than_producing_invalid_html()
    {
        var result = Convert("a *dangling emphasis");

        Assert.False(result.Ok);
        Assert.Contains("Unmatched emphasis", Assert.Single(result.Problems).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_unclosed_fence_keeps_the_code_and_reports_it()
    {
        var result = Convert("```\nvar x = 1;");

        Assert.False(result.Ok);
        Assert.Contains("never closed", Assert.Single(result.Problems).Message, StringComparison.Ordinal);
        // The author's code is not thrown away over three missing backticks.
        Assert.Equal("var x = 1;", Assert.IsType<CodeNode>(Assert.Single(result.Nodes)).Text);
    }

    [Fact]
    public void An_unusable_language_tag_is_reported_and_the_block_still_converts()
    {
        var result = Convert("```\" onload=\"x\ncode\n```");

        Assert.False(result.Ok);
        Assert.Null(Assert.IsType<CodeNode>(Assert.Single(result.Nodes)).Language);
    }

    [Fact]
    public void Good_content_around_a_refusal_still_converts()
    {
        // An editor showing "everything or nothing" would make one stray backtick look like a
        // catastrophe; the preview should render what it can and point at the rest.
        var result = Convert("# Heading\n\n> quoted\n\nA good paragraph.");

        Assert.False(result.Ok);
        Assert.IsType<HeadingNode>(result.Nodes[0]);
        Assert.Equal("<p>A good paragraph.</p>", Html(result.Nodes[1]));
    }

    [Fact]
    public void Empty_markdown_converts_to_nothing_without_complaint()
    {
        var result = Convert("   \n\n  ");

        Assert.True(result.Ok);
        Assert.Empty(result.Nodes);
    }

    [Fact]
    public void Node_ids_come_from_the_injected_source()
    {
        var ids = new Queue<NodeId>([NodeId.New(), NodeId.New()]);
        var expected = ids.ToArray();

        var result = MarkdownToNodes.Convert("# A\n\nB", En, ids.Dequeue);

        Assert.Equal(expected[0], result.Nodes[0].Id);
        Assert.Equal(expected[1], result.Nodes[1].Id);
    }
}
