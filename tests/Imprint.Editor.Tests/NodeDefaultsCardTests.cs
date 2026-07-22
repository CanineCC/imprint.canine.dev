using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Editor.Services;

namespace Imprint.Editor.Tests;

/// <summary>
/// The one-click "Card" insertable: a StackNode pre-populated with a HeadingNode + a
/// RichTextNode (mirroring the marketing FeatureCard preset), so a real card is one
/// insert — and, being a single subtree, one Undo removes the whole thing.
/// </summary>
public sealed class NodeDefaultsCardTests
{
    private static readonly Locale En = new("en");

    [Fact]
    public void Card_default_is_a_stack_of_a_heading_and_rich_text()
    {
        var entry = NodeDefaults.All.Single(d => d.Name == "Card");
        var node = entry.Create(En);

        var stack = Assert.IsType<StackNode>(node);
        Assert.Collection(
            stack.Children,
            first => Assert.IsType<HeadingNode>(first),
            second => Assert.IsType<RichTextNode>(second));
    }

    [Fact]
    public void Card_inserts_into_a_section_through_the_aggregate_and_one_removal_takes_it_all()
    {
        // Root section on a page, then the card into it — the exact AddNode/RemoveNode
        // path InsertPicker drives, proving the compound is placeable and atomically undoable.
        var page = Page.Create(PageId.New(), SiteId.New(), Slug.TryCreate("home", out var slug, out _) ? slug : default!, En, "Home");
        var section = new SectionNode { Id = NodeId.New() };
        page.AddNode(NodeId.Root, 0, section);

        var card = NodeDefaults.All.Single(d => d.Name == "Card").Create(En);
        page.AddNode(section.Id, 0, card);
        Assert.NotNull(page.Tree.Find(card.Id));
        Assert.Equal(3, PageTree.Flatten(page.Tree.Find(card.Id)!).Count()); // stack + heading + richtext

        // The inverse InsertPicker computes (RemoveNode of the card's root) drops the whole card.
        page.RemoveNode(card.Id);
        Assert.Null(page.Tree.Find(card.Id));
    }
}
