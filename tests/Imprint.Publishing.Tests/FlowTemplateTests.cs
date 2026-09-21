using Imprint.Publishing;

namespace Imprint.Publishing.Tests;

/// <summary>A diagram made entirely of sentences — and it was in a shadow root.</summary>
public sealed class FlowTemplateTests
{
    private const string Nodes = """
        [{"title":"Watchdog scans","body":"on your calendar cadence","tone":"default"},
         {"title":"Prioritised tasks","body":"served over MCP, ranked by impact","tone":"on"},
         {"title":"Your agent fixes","body":"in your own repo","tone":"default"},
         {"title":"The re-scan verifies","body":"fingerprint delta marks it fixed","tone":"default"}]
        """;

    private static Func<string, string?> Props(string? nodes, string? loop = null) => name => name switch
    {
        "nodes" => nodes,
        "loop-label" => loop,
        _ => null,
    };

    [Fact]
    public void Every_step_publishes_its_title_and_its_sentence()
    {
        var html = FlowTemplate.Render(Props(Nodes))!;

        Assert.Contains("Watchdog scans", html, StringComparison.Ordinal);
        Assert.Contains("served over MCP, ranked by impact", html, StringComparison.Ordinal);
        Assert.Contains("The re-scan verifies", html, StringComparison.Ordinal);
        Assert.Equal(4, html.Split("ip-flow-step").Length - 1);
    }

    [Fact]
    public void The_steps_are_an_ordered_list_because_they_are_ordered()
    {
        // ★ A row of boxes leaves the ORDER to visual inference. A screen reader now hears "1 of 4"
        //   instead of four unrelated headings, and the numbering is drawn from the list itself, so
        //   it cannot disagree with the order the markup is in.
        var html = FlowTemplate.Render(Props(Nodes))!;

        Assert.StartsWith("<ol class=\"ip-flow\">", html, StringComparison.Ordinal);
        Assert.Contains("<li class=\"ip-flow-step\">", html, StringComparison.Ordinal);
    }

    [Fact]
    public void The_loop_label_is_rendered_because_an_arrow_is_not_readable()
    {
        var html = FlowTemplate.Render(Props(Nodes, "The loop repeats — the score only moves when the re-scan proves it."))!;

        Assert.Contains("The loop repeats", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Text_from_the_props_is_escaped()
    {
        var html = FlowTemplate.Render(Props("""[{"title":"<script>x</script>","body":"<b>y</b>"}]"""))!;

        Assert.DoesNotContain("<script>", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<b>", html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("[{\"body\":\"no title\"}]")]
    public void Nothing_to_show_renders_nothing(string? nodes) =>
        Assert.Null(FlowTemplate.Render(Props(nodes)));

    [Fact]
    public void The_dispatcher_routes_the_name_to_the_template() =>
        Assert.Contains("ip-flow", PrerenderTemplates.RenderFromProps(FlowTemplate.Name, Props(Nodes))!,
            StringComparison.Ordinal);
}
