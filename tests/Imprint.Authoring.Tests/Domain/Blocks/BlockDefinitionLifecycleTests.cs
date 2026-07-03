using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Blocks;
using Imprint.Authoring.Domain.Blocks.Events;
using Imprint.Authoring.Domain.Pages;
using Imprint.EventSourcing;
using Imprint.TestKit;

namespace Imprint.Authoring.Tests.Domain.Blocks;

public sealed class BlockDefinitionLifecycleTests
{
    private static readonly BlockDefinitionId Id = BlockDefinitionId.New();
    private static readonly Locale En = new("en");

    private static Node SimpleSpec() => new StackNode
    {
        Id = NodeId.New(),
        Children = NodeList.Of(new HeadingNode { Id = NodeId.New(), Level = 2, Text = LocalizedText.Of(En, "Hello") }),
    };

    private static BlockDefined Defined(string name = "Hero") => new(Id, name, SimpleSpec());

    // ---------------------------------------------------------------- define

    [Fact]
    public void Define_valid_spec_raises_block_defined()
    {
        var spec = SimpleSpec();
        var definition = BlockDefinition.Define(Id, "Hero", spec);

        var raised = Assert.Single(definition.UncommittedEvents);
        Assert.Equal(new BlockDefined(Id, "Hero", spec), raised);
        Assert.Equal(Id, definition.Id);
        Assert.Equal("Hero", definition.Name);
        Assert.Equal(spec, definition.Spec);
        Assert.False(definition.IsDeleted);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Define_blank_name_is_rejected(string name)
    {
        var ex = Assert.Throws<DomainException>(() => BlockDefinition.Define(Id, name, SimpleSpec()));
        Assert.Contains("needs a name", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Define_name_over_100_characters_is_rejected()
    {
        var ex = Assert.Throws<DomainException>(() => BlockDefinition.Define(Id, new string('a', 101), SimpleSpec()));
        Assert.Contains("100", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Define_name_at_100_characters_is_accepted()
    {
        var definition = BlockDefinition.Define(Id, new string('a', 100), SimpleSpec());
        Assert.Single(definition.UncommittedEvents);
    }

    // ---------------------------------------------------------------- rename

    [Fact]
    public void Rename_changes_the_name()
    {
        var outcome = AggregateSpec.For<BlockDefinition>()
            .Given(Defined())
            .When(b => b.Rename("Hero banner"));

        outcome.ThenRaised(new BlockRenamed("Hero banner"));
        Assert.Equal("Hero banner", outcome.Aggregate.Name);
    }

    [Fact]
    public void Rename_to_unchanged_name_raises_nothing() =>
        AggregateSpec.For<BlockDefinition>()
            .Given(Defined())
            .When(b => b.Rename("Hero"))
            .ThenNothing();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_to_blank_is_rejected(string name) =>
        AggregateSpec.For<BlockDefinition>()
            .Given(Defined())
            .When(b => b.Rename(name))
            .ThenFails("needs a name");

    [Fact]
    public void Rename_over_100_characters_is_rejected() =>
        AggregateSpec.For<BlockDefinition>()
            .Given(Defined())
            .When(b => b.Rename(new string('a', 101)))
            .ThenFails("100");

    [Fact]
    public void Rename_on_deleted_block_is_rejected() =>
        AggregateSpec.For<BlockDefinition>()
            .Given(Defined(), new BlockDeleted())
            .When(b => b.Rename("New name"))
            .ThenFails("deleted");

    // ---------------------------------------------------------------- delete

    [Fact]
    public void Delete_marks_the_block_deleted()
    {
        var outcome = AggregateSpec.For<BlockDefinition>()
            .Given(Defined())
            .When(b => b.Delete());

        outcome.ThenRaised(new BlockDeleted());
        Assert.True(outcome.Aggregate.IsDeleted);
    }

    [Fact]
    public void Delete_twice_is_rejected() =>
        AggregateSpec.For<BlockDefinition>()
            .Given(Defined(), new BlockDeleted())
            .When(b => b.Delete())
            .ThenFails("deleted");

    [Fact]
    public void ChangeSpec_on_deleted_block_is_rejected() =>
        AggregateSpec.For<BlockDefinition>()
            .Given(Defined(), new BlockDeleted())
            .When(b => b.ChangeSpec(SimpleSpec()))
            .ThenFails("deleted");
}
