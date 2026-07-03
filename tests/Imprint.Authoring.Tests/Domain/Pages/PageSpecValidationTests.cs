using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Pages.Events;
using Imprint.TestKit;

namespace Imprint.Authoring.Tests.Domain.Pages;

using static PageTestData;

/// <summary>
/// The publisher renders stored RichText as raw markup, so "stored content is always
/// canonical" must be an invariant at EVERY entry point — not just EditText. AddNode
/// and ChangeNodeProps carry whole node specs; these tests pin that they validate
/// content the same way (a stored-XSS regression guard) and enforce the documented
/// property ranges.
/// </summary>
public sealed class PageSpecValidationTests
{
    private readonly PageCreated _created = Created(PageId.New(), SiteId.New());
    private readonly SectionNode _section = Section(Stack());

    private AggregateSpec<Page> Spec() =>
        AggregateSpec.For<Page>().Given(_created, new NodeAdded(NodeId.Root, 0, _section));

    private NodeId StackId => _section.Children[0].Id;

    [Fact]
    public void AddNode_with_non_canonical_richtext_is_rejected()
    {
        var evil = new RichTextNode { Id = NodeId.New(), Html = LocalizedText.Of(En, "<script>alert(1)</script>") };

        Spec().When(p => p.AddNode(StackId, 0, evil)).ThenFails("Expected");
    }

    [Fact]
    public void AddNode_with_canonical_richtext_is_accepted()
    {
        var ok = new RichTextNode { Id = NodeId.New(), Html = LocalizedText.Of(En, "<p>Fine <strong>text</strong>.</p>") };

        Assert.NotEmpty(Spec().When(p => p.AddNode(StackId, 0, ok)).Raised);
    }

    [Fact]
    public void AddNode_with_non_canonical_richtext_in_a_nested_spec_is_rejected()
    {
        // The bad node is buried inside a section subtree — the walk must reach it.
        var evil = Section(Stack(new RichTextNode
        {
            Id = NodeId.New(),
            Html = LocalizedText.Of(En, "<p>ok</p><iframe src=x>"),
        }));

        Spec().When(p => p.AddNode(NodeId.Root, 1, evil)).ThenFails("Expected");
    }

    [Fact]
    public void ChangeNodeProps_cannot_smuggle_non_canonical_richtext()
    {
        var richId = NodeId.New();
        var good = new RichTextNode { Id = richId, Html = LocalizedText.Of(En, "<p>ok</p>") };
        var evil = new RichTextNode { Id = richId, Html = LocalizedText.Of(En, "<p onclick=\"x()\">bad</p>") };

        AggregateSpec.For<Page>()
            .Given(_created, new NodeAdded(NodeId.Root, 0, Section(Stack(good))))
            .When(p => p.ChangeNodeProps(evil))
            .ThenFails("Expected");
    }

    [Fact]
    public void AddNode_heading_level_out_of_range_is_rejected()
    {
        var badHeading = new HeadingNode { Id = NodeId.New(), Level = 7, Text = LocalizedText.Of(En, "Hi") };

        Spec().When(p => p.AddNode(StackId, 0, badHeading)).ThenFails("level");
    }

    [Fact]
    public void AddNode_grid_item_size_out_of_range_is_rejected()
    {
        var badGrid = new GridNode { Id = NodeId.New(), MinItemPx = 999 };

        Spec().When(p => p.AddNode(StackId, 0, badGrid)).ThenFails("between 160 and 480");
    }
}
