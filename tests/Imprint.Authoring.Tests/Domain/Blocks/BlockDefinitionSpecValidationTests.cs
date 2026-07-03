using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Blocks;
using Imprint.Authoring.Domain.Blocks.Events;
using Imprint.Authoring.Domain.Pages;
using Imprint.EventSourcing;
using Imprint.TestKit;

namespace Imprint.Authoring.Tests.Domain.Blocks;

/// <summary>
/// The spec validation battery. Define and ChangeSpec share one validator, so Define
/// carries the full matrix and ChangeSpec re-checks each rule category once.
/// </summary>
public sealed class BlockDefinitionSpecValidationTests
{
    private static readonly BlockDefinitionId Id = BlockDefinitionId.New();
    private static readonly Locale En = new("en");

    private static HeadingNode Heading() =>
        new() { Id = NodeId.New(), Level = 3, Text = LocalizedText.Of(En, "Title") };

    private static StackNode Stack(params Node[] children) =>
        new() { Id = NodeId.New(), Children = NodeList.Of(children) };

    private static SectionNode Section() => new() { Id = NodeId.New() };

    private static BlockInstanceNode Instance() =>
        new() { Id = NodeId.New(), DefinitionId = BlockDefinitionId.New() };

    private static ColumnsNode Columns(int[] ratios, params Node[] cells) =>
        new() { Id = NodeId.New(), Ratios = [.. ratios], Children = NodeList.Of(cells) };

    private static Node NestedStacks(int depth)
    {
        Node current = new StackNode { Id = NodeId.New() };
        for (var i = 1; i < depth; i++)
        {
            current = new StackNode { Id = NodeId.New(), Children = NodeList.Of(current) };
        }

        return current;
    }

    private static Node StackWithDividers(int dividerCount) => new StackNode
    {
        Id = NodeId.New(),
        Children = NodeList.Of([.. Enumerable.Range(0, dividerCount).Select(Node (_) => new DividerNode { Id = NodeId.New() })]),
    };

    private static DomainException DefineFails(Node spec) =>
        Assert.Throws<DomainException>(() => BlockDefinition.Define(Id, "Hero", spec));

    // ---------------------------------------------------------------- root rule

    [Fact]
    public void Define_layout_root_is_accepted()
    {
        var definition = BlockDefinition.Define(Id, "Hero", Stack(Heading()));
        Assert.Single(definition.UncommittedEvents);
    }

    [Fact]
    public void Define_content_leaf_root_is_accepted()
    {
        var definition = BlockDefinition.Define(Id, "Just a heading", Heading());
        Assert.Single(definition.UncommittedEvents);
    }

    [Fact]
    public void Define_section_root_is_rejected() =>
        Assert.Contains("section", DefineFails(Section()).Message, StringComparison.OrdinalIgnoreCase);

    // ------------------------------------------------------- no nested symbols

