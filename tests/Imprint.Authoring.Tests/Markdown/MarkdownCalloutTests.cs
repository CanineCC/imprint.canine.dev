using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Markdown;

namespace Imprint.Authoring.Tests.Markdown;

/// <summary>
/// Callouts: a blockquote becomes a <see cref="CalloutNode"/>, optionally labelled NOTE or WARNING with
/// GitHub's alert line (<c>&gt; [!NOTE]</c>). Its inside is converted by the same rules as the rest of
/// the prose, and refused at the line it is on in the SOURCE, not in the quote. No other labels, and
/// no callout inside a callout.
/// </summary>
public sealed class MarkdownCalloutTests
{
    private static readonly Locale En = new("en");

    private static MarkdownConversion Convert(string markdown) => MarkdownToNodes.Convert(markdown, En);

    private static CalloutNode SingleCallout(MarkdownConversion result)
    {
        Assert.True(result.Ok, string.Join("; ", result.Problems.Select(p => $"line {p.Line}: {p.Message}")));
        return Assert.IsType<CalloutNode>(Assert.Single(result.Nodes));
    }

    private static string Html(Node node) => ((RichTextNode)node).Html.Get(En) ?? "";

    [Fact]
    public void A_blockquote_becomes_a_plain_callout_holding_its_paragraphs()
    {
        var callout = SingleCallout(Convert("> First **point**.\n>\n> Second point."));

        Assert.Equal(CalloutTone.Plain, callout.Tone);
        Assert.Equal(2, callout.Children.Count);
        Assert.Equal("<p>First <strong>point</strong>.</p>", Html(callout.Children[0]));
        Assert.Equal("<p>Second point.</p>", Html(callout.Children[1]));
    }

    [Theory]
    [InlineData("[!NOTE]", CalloutTone.Note)]
    [InlineData("[!WARNING]", CalloutTone.Warning)]
    [InlineData("[!note]", CalloutTone.Note)]
    public void An_alert_line_labels_the_callout_and_is_not_content(string alert, CalloutTone tone)
    {
        var callout = SingleCallout(Convert($"> {alert}\n> The figures are from cycle 1."));

        Assert.Equal(tone, callout.Tone);
        Assert.Equal("<p>The figures are from cycle 1.</p>", Html(Assert.Single(callout.Children)));
    }

    [Fact]
    public void A_callout_may_hold_a_list()
    {
        var callout = SingleCallout(Convert("> [!WARNING]\n> - one\n> - two"));

        Assert.Single(callout.Children);
        Assert.Contains("<li>one</li>", Html(callout.Children[0]), StringComparison.Ordinal);
    }

    [Fact]
    public void The_callout_ends_at_the_first_line_without_a_quote_marker()
    {
        var result = Convert("> Quoted.\n\nAfter.");

        Assert.True(result.Ok);
        Assert.IsType<CalloutNode>(result.Nodes[0]);
        Assert.Equal("<p>After.</p>", Html(result.Nodes[1]));
    }

    [Fact]
    public void An_unknown_alert_is_refused_naming_the_two_there_are()
    {
        var problem = Assert.Single(Convert("Intro.\n\n> [!CLAIM]\n> Text.").Problems);

        Assert.Equal(3, problem.Line);
        Assert.Contains("NOTE or WARNING", problem.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_problem_inside_a_callout_names_its_line_in_the_source()
    {
        // A paragraph's problems are reported at the paragraph's first line, so the faulty line is its own paragraph.
        var problem = Assert.Single(Convert("Intro.\n\n> [!NOTE]\n> Fine.\n>\n> Some <span>raw</span> here.").Problems);

        Assert.Equal(6, problem.Line);
        Assert.Contains("Raw HTML", problem.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_callout_inside_a_callout_is_refused()
    {
        var problem = Assert.Single(Convert("> Outer.\n> > Inner.").Problems);

        Assert.Equal(2, problem.Line);
        Assert.Contains("inside a callout", problem.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_empty_callout_is_refused()
    {
        var problem = Assert.Single(Convert("> [!NOTE]\n>").Problems);

        Assert.Equal(1, problem.Line);
        Assert.Contains("empty", problem.Message, StringComparison.Ordinal);
    }
}
