using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Pages.Events;
using Imprint.TestKit;

namespace Imprint.Authoring.Tests.Domain.Pages;

using static PageTestData;

public sealed class PageRemoveAndDuplicateTests
{
    private readonly PageCreated _created = Created(PageId.New(), SiteId.New());
    private readonly SectionNode _section;
    private readonly StackNode _stack;
    private readonly HeadingNode _heading;
    private readonly ColumnsNode _columns;

    public PageRemoveAndDuplicateTests()
    {
        _heading = Heading();
        _columns = Columns([2, 1], [Stack(Heading("In a cell")), Stack()]);
        _stack = Stack(_heading, _columns);
        _section = Section(_stack);
    }

    private AggregateSpec<Page> Spec() =>
        AggregateSpec.For<Page>().Given(_created, new NodeAdded(NodeId.Root, 0, _section));

    // ------------------------------------------------------------------- remove

    [Fact]
    public void RemoveNode_raises_event_and_folds()
    {
        var outcome = Spec().When(p => p.RemoveNode(_heading.Id));

        outcome.ThenRaised(new NodeRemoved(_heading.Id));
        Assert.False(outcome.Aggregate.Tree.Contains(_heading.Id));
    }

    [Fact]
    public void RemoveNode_unknown_node_is_rejected()
    {
        Spec()
            .When(p => p.RemoveNode(NodeId.New()))
            .ThenFails("no longer exists");
    }

    [Fact]
    public void RemoveNode_of_a_managed_column_cell_is_rejected()
    {
        Spec()
            .When(p => p.RemoveNode(_columns.Children[0].Id))
            .ThenFails("cannot be removed");
    }

    [Fact]
    public void RemoveNode_of_a_whole_columns_element_is_allowed()
    {
        Spec()
            .When(p => p.RemoveNode(_columns.Id))
            .ThenRaised(new NodeRemoved(_columns.Id));
    }

    // ---------------------------------------------------------------- duplicate

    [Fact]
    public void DuplicateNode_unknown_node_is_rejected()
    {
        Spec()
            .When(p => p.DuplicateNode(NodeId.New(), NodeId.New()))
            .ThenFails("no longer exists");
    }

    [Fact]
    public void DuplicateNode_of_a_managed_column_cell_is_rejected()
    {
        Spec()
            .When(p => p.DuplicateNode(_columns.Children[0].Id, NodeId.New()))
            .ThenFails("cannot be duplicated");
    }

    [Fact]
    public void DuplicateNode_carries_a_deep_copy_with_fresh_ids()
    {
        var outcome = Spec().When(p => p.DuplicateNode(_columns.Id, NodeId.New()));

        var raised = Assert.IsType<NodeDuplicated>(Assert.Single(outcome.Raised));
        Assert.Equal(_columns.Id, raised.SourceId);

        var sourceNodes = PageTree.Flatten(_columns).ToList();
        var copyNodes = PageTree.Flatten(raised.Copy).ToList();
        Assert.Equal(sourceNodes.Count, copyNodes.Count);
        Assert.Equal(sourceNodes.Select(n => n.GetType()), copyNodes.Select(n => n.GetType()));

        var sourceIds = sourceNodes.Select(n => n.Id).ToHashSet();
        Assert.All(copyNodes, node => Assert.DoesNotContain(node.Id, sourceIds));

        // Ids aside, the copy is content-identical to the source.
        var copyColumns = Assert.IsType<ColumnsNode>(raised.Copy);
        Assert.Equal(_columns.Ratios, copyColumns.Ratios);
        var copyCellHeading = Assert.IsType<HeadingNode>(
            Assert.IsType<StackNode>(copyColumns.Children[0]).Children[0]);
        Assert.Equal("In a cell", copyCellHeading.Text.Get(En));
    }

    [Fact]
    public void DuplicateNode_inserts_the_copy_immediately_after_the_source()
    {
        var outcome = Spec().When(p => p.DuplicateNode(_heading.Id, NodeId.New()));

        var raised = Assert.IsType<NodeDuplicated>(Assert.Single(outcome.Raised));
        var stack = Assert.IsType<StackNode>(outcome.Aggregate.Tree.Find(_stack.Id));
        Assert.Equal(_heading.Id, stack.Children[0].Id);
        Assert.Equal(raised.Copy.Id, stack.Children[1].Id);
        Assert.Equal(_columns.Id, stack.Children[2].Id);
    }

    [Fact]
    public void DuplicateNode_replay_yields_the_identical_tree()
    {
        var outcome = Spec().When(p => p.DuplicateNode(_columns.Id, NodeId.New()));

        // Determinism: the event alone must rebuild the exact same tree — the fold
        // may not mint ids or re-derive anything.
        var replayed = new Page();
        replayed.LoadFrom([_created, new NodeAdded(NodeId.Root, 0, _section), .. outcome.Raised]);

        Assert.Equal(outcome.Aggregate.Tree, replayed.Tree);
    }

    [Fact]
    public void DuplicateNode_beyond_max_node_count_is_rejected()
    {
        var big = Section([.. Enumerable.Range(0, 250).Select(_ => (Node)Spacer())]); // 251 nodes

        AggregateSpec.For<Page>()
            .Given(_created, new NodeAdded(NodeId.Root, 0, big))
            .When(p => p.DuplicateNode(big.Id, NodeId.New())) // 251 + 251 = 502 > 500
            .ThenFails("500 elements");
    }

    [Fact]
    public void DuplicateNode_reaching_exactly_max_node_count_is_accepted()
    {
        var big = Section([.. Enumerable.Range(0, 249).Select(_ => (Node)Spacer())]); // 250 nodes

        var outcome = AggregateSpec.For<Page>()
            .Given(_created, new NodeAdded(NodeId.Root, 0, big))
            .When(p => p.DuplicateNode(big.Id, NodeId.New())); // 250 + 250 = 500

        Assert.Single(outcome.Raised);
        Assert.Equal(500, outcome.Aggregate.Tree.Count());
    }
}
