using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Posts;

namespace Imprint.Authoring.Tests.Domain.Posts;

/// <summary>
/// The one function the preview and the publisher share. If these two ever called different
/// code the preview would be a decoration; this pins that they cannot.
/// </summary>
public sealed class PostContentTests
{
    private static readonly Locale En = new("en");

    [Fact]
    public void A_body_is_wrapped_in_a_single_doc_section()
    {
        var (roots, problems) = PostContent.Render("# Title\n\nProse.", En);

        Assert.Empty(problems);
        var section = Assert.IsType<SectionNode>(Assert.Single(roots));
        // Doc is the measure-width reading column the theme already defines for a whole
        // markdown page — reused rather than duplicated into a second appearance.
        Assert.Equal(SectionAppearance.Doc, section.Appearance);
        Assert.Collection(
            section.Children,
            node => Assert.IsType<HeadingNode>(node),
            node => Assert.IsType<RichTextNode>(node));
    }

    [Fact]
    public void The_roots_are_placeable_on_a_page()
    {
        // The page root holds sections and nothing else. A body that converted straight to
        // content nodes could never be rendered as a page, so this is the invariant that makes
        // the wrapper necessary rather than cosmetic.
        var (roots, _) = PostContent.Render("Just prose.", En);

        Assert.True(Placement.CanPlace(null, Assert.Single(roots)));
    }

    [Fact]
    public void An_empty_body_still_yields_a_section()
    {
        // A post being written is still a page; a preview pane that vanished between
        // keystrokes would be worse than an empty column.
        var (roots, problems) = PostContent.Render("", En);

        Assert.Empty(problems);
        Assert.Empty(Assert.IsType<SectionNode>(Assert.Single(roots)).Children);
    }

    [Fact]
    public void Problems_travel_with_the_partial_render()
    {
        var (roots, problems) = PostContent.Render("Fine.\n\n> quoted", En);

        // What converted is still rendered — the preview shows the good paragraph and the
        // author is told about line 3, rather than being shown a blank pane.
        Assert.Single(problems);
        Assert.Single(Assert.IsType<SectionNode>(Assert.Single(roots)).Children);
    }

    [Fact]
    public void Every_id_comes_from_the_injected_source()
    {
        // Which node draws first is not a contract (the converter numbers the content, then
        // the wrapper takes the next) — that nothing reaches for NodeId.New behind the
        // caller's back is, because a re-render that minted fresh ids would defeat any caller
        // wanting a stable tree.
        var minted = new[] { NodeId.New(), NodeId.New(), NodeId.New() };
        var ids = new Queue<NodeId>(minted);

        var (roots, _) = PostContent.Render("Prose.", En, ids.Dequeue);

        var section = Assert.IsType<SectionNode>(Assert.Single(roots));
        Assert.Contains(section.Id, minted);
        Assert.Contains(Assert.Single(section.Children).Id, minted);
        Assert.NotEqual(section.Id, section.Children[0].Id);
    }
}
