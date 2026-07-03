using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Pages.Events;
using Imprint.TestKit;

namespace Imprint.Authoring.Tests.Domain.Pages;

using static PageTestData;

public sealed class PageChangeNodePropsTests
{
    private readonly PageCreated _created = Created(PageId.New(), SiteId.New());
    private readonly SectionNode _section;
    private readonly StackNode _stack;
    private readonly HeadingNode _heading;
    private readonly ColumnsNode _columns;
    private readonly StackNode _filledCell;

    public PageChangeNodePropsTests()
    {
        _heading = Heading();
        _filledCell = Stack(Heading("In a cell"));
        _columns = Columns([2, 1], [_filledCell, Stack()]);
        _stack = Stack(_heading, _columns);
        _section = Section(_stack);
    }

    private AggregateSpec<Page> Spec() =>
        AggregateSpec.For<Page>().Given(_created, new NodeAdded(NodeId.Root, 0, _section));

    [Fact]
    public void ChangeNodeProps_on_a_leaf_replaces_the_node()
    {
        var replacement = _heading with { Level = 3 };
        var outcome = Spec().When(p => p.ChangeNodeProps(replacement));

        outcome.ThenRaised(new NodePropsChanged(replacement));
        Assert.Equal(replacement, outcome.Aggregate.Tree.Find(_heading.Id));
    }

    [Fact]
    public void ChangeNodeProps_unknown_node_is_rejected()
    {
        Spec()
            .When(p => p.ChangeNodeProps(Heading()))
            .ThenFails("no longer exists");
    }

    [Fact]
    public void ChangeNodeProps_changing_the_node_type_is_rejected()
    {
        Spec()
            .When(p => p.ChangeNodeProps(new SpacerNode { Id = _heading.Id }))
            .ThenFails("cannot be changed into");
    }

    [Fact]
    public void ChangeNodeProps_on_a_container_preserves_current_children()
    {
        // The editor sends props only — the replacement arrives childless and the
        // aggregate must resolve the children, not drop them.
        var replacement = new StackNode { Id = _stack.Id, Gap = Gap.Loose, Align = StackAlign.Center };
        var outcome = Spec().When(p => p.ChangeNodeProps(replacement));

        var raised = Assert.IsType<NodePropsChanged>(Assert.Single(outcome.Raised));
        var final = Assert.IsType<StackNode>(raised.Node);
        Assert.Equal(Gap.Loose, final.Gap);
        Assert.Equal(StackAlign.Center, final.Align);
        Assert.Equal(_stack.Children, final.Children);
        Assert.Equal(final, outcome.Aggregate.Tree.Find(_stack.Id));
    }

    [Fact]
    public void ChangeNodeProps_identical_result_raises_nothing()
    {
        // Same props, no children supplied: after resolving children the node is
        // unchanged, so nothing happened.
        Spec()
            .When(p => p.ChangeNodeProps(new StackNode { Id = _stack.Id }))
            .ThenNothing();
    }

    [Fact]
    public void ChangeNodeProps_identical_leaf_raises_nothing()
    {
        Spec()
            .When(p => p.ChangeNodeProps(_heading with { }))
            .ThenNothing();
    }

    // ------------------------------------------------------------------- columns

    [Fact]
    public void ChangeNodeProps_columns_growing_appends_fresh_empty_cells()
    {
        var replacement = new ColumnsNode { Id = _columns.Id, Ratios = [2, 1, 1] };
        var outcome = Spec().When(p => p.ChangeNodeProps(replacement));

        var raised = Assert.IsType<NodePropsChanged>(Assert.Single(outcome.Raised));
        var final = Assert.IsType<ColumnsNode>(raised.Node);
        Assert.Equal(3, final.Children.Count);
        Assert.Equal(_filledCell.Id, final.Children[0].Id); // existing cells kept
        Assert.Equal(_columns.Children[1].Id, final.Children[1].Id);

        var newCell = Assert.IsType<StackNode>(final.Children[2]);
        Assert.Empty(newCell.Children);
        // The appended cell id is genuinely fresh — it exists nowhere in the old tree.
        Assert.DoesNotContain(PageTree.Flatten(_section), n => n.Id == newCell.Id);
    }

    [Fact]
    public void ChangeNodeProps_columns_shrinking_with_empty_trailing_cell_is_accepted()
    {
        var grown = Columns([1, 1, 1], [Stack(Heading("Keep")), Stack(), Stack()]);
        var replacement = new ColumnsNode { Id = grown.Id, Ratios = [1, 1] };

        var outcome = AggregateSpec.For<Page>()
            .Given(_created, new NodeAdded(NodeId.Root, 0, Section(Stack(grown))))
            .When(p => p.ChangeNodeProps(replacement));

        var raised = Assert.IsType<NodePropsChanged>(Assert.Single(outcome.Raised));
        var final = Assert.IsType<ColumnsNode>(raised.Node);
        Assert.Equal(2, final.Children.Count);
        Assert.Equal(grown.Children[0].Id, final.Children[0].Id);
    }

    [Fact]
    public void ChangeNodeProps_columns_shrinking_over_a_non_empty_cell_is_rejected()
    {
        var grown = Columns([1, 1, 1], [Stack(), Stack(), Stack(Heading("Content"))]);

        AggregateSpec.For<Page>()
            .Given(_created, new NodeAdded(NodeId.Root, 0, Section(Stack(grown))))
            .When(p => p.ChangeNodeProps(new ColumnsNode { Id = grown.Id, Ratios = [1, 1] }))
            .ThenFails("move it");
    }

    [Fact]
    public void ChangeNodeProps_columns_ratio_out_of_range_is_rejected()
    {
        Spec()
            .When(p => p.ChangeNodeProps(new ColumnsNode { Id = _columns.Id, Ratios = [4, 1] }))
            .ThenFails("between 1 and 3");
    }

    [Fact]
    public void ChangeNodeProps_columns_ratio_count_out_of_range_is_rejected()
    {
        Spec()
            .When(p => p.ChangeNodeProps(new ColumnsNode { Id = _columns.Id, Ratios = [1] }))
            .ThenFails("between 2 and 4");

        Spec()
            .When(p => p.ChangeNodeProps(new ColumnsNode { Id = _columns.Id, Ratios = [1, 1, 1, 1, 1] }))
            .ThenFails("between 2 and 4");
    }

    [Fact]
    public void ChangeNodeProps_columns_replacement_children_are_ignored()
    {
        // A malicious/buggy client cannot smuggle new cells in via the replacement:
        // cells are derived from current state plus the ratio count only.
        var smuggled = new ColumnsNode
        {
            Id = _columns.Id,
            Ratios = [2, 1],
            Gap = Gap.Loose,
            Children = NodeList.Of(Stack(Heading("Smuggled")), Stack()),
        };

        var outcome = Spec().When(p => p.ChangeNodeProps(smuggled));

        var raised = Assert.IsType<NodePropsChanged>(Assert.Single(outcome.Raised));
        var final = Assert.IsType<ColumnsNode>(raised.Node);
        Assert.Equal(Gap.Loose, final.Gap);
        Assert.Equal(_columns.Children, final.Children);
    }

    [Fact]
    public void ChangeNodeProps_columns_identical_result_raises_nothing()
    {
        Spec()
            .When(p => p.ChangeNodeProps(new ColumnsNode { Id = _columns.Id, Ratios = [2, 1] }))
            .ThenNothing();
    }
}
