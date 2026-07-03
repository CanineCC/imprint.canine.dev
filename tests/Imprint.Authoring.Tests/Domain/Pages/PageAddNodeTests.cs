using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Pages.Events;
using Imprint.TestKit;

namespace Imprint.Authoring.Tests.Domain.Pages;

using static PageTestData;

public sealed class PageAddNodeTests
{
    private readonly PageCreated _created = Created(PageId.New(), SiteId.New());
    private readonly SectionNode _section;
    private readonly StackNode _stack;
    private readonly HeadingNode _heading;

    public PageAddNodeTests()
    {
        _heading = Heading();
        _stack = Stack(_heading);
        _section = Section(_stack);
    }

    private AggregateSpec<Page> Spec() =>
        AggregateSpec.For<Page>().Given(_created, new NodeAdded(NodeId.Root, 0, _section));

    // ---------------------------------------------------------------- placement

    [Fact]
    public void AddNode_section_at_root_raises_event()
    {
        var section = Section();
        var outcome = Spec().When(p => p.AddNode(NodeId.Root, 1, section));

        outcome.ThenRaised(new NodeAdded(NodeId.Root, 1, section));
        Assert.Equal(section, outcome.Aggregate.Tree.Roots[1]);
    }

    [Fact]
    public void AddNode_non_section_at_root_is_rejected()
    {
        Spec()
            .When(p => p.AddNode(NodeId.Root, 0, Heading()))
            .ThenFails("Only sections");
    }

    [Fact]
    public void AddNode_section_inside_a_container_is_rejected()
    {
        Spec()
            .When(p => p.AddNode(_stack.Id, 0, Section()))
            .ThenFails("not inside other");
    }

    [Fact]
    public void AddNode_into_unknown_parent_is_rejected()
    {
        Spec()
            .When(p => p.AddNode(NodeId.New(), 0, Heading()))
            .ThenFails("no longer exists");
    }

    [Fact]
    public void AddNode_into_a_leaf_is_rejected()
    {
        Spec()
            .When(p => p.AddNode(_heading.Id, 0, Spacer()))
            .ThenFails("cannot contain");
    }

    [Fact]
    public void AddNode_into_columns_element_directly_is_rejected()
    {
        var columns = Columns(2, 1);

        AggregateSpec.For<Page>()
            .Given(_created, new NodeAdded(NodeId.Root, 0, Section(columns)))
            .When(p => p.AddNode(columns.Id, 0, Heading()))
            .ThenFails("columns element itself");
    }

    [Fact]
    public void AddNode_into_a_column_cell_is_allowed()
    {
        var columns = Columns(2, 1);
        var cell = columns.Children[0];
        var heading = Heading();

        AggregateSpec.For<Page>()
            .Given(_created, new NodeAdded(NodeId.Root, 0, Section(columns)))
            .When(p => p.AddNode(cell.Id, 0, heading))
            .ThenRaised(new NodeAdded(cell.Id, 0, heading));
    }

    [Fact]
    public void AddNode_spec_with_invalid_nested_placement_is_rejected()
    {
        // The section hides one level down inside an otherwise legal stack spec.
        Spec()
            .When(p => p.AddNode(_stack.Id, 0, Stack(Section())))
            .ThenFails("not inside other");
    }

    // -------------------------------------------------------------------- fresh ids

    [Fact]
    public void AddNode_spec_reusing_an_id_from_the_tree_is_rejected()
    {
        Spec()
            .When(p => p.AddNode(_stack.Id, 0, new SpacerNode { Id = _heading.Id }))
            .ThenFails("already on this page");
    }

    [Fact]
    public void AddNode_spec_with_duplicate_ids_inside_itself_is_rejected()
    {
        var duplicated = NodeId.New();

        Spec()
            .When(p => p.AddNode(_stack.Id, 0, Stack(
                new SpacerNode { Id = duplicated },
                new DividerNode { Id = duplicated })))
            .ThenFails("already on this page");
    }

    [Fact]
    public void AddNode_spec_using_the_root_sentinel_id_is_rejected()
    {
        Spec()
            .When(p => p.AddNode(_stack.Id, 0, new SpacerNode { Id = NodeId.Root }))
            .ThenFails("reserved");
    }

    // ---------------------------------------------------------------- columns specs

    [Fact]
    public void AddNode_valid_columns_spec_is_accepted()
    {
        var columns = Columns(2, 1, 1);

        Spec()
            .When(p => p.AddNode(_stack.Id, 0, columns))
            .ThenRaised(new NodeAdded(_stack.Id, 0, columns));
    }

