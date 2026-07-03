using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Pages.Events;
using Imprint.TestKit;

namespace Imprint.Authoring.Tests.Domain.Pages;

using static PageTestData;

public sealed class PageMoveNodeTests
{
    private readonly PageCreated _created = Created(PageId.New(), SiteId.New());
    private readonly SectionNode _sectionA;
    private readonly SectionNode _sectionB;
    private readonly StackNode _outerStack;
    private readonly StackNode _innerStack;
    private readonly HeadingNode _heading;
    private readonly DividerNode _divider;

    public PageMoveNodeTests()
    {
        _heading = Heading();
        _divider = Divider();
        _innerStack = Stack(_heading);
        _outerStack = Stack(_innerStack, _divider);
        _sectionA = Section(_outerStack);
        _sectionB = Section();
    }

    private AggregateSpec<Page> Spec() => AggregateSpec.For<Page>().Given(
        _created,
        new NodeAdded(NodeId.Root, 0, _sectionA),
        new NodeAdded(NodeId.Root, 1, _sectionB));

    [Fact]
    public void MoveNode_to_another_container_raises_event()
    {
        var outcome = Spec().When(p => p.MoveNode(_heading.Id, _outerStack.Id, 0));

        outcome.ThenRaised(new NodeMoved(_heading.Id, _outerStack.Id, 0));
        var outer = Assert.IsType<StackNode>(outcome.Aggregate.Tree.Find(_outerStack.Id));
        Assert.Equal(_heading.Id, outer.Children[0].Id);
        var inner = Assert.IsType<StackNode>(outcome.Aggregate.Tree.Find(_innerStack.Id));
        Assert.Empty(inner.Children);
    }

    [Fact]
    public void MoveNode_unknown_node_is_rejected()
    {
        Spec()
            .When(p => p.MoveNode(NodeId.New(), _outerStack.Id, 0))
            .ThenFails("no longer exists");
    }

    [Fact]
    public void MoveNode_into_own_descendant_is_rejected()
    {
        Spec()
            .When(p => p.MoveNode(_outerStack.Id, _innerStack.Id, 0))
            .ThenFails("into itself");
    }

    [Fact]
    public void MoveNode_into_itself_is_rejected()
    {
        Spec()
            .When(p => p.MoveNode(_outerStack.Id, _outerStack.Id, 0))
            .ThenFails("into itself");
    }

    [Fact]
    public void MoveNode_to_unknown_parent_is_rejected()
    {
        Spec()
            .When(p => p.MoveNode(_heading.Id, NodeId.New(), 0))
            .ThenFails("no longer exists");
    }

    [Fact]
    public void MoveNode_into_a_leaf_is_rejected()
    {
        Spec()
            .When(p => p.MoveNode(_heading.Id, _divider.Id, 0))
            .ThenFails("cannot contain");
    }

    [Fact]
    public void MoveNode_non_section_to_root_is_rejected()
    {
        Spec()
            .When(p => p.MoveNode(_outerStack.Id, NodeId.Root, 0))
            .ThenFails("Only sections");
    }

    [Fact]
    public void MoveNode_section_into_a_container_is_rejected()
    {
        Spec()
            .When(p => p.MoveNode(_sectionB.Id, _outerStack.Id, 0))
            .ThenFails("not inside other");
    }

    [Fact]
    public void MoveNode_section_within_root_reorders()
    {
        var outcome = Spec().When(p => p.MoveNode(_sectionB.Id, NodeId.Root, 0));

        outcome.ThenRaised(new NodeMoved(_sectionB.Id, NodeId.Root, 0));
        Assert.Equal(_sectionB.Id, outcome.Aggregate.Tree.Roots[0].Id);
        Assert.Equal(_sectionA.Id, outcome.Aggregate.Tree.Roots[1].Id);
    }

    // ------------------------------------------------------- managed column cells