    [Fact]
    public void Define_block_instance_root_is_rejected() =>
        Assert.Contains("another block", DefineFails(Instance()).Message, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void Define_deeply_nested_block_instance_is_rejected() =>
        Assert.Contains("another block", DefineFails(Stack(Stack(Instance()))).Message, StringComparison.OrdinalIgnoreCase);

    // ------------------------------------------------------------ placement walk

    [Fact]
    public void Define_section_nested_in_stack_is_rejected() =>
        Assert.Contains("placed inside", DefineFails(Stack(Section())).Message, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void Define_section_inside_column_cell_is_rejected() =>
        Assert.Contains("placed inside",
            DefineFails(Columns([2, 1], Stack(Section()), Stack())).Message, StringComparison.OrdinalIgnoreCase);

    // ------------------------------------------------------------------ unique ids

    [Fact]
    public void Define_duplicate_node_ids_is_rejected()
    {
        var shared = NodeId.New();
        var spec = new StackNode
        {
            Id = NodeId.New(),
            Children = NodeList.Of(new DividerNode { Id = shared }, new DividerNode { Id = shared }),
        };

        Assert.Contains("own id", DefineFails(spec).Message, StringComparison.OrdinalIgnoreCase);
    }

    // ------------------------------------------------------------- depth and size

    [Fact]
    public void Define_spec_at_seven_levels_is_accepted()
    {
        var definition = BlockDefinition.Define(Id, "Deep", NestedStacks(7));
        Assert.Single(definition.UncommittedEvents);
    }

    [Fact]
    public void Define_spec_at_eight_levels_is_rejected() =>
        Assert.Contains("levels deep", DefineFails(NestedStacks(8)).Message, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void Define_spec_with_200_nodes_is_accepted()
    {
        var definition = BlockDefinition.Define(Id, "Wide", StackWithDividers(199));
        Assert.Single(definition.UncommittedEvents);
    }

    [Fact]
    public void Define_spec_with_201_nodes_is_rejected() =>
        Assert.Contains("limited to 200 nodes", DefineFails(StackWithDividers(200)).Message, StringComparison.OrdinalIgnoreCase);

    // ------------------------------------------------------------------- columns

    [Fact]
    public void Define_columns_with_cells_matching_ratios_is_accepted()
    {
        var definition = BlockDefinition.Define(Id, "Two columns", Columns([2, 1], Stack(Heading()), Stack()));
        Assert.Single(definition.UncommittedEvents);
    }

    [Fact]
    public void Define_columns_with_one_ratio_is_rejected() =>
        Assert.Contains("between 2 and 4", DefineFails(Columns([2], Stack())).Message, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void Define_columns_with_five_ratios_is_rejected() =>
        Assert.Contains("between 2 and 4",
            DefineFails(Columns([1, 1, 1, 1, 1], Stack(), Stack(), Stack(), Stack(), Stack())).Message,
            StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void Define_columns_ratio_of_zero_is_rejected() =>
        Assert.Contains("between 1 and 3",
            DefineFails(Columns([2, 0], Stack(), Stack())).Message, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void Define_columns_ratio_of_four_is_rejected() =>
        Assert.Contains("between 1 and 3",
            DefineFails(Columns([2, 4], Stack(), Stack())).Message, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void Define_columns_with_missing_cell_is_rejected() =>
        Assert.Contains("one cell per column",
            DefineFails(Columns([2, 1], Stack())).Message, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void Define_columns_with_non_stack_cell_is_rejected() =>
        Assert.Contains("must be stacks",
            DefineFails(Columns([2, 1], Stack(), Heading())).Message, StringComparison.OrdinalIgnoreCase);

    // ---------------------------------------------------- ChangeSpec, same rules

    private static BlockDefined Defined() => new(Id, "Hero", Stack(Heading()));

    [Fact]
    public void ChangeSpec_valid_spec_raises_spec_changed()
    {
        var newSpec = Stack(Heading(), Heading());
        var outcome = AggregateSpec.For<BlockDefinition>()
            .Given(Defined())
            .When(b => b.ChangeSpec(newSpec));

        outcome.ThenRaised(new BlockSpecChanged(newSpec));
        Assert.Equal(newSpec, outcome.Aggregate.Spec);
    }

    [Fact]
    public void ChangeSpec_section_root_is_rejected() =>
        AggregateSpec.For<BlockDefinition>()
            .Given(Defined())
            .When(b => b.ChangeSpec(Section()))
            .ThenFails("section");

    [Fact]
    public void ChangeSpec_nested_block_instance_is_rejected() =>
        AggregateSpec.For<BlockDefinition>()
            .Given(Defined())
            .When(b => b.ChangeSpec(Stack(Instance())))
            .ThenFails("another block");

    [Fact]
    public void ChangeSpec_duplicate_node_ids_is_rejected()
    {
        var shared = NodeId.New();
        AggregateSpec.For<BlockDefinition>()
            .Given(Defined())
            .When(b => b.ChangeSpec(Stack(new DividerNode { Id = shared }, new DividerNode { Id = shared })))
            .ThenFails("own id");
    }

    [Fact]
    public void ChangeSpec_columns_cell_count_mismatch_is_rejected() =>
        AggregateSpec.For<BlockDefinition>()
            .Given(Defined())
            .When(b => b.ChangeSpec(Columns([2, 1], Stack())))
            .ThenFails("one cell per column");

    [Fact]
    public void ChangeSpec_spec_at_eight_levels_is_rejected() =>
        AggregateSpec.For<BlockDefinition>()
            .Given(Defined())
            .When(b => b.ChangeSpec(NestedStacks(8)))
            .ThenFails("levels deep");
}