    [Fact]
    public void AddNode_columns_spec_with_cell_count_mismatch_is_rejected()
    {
        Spec()
            .When(p => p.AddNode(_stack.Id, 0, Columns([2, 1], [Stack()])))
            .ThenFails("one cell per ratio");
    }

    [Fact]
    public void AddNode_columns_spec_with_non_stack_cell_is_rejected()
    {
        Spec()
            .When(p => p.AddNode(_stack.Id, 0, Columns([2, 1], [Stack(), Grid()])))
            .ThenFails("must be stacks");
    }

    [Fact]
    public void AddNode_columns_spec_with_a_single_ratio_is_rejected()
    {
        Spec()
            .When(p => p.AddNode(_stack.Id, 0, Columns([2], [Stack()])))
            .ThenFails("between 2 and 4");
    }

    [Fact]
    public void AddNode_columns_spec_with_five_ratios_is_rejected()
    {
        Spec()
            .When(p => p.AddNode(_stack.Id, 0, Columns(1, 1, 1, 1, 1)))
            .ThenFails("between 2 and 4");
    }

    [Fact]
    public void AddNode_columns_spec_with_ratio_out_of_range_is_rejected()
    {
        Spec()
            .When(p => p.AddNode(_stack.Id, 0, Columns(4, 1)))
            .ThenFails("between 1 and 3");
    }

    [Fact]
    public void AddNode_nested_columns_spec_is_validated_too()
    {
        // The invalid ratio sits on a columns element nested inside a valid stack.
        Spec()
            .When(p => p.AddNode(_stack.Id, 0, Stack(Columns(0, 1))))
            .ThenFails("between 1 and 3");
    }

    // ------------------------------------------------------------------- indexes

    [Fact]
    public void AddNode_with_negative_index_is_rejected()
    {
        Spec()
            .When(p => p.AddNode(_stack.Id, -1, Heading()))
            .ThenFails("outside the valid range");
    }

    [Fact]
    public void AddNode_with_index_beyond_child_count_is_rejected()
    {
        Spec()
            .When(p => p.AddNode(_stack.Id, 2, Heading()))
            .ThenFails("outside the valid range");
    }

    [Fact]
    public void AddNode_with_index_equal_to_child_count_appends()
    {
        var divider = Divider();
        var outcome = Spec().When(p => p.AddNode(_stack.Id, 1, divider));

        outcome.ThenRaised(new NodeAdded(_stack.Id, 1, divider));
        var stack = Assert.IsType<StackNode>(outcome.Aggregate.Tree.Find(_stack.Id));
        Assert.Equal(divider.Id, stack.Children[1].Id);
    }

    // ---------------------------------------------------------------------- caps

    [Fact]
    public void AddNode_beyond_max_depth_is_rejected()
    {
        var (root, deepest) = NestedSection(stackDepth: 6); // deepest stack at depth 7

        AggregateSpec.For<Page>()
            .Given(_created, new NodeAdded(NodeId.Root, 0, root))
            .When(p => p.AddNode(deepest.Id, 0, Stack(Heading()))) // would reach depth 9
            .ThenFails("levels deep");
    }

    [Fact]
    public void AddNode_reaching_exactly_max_depth_is_accepted()
    {
        var (root, deepest) = NestedSection(stackDepth: 6);
        var heading = Heading();

        AggregateSpec.For<Page>()
            .Given(_created, new NodeAdded(NodeId.Root, 0, root))
            .When(p => p.AddNode(deepest.Id, 0, heading)) // leaf at depth 8
            .ThenRaised(new NodeAdded(deepest.Id, 0, heading));
    }

    [Fact]
    public void AddNode_beyond_max_node_count_is_rejected()
    {
        var big = Section([.. Enumerable.Range(0, 499).Select(_ => (Node)Spacer())]);

        AggregateSpec.For<Page>()
            .Given(_created, new NodeAdded(NodeId.Root, 0, big)) // exactly 500 nodes
            .When(p => p.AddNode(NodeId.Root, 1, Section()))
            .ThenFails("500 elements");
    }

    [Fact]
    public void AddNode_reaching_exactly_max_node_count_is_accepted()
    {
        var big = Section([.. Enumerable.Range(0, 499).Select(_ => (Node)Spacer())]);

        AggregateSpec.For<Page>()
            .Given(_created)
            .When(p => p.AddNode(NodeId.Root, 0, big))
            .ThenRaised(new NodeAdded(NodeId.Root, 0, big));
    }
}
