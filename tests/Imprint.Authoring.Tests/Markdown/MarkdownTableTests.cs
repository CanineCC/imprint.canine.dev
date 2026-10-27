using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Markdown;

namespace Imprint.Authoring.Tests.Markdown;

/// <summary>
/// GitHub pipe tables. The same two-sided contract as the rest of the converter: a well-formed
/// table becomes a <see cref="TableNode"/> whose cells keep their inline formatting, and every
/// shape GitHub would quietly repair — a ragged row, a missing delimiter row, a header and
/// delimiter that disagree on the column count — is REFUSED at its line instead of guessed at.
/// </summary>
public sealed class MarkdownTableTests
{
    private static readonly Locale En = new("en");

    private static MarkdownConversion Convert(string markdown) => MarkdownToNodes.Convert(markdown, En);

    private static string[] Cells(IEnumerable<LocalizedText> cells) => [.. cells.Select(c => c.Resolve(En, En))];

    private static TableNode SingleTable(MarkdownConversion result)
    {
        Assert.True(result.Ok, string.Join("; ", result.Problems.Select(p => $"line {p.Line}: {p.Message}")));
        return Assert.IsType<TableNode>(Assert.Single(result.Nodes));
    }

    // ------------------------------------------------------------------ what it represents

    [Fact]
    public void A_pipe_table_becomes_a_table_node_with_a_head_and_rows()
    {
        var table = SingleTable(Convert("""
            | Term | Meaning |
            |---|:--:|
            | Producer | The organisation |
            | Corpus | The repositories |
            """));

        Assert.Equal(["Term", "Meaning"], Cells(table.Head));
        Assert.Equal(2, table.Rows.Count);
        Assert.Equal(["Producer", "The organisation"], Cells(table.Rows[0]));
        Assert.Equal(["Corpus", "The repositories"], Cells(table.Rows[1]));
    }

    [Fact]
    public void A_converted_table_declares_its_cells_inline_html()
    {
        // Said on the node, not inferred by the renderer: the same characters mean different
        // things in a plain-text table and an inline one.
        var table = SingleTable(Convert("| a |\n|---|\n| 1 |"));

        Assert.Equal(TableCellMarkup.Inline, table.CellMarkup);
    }

    [Fact]
    public void Outer_pipes_are_optional_as_on_GitHub()
    {
        var table = SingleTable(Convert("Term | Meaning\n--- | ---\nA | B"));

        Assert.Equal(["Term", "Meaning"], Cells(table.Head));
        Assert.Equal(["A", "B"], Cells(Assert.Single(table.Rows)));
    }

    [Fact]
    public void Cells_carry_bold_italic_and_links_as_canonical_inline_html()
    {
        var table = SingleTable(Convert("""
            | **Kind** | Where |
            |---|---|
            | *essay* | [the guide](https://example.test/guide) |
            """));

        Assert.Equal(["<strong>Kind</strong>", "Where"], Cells(table.Head));
        Assert.Equal(
            ["<em>essay</em>", "<a href=\"https://example.test/guide\">the guide</a>"],
            Cells(Assert.Single(table.Rows)));
    }

    [Fact]
    public void An_escaped_pipe_is_a_character_in_the_cell_not_a_column_boundary()
    {
        var table = SingleTable(Convert("| Expression | Means |\n|---|---|\n| a \\| b | either |"));

        Assert.Equal(["a | b", "either"], Cells(Assert.Single(table.Rows)));
    }

    [Fact]
    public void Cell_text_is_escaped_into_the_inline_grammar()
    {
        var table = SingleTable(Convert("| Rule |\n|---|\n| x < y & \"z\" |"));

        Assert.Equal(["x &lt; y &amp; &quot;z&quot;"], Cells(Assert.Single(table.Rows)));
    }

    [Fact]
    public void An_empty_cell_is_an_empty_cell()
    {
        var table = SingleTable(Convert("| a | b |\n|---|---|\n| 1 |  |"));

        Assert.Equal(["1", ""], Cells(Assert.Single(table.Rows)));
    }

    [Fact]
    public void A_table_with_only_a_header_is_a_table_with_no_rows()
    {
        var table = SingleTable(Convert("| a | b |\n|---|---|"));

        Assert.Equal(["a", "b"], Cells(table.Head));
        Assert.Empty(table.Rows);
    }

    [Fact]
    public void A_table_sits_between_paragraphs()
    {
        var result = Convert("Before.\n\n| a |\n|---|\n| 1 |\n\nAfter.");

        Assert.True(result.Ok);
        Assert.Collection(
            result.Nodes,
            n => Assert.IsType<RichTextNode>(n),
            n => Assert.IsType<TableNode>(n),
            n => Assert.IsType<RichTextNode>(n));
    }

