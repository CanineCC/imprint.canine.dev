using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Rendering;

namespace Imprint.Publishing.Tests.Rendering;

/// <summary>
/// The table's contract: cells are CONTENT (escaped, never markup), the header row is
/// optional, cells resolve per locale with default-locale fallback, and the wrapper owns
/// horizontal overflow so a wide table never makes the page body scroll sideways.
/// </summary>
public sealed class TableViewTests
{
    private static readonly RenderContext Static = RenderHarness.Context(RenderMode.Static);

    private static LocalizedText En(string value) => LocalizedText.Of(RenderHarness.En, value);

    [Fact]
    public async Task Head_and_rows_render_as_a_real_table_inside_the_scroll_wrapper()
    {
        var node = new TableNode
        {
            Id = NodeId.New(),
            Head = [En("Term"), En("Meaning")],
            Rows = [[En("Producer"), En("The organisation responsible for the codebase.")]],
        };

        var html = await RenderHarness.RenderNode(Static, node);

        Assert.Contains("<div class=\"ip-table-wrap\"", html);
        Assert.Contains("<table class=\"ip-table\">", html);
        Assert.Contains("<th>Term</th><th>Meaning</th>", html);
        Assert.Contains("<td>Producer</td>", html);
    }

    [Fact]
    public async Task A_headless_table_emits_no_thead()
    {
        var node = new TableNode { Id = NodeId.New(), Rows = [[En("only body")]] };

        var html = await RenderHarness.RenderNode(Static, node);

        Assert.DoesNotContain("<thead>", html);
        Assert.Contains("<td>only body</td>", html);
    }

    [Fact]
    public async Task Cell_content_is_escaped_never_markup()
    {
        var node = new TableNode { Id = NodeId.New(), Rows = [[En("<script>alert(1)</script>")]] };

        var html = await RenderHarness.RenderNode(Static, node);

        Assert.DoesNotContain("<script>", html);
        Assert.Contains("&lt;script&gt;", html);
    }

    [Fact]
    public async Task Cells_fall_back_to_the_default_locale()
    {
        var node = new TableNode { Id = NodeId.New(), Rows = [[En("English cell")]] };
        var ctx = Static with { Locale = RenderHarness.Da };

        var html = await RenderHarness.RenderNode(ctx, node);

        Assert.Contains("<td>English cell</td>", html);
    }
}