    [Fact]
    public void MoveNode_of_a_managed_column_cell_is_rejected()
    {
        var columns = Columns(2, 1);
        var cell = columns.Children[0];

        AggregateSpec.For<Page>()
            .Given(_created, new NodeAdded(NodeId.Root, 0, Section(columns)))
            .When(p => p.MoveNode(cell.Id, NodeId.Root, 0))
            .ThenFails("cannot be moved");
    }

    [Fact]
    public void MoveNode_into_columns_element_directly_is_rejected()
    {
        var columns = Columns(2, 1);

        AggregateSpec.For<Page>()
            .Given(
                _created,
                new NodeAdded(NodeId.Root, 0, _sectionA),
                new NodeAdded(_sectionA.Id, 1, columns))
            .When(p => p.MoveNode(_heading.Id, columns.Id, 0))
            .ThenFails("columns element itself");
    }

    [Fact]
    public void MoveNode_into_a_column_cell_is_allowed()
    {
        var columns = Columns(2, 1);
        var cell = columns.Children[1];

        AggregateSpec.For<Page>()
            .Given(
                _created,
                new NodeAdded(NodeId.Root, 0, _sectionA),
                new NodeAdded(_sectionA.Id, 1, columns))
            .When(p => p.MoveNode(_heading.Id, cell.Id, 0))
            .ThenRaised(new NodeMoved(_heading.Id, cell.Id, 0));
    }

    // ------------------------------------------------------------------- indexes

    [Fact]
    public void MoveNode_identical_position_raises_nothing()
    {
        Spec()
            .When(p => p.MoveNode(_divider.Id, _outerStack.Id, 1))
            .ThenNothing();
    }

    [Fact]
    public void MoveNode_within_same_parent_uses_index_after_removal()
    {
        // Two children: after taking the divider out, slot 1 is the last valid slot.
        var outcome = Spec().When(p => p.MoveNode(_divider.Id, _outerStack.Id, 0));

        outcome.ThenRaised(new NodeMoved(_divider.Id, _outerStack.Id, 0));
        var outer = Assert.IsType<StackNode>(outcome.Aggregate.Tree.Find(_outerStack.Id));
        Assert.Equal(_divider.Id, outer.Children[0].Id);
        Assert.Equal(_innerStack.Id, outer.Children[1].Id);
    }

    [Fact]
    public void MoveNode_within_same_parent_beyond_shrunk_range_is_rejected()
    {
        Spec()
            .When(p => p.MoveNode(_divider.Id, _outerStack.Id, 2))
            .ThenFails("outside the valid range");
    }

    [Fact]
    public void MoveNode_to_other_parent_with_index_beyond_count_is_rejected()
    {
        Spec()
            .When(p => p.MoveNode(_heading.Id, _outerStack.Id, 3))
            .ThenFails("outside the valid range");
    }

    [Fact]
    public void MoveNode_with_negative_index_is_rejected()
    {
        Spec()
            .When(p => p.MoveNode(_heading.Id, _outerStack.Id, -1))
            .ThenFails("outside the valid range");
    }

    // ---------------------------------------------------------------------- caps

    [Fact]
    public void MoveNode_subtree_beyond_max_depth_is_rejected()
    {
        var (deepRoot, deepest) = NestedSection(stackDepth: 6); // deepest at depth 7

        AggregateSpec.For<Page>()
            .Given(
                _created,
                new NodeAdded(NodeId.Root, 0, deepRoot),
                new NodeAdded(NodeId.Root, 1, _sectionA))
            .When(p => p.MoveNode(_innerStack.Id, deepest.Id, 0)) // stack+heading → depth 9
            .ThenFails("levels deep");
    }

    [Fact]
    public void MoveNode_leaf_to_max_depth_is_accepted()
    {
        var (deepRoot, deepest) = NestedSection(stackDepth: 6);

        AggregateSpec.For<Page>()
            .Given(
                _created,
                new NodeAdded(NodeId.Root, 0, deepRoot),
                new NodeAdded(NodeId.Root, 1, _sectionA))
            .When(p => p.MoveNode(_heading.Id, deepest.Id, 0)) // leaf at depth 8
            .ThenRaised(new NodeMoved(_heading.Id, deepest.Id, 0));
    }
}
