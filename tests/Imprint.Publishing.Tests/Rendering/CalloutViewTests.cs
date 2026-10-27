using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Rendering;

namespace Imprint.Publishing.Tests.Rendering;

/// <summary>
/// A callout renders as an aside a screen reader announces as a note, with its label as real text
/// (so "Warning" is heard, not only seen), and its children rendered as ordinary prose.
/// </summary>
public sealed class CalloutViewTests
{
    private static readonly RenderContext Static = RenderHarness.Context(RenderMode.Static);

    private static CalloutNode Callout(CalloutTone tone) => new()
    {
        Id = NodeId.New(),
        Tone = tone,
        Children = NodeList.Of(SampleNodes.RichText("<p>Set apart.</p>")),
    };

    [Fact]
    public async Task A_warning_renders_as_a_labelled_note_aside()
    {
        var html = await RenderHarness.RenderNode(Static, Callout(CalloutTone.Warning));

        Assert.Contains("<aside class=\"ip-callout ip-callout-warning\" role=\"note\"", html);
        Assert.Contains("<p class=\"ip-callout-label\">Warning</p>", html);
        Assert.Contains("<p>Set apart.</p>", html);
    }

    [Fact]
    public async Task A_plain_callout_has_no_label()
    {
        var html = await RenderHarness.RenderNode(Static, Callout(CalloutTone.Plain));

        Assert.Contains("<aside class=\"ip-callout\" role=\"note\"", html);
        Assert.DoesNotContain("ip-callout-label", html);
    }
}
