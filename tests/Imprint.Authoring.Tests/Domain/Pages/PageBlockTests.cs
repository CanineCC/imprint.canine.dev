using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Pages.Events;
using Imprint.TestKit;

namespace Imprint.Authoring.Tests.Domain.Pages;

using static PageTestData;

public sealed class PageBlockTests
{
    private readonly PageCreated _created = Created(PageId.New(), SiteId.New());
    private readonly BlockInstanceNode _instance;
    private readonly HeadingNode _heading;
    private readonly StackNode _stack;
    private readonly SectionNode _section;
    private readonly NodeId _definitionNode = NodeId.New();

    public PageBlockTests()
    {
        _instance = BlockInstance();
        _heading = Heading();
        _stack = Stack(_heading, _instance);
        _section = Section(_stack);
    }

    private AggregateSpec<Page> Spec() =>
        AggregateSpec.For<Page>().Given(_created, new NodeAdded(NodeId.Root, 0, _section));

    // ----------------------------------------------------------------- overrides

    [Fact]
    public void SetBlockOverride_raises_event_and_folds()
    {
        var outcome = Spec().When(p => p.SetBlockOverride(_instance.Id, _definitionNode, "text", En, "Custom headline"));

        outcome.ThenRaised(new BlockOverrideSet(_instance.Id, _definitionNode, "text", En, "Custom headline"));
        var instance = Assert.IsType<BlockInstanceNode>(outcome.Aggregate.Tree.Find(_instance.Id));
        Assert.Equal("Custom headline", instance.Overrides.Get(_definitionNode, "text", En));
    }

    [Fact]
    public void SetBlockOverride_with_null_clears_the_override()
    {
        var outcome = AggregateSpec.For<Page>()
            .Given(
                _created,
                new NodeAdded(NodeId.Root, 0, _section),
                new BlockOverrideSet(_instance.Id, _definitionNode, "text", En, "Custom headline"))
            .When(p => p.SetBlockOverride(_instance.Id, _definitionNode, "text", En, null));

        outcome.ThenRaised(new BlockOverrideSet(_instance.Id, _definitionNode, "text", En, null));
        var instance = Assert.IsType<BlockInstanceNode>(outcome.Aggregate.Tree.Find(_instance.Id));
        Assert.Null(instance.Overrides.Get(_definitionNode, "text", En));
    }

    [Fact]
    public void SetBlockOverride_on_unknown_instance_is_rejected()
    {
        Spec()
            .When(p => p.SetBlockOverride(NodeId.New(), _definitionNode, "text", En, "x"))
            .ThenFails("block instance");
    }

    [Fact]
    public void SetBlockOverride_on_a_non_instance_node_is_rejected()
    {
        Spec()
            .When(p => p.SetBlockOverride(_heading.Id, _definitionNode, "text", En, "x"))
            .ThenFails("not a block instance");
    }

    [Fact]
    public void SetBlockOverride_with_unknown_field_is_rejected()
    {
        Spec()
            .When(p => p.SetBlockOverride(_instance.Id, _definitionNode, "tag", En, "x"))
            .ThenFails("cannot be overridden");
    }

    [Fact]
    public void SetBlockOverride_html_value_must_be_canonical()
    {
        Spec()
            .When(p => p.SetBlockOverride(_instance.Id, _definitionNode, "html", En, "<div>nope</div>"))
            .ThenFails("Expected <p>");
    }

    [Fact]
    public void SetBlockOverride_canonical_html_value_is_accepted()
    {
        const string html = "<p>Overridden <em>body</em>.</p>";

        Spec()
            .When(p => p.SetBlockOverride(_instance.Id, _definitionNode, "html", En, html))
            .ThenRaised(new BlockOverrideSet(_instance.Id, _definitionNode, "html", En, html));
    }

    [Fact]
    public void SetBlockOverride_plain_value_over_500_characters_is_rejected()
    {
        Spec()
            .When(p => p.SetBlockOverride(_instance.Id, _definitionNode, "label", En, new string('x', 501)))
            .ThenFails("500 characters");
    }