    [Fact]
    public void A_table_directly_under_a_paragraph_line_is_still_a_table()
    {
        // Without this the header and delimiter rows would be swallowed into the paragraph as
        // text — the table silently dropped, which is the one outcome this converter never allows.
        var result = Convert("Compare:\n| a | b |\n|---|---|\n| 1 | 2 |");

        Assert.True(result.Ok);
        Assert.Equal("<p>Compare:</p>", Assert.IsType<RichTextNode>(result.Nodes[0]).Html.Resolve(En, En));
        Assert.IsType<TableNode>(result.Nodes[1]);
    }

    [Fact]
    public void A_heading_right_under_a_table_ends_it()
    {
        var result = Convert("| a |\n|---|\n| 1 |\n## Next");

        Assert.True(result.Ok);
        Assert.Single(Assert.IsType<TableNode>(result.Nodes[0]).Rows);
        Assert.IsType<HeadingNode>(result.Nodes[1]);
    }

    // --------------------------------------------------------------------- what it refuses

    [Fact]
    public void A_header_without_a_delimiter_row_is_refused_at_the_header_line()
    {
        var result = Convert("Intro.\n\n| a | b |\n| 1 | 2 |");

        var problem = Assert.Single(result.Problems);
        Assert.Equal(3, problem.Line);
        Assert.Contains("delimiter row", problem.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(result.Nodes, n => n is TableNode);
    }

    [Fact]
    public void A_delimiter_row_that_is_not_dashes_is_no_delimiter_row()
    {
        var result = Convert("| a | b |\n|---|abc|\n| 1 | 2 |");

        Assert.Equal(1, Assert.Single(result.Problems).Line);
        Assert.DoesNotContain(result.Nodes, n => n is TableNode);
    }

    [Fact]
    public void A_header_and_delimiter_with_different_column_counts_are_refused_at_the_delimiter()
    {
        var result = Convert("| a | b |\n|---|\n| 1 | 2 |");

        var problem = Assert.Single(result.Problems);
        Assert.Equal(2, problem.Line);
        Assert.Contains("1 column", problem.Message, StringComparison.Ordinal);
        Assert.Contains("2", problem.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(result.Nodes, n => n is TableNode);
    }

    [Fact]
    public void Every_ragged_row_is_refused_at_its_own_line_and_no_table_is_guessed()
    {
        // GitHub pads a short row and drops a long row's extra cells. Both lose or invent a cell
        // the author did not mean, so neither is done here.
        var result = Convert("Intro.\n\n| a | b |\n|---|---|\n| 1 | 2 |\n| 3 |\n| 4 | 5 | 6 |");

        Assert.Equal([6, 7], result.Problems.Select(p => p.Line).ToArray());
        Assert.Contains("1 cell", result.Problems[0].Message, StringComparison.Ordinal);
        Assert.Contains("3 cells", result.Problems[1].Message, StringComparison.Ordinal);
        Assert.DoesNotContain(result.Nodes, n => n is TableNode);
        Assert.IsType<RichTextNode>(Assert.Single(result.Nodes));
    }

    [Fact]
    public void A_text_line_run_into_a_table_is_a_ragged_row_not_a_paragraph()
    {
        // On GitHub that line becomes a row. Rendering it as a paragraph instead would show the
        // author something GitHub never would; refusing it says "leave a blank line".
        var result = Convert("| a | b |\n|---|---|\n| 1 | 2 |\nplain text");

        Assert.Equal(4, Assert.Single(result.Problems).Line);
    }

    [Theory]
    [InlineData("| `code` |", "Inline code")]
    [InlineData("| [x](/relative) |", "not a link this site can resolve")]
    [InlineData("| *open |", "Unmatched emphasis")]
    [InlineData("| a <span>b</span> |", "Raw HTML")]
    public void Inline_the_converter_refuses_elsewhere_is_refused_in_a_cell_at_the_row_line(string row, string expected)
    {
        var result = Convert($"| h |\n|---|\n| fine |\n{row}");

        Assert.False(result.Ok);
        Assert.All(result.Problems, p => Assert.Equal(4, p.Line));
        Assert.Contains(result.Problems, p => p.Message.Contains(expected, StringComparison.Ordinal));
    }

    [Fact]
    public void A_header_cell_problem_carries_the_header_line()
    {
        var result = Convert("Intro.\n\n| `h` |\n|---|\n| 1 |");

        Assert.Equal(3, Assert.Single(result.Problems).Line);
    }

    [Theory]
    [InlineData("    indented code", "Indented code", 1)]
    [InlineData("Fine.\n\n- one\n  - nested", "Nested lists", 4)]
    public void Indented_code_and_nested_lists_are_still_refused(string markdown, string expected, int line)
    {
        var result = Convert(markdown);

        var problem = Assert.Single(result.Problems);
        Assert.Equal(line, problem.Line);
        Assert.Contains(expected, problem.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_converted_table_passes_the_content_rules_the_aggregate_applies()
    {
        // The converter's output is only ever as good as what the store would accept.
        var table = SingleTable(Convert("| **a** | [b](https://example.test/) |\n|---|---|\n| x & y | z |"));

        NodeContentRules.Validate(table);
    }
}