    // -------------------------------------------------------------------- detach

    [Fact]
    public void DetachBlockInstance_raises_event_and_swaps_in_place()
    {
        var replacement = Stack(Heading("Resolved"), Button("Resolved CTA"));
        var outcome = Spec().When(p => p.DetachBlockInstance(_instance.Id, replacement));

        outcome.ThenRaised(new BlockInstanceDetached(_instance.Id, replacement));
        Assert.False(outcome.Aggregate.Tree.Contains(_instance.Id));

        // The replacement occupies the instance's exact slot: after the heading.
        var stack = Assert.IsType<StackNode>(outcome.Aggregate.Tree.Find(_stack.Id));
        Assert.Equal(2, stack.Children.Count);
        Assert.Equal(_heading.Id, stack.Children[0].Id);
        Assert.Equal(replacement.Id, stack.Children[1].Id);
    }

    [Fact]
    public void DetachBlockInstance_on_unknown_instance_is_rejected()
    {
        Spec()
            .When(p => p.DetachBlockInstance(NodeId.New(), Stack()))
            .ThenFails("block instance");
    }

    [Fact]
    public void DetachBlockInstance_on_a_non_instance_node_is_rejected()
    {
        Spec()
            .When(p => p.DetachBlockInstance(_heading.Id, Stack()))
            .ThenFails("not a block instance");
    }

    [Fact]
    public void DetachBlockInstance_replacement_reusing_a_tree_id_is_rejected()
    {
        Spec()
            .When(p => p.DetachBlockInstance(_instance.Id, Stack(new HeadingNode { Id = _heading.Id })))
            .ThenFails("already on this page");
    }

    [Fact]
    public void DetachBlockInstance_replacement_with_invalid_placement_is_rejected()
    {
        // A section cannot live inside the stack that held the instance.
        Spec()
            .When(p => p.DetachBlockInstance(_instance.Id, Section()))
            .ThenFails("not inside other");
    }

    [Fact]
    public void DetachBlockInstance_replacement_with_invalid_nested_columns_is_rejected()
    {
        Spec()
            .When(p => p.DetachBlockInstance(_instance.Id, Stack(Columns([2, 1], [Stack()]))))
            .ThenFails("one cell per ratio");
    }

    [Fact]
    public void DetachBlockInstance_beyond_max_depth_is_rejected()
    {
        var (root, deepest) = NestedSection(stackDepth: 6); // deepest stack at depth 7
        var instance = BlockInstance();

        AggregateSpec.For<Page>()
            .Given(
                _created,
                new NodeAdded(NodeId.Root, 0, root),
                new NodeAdded(deepest.Id, 0, instance)) // instance at depth 8
            .When(p => p.DetachBlockInstance(instance.Id, Stack(Heading()))) // depth 9
            .ThenFails("levels deep");
    }

    [Fact]
    public void DetachBlockInstance_counts_the_replaced_node_against_the_cap()
    {
        // section + stack + instance + 497 spacers = 500 nodes; the detach removes
        // the instance (499) so a two-node replacement overflows by exactly one.
        var filler = Enumerable.Range(0, 497).Select(_ => (Node)Spacer());
        var stack = Stack([_instance, .. filler]);

        AggregateSpec.For<Page>()
            .Given(_created, new NodeAdded(NodeId.Root, 0, Section(stack)))
            .When(p => p.DetachBlockInstance(_instance.Id, Stack(Heading())))
            .ThenFails("500 elements");
    }

    [Fact]
    public void DetachBlockInstance_replay_yields_the_identical_tree()
    {
        var replacement = Stack(Heading("Resolved"));
        var outcome = Spec().When(p => p.DetachBlockInstance(_instance.Id, replacement));

        var replayed = new Page();
        replayed.LoadFrom([_created, new NodeAdded(NodeId.Root, 0, _section), .. outcome.Raised]);

        Assert.Equal(outcome.Aggregate.Tree, replayed.Tree);
    }
}
