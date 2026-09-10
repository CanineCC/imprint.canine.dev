using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Syndication;

namespace Imprint.Authoring.Tests.Syndication;

/// <summary>
/// The content hash of a syndicated page, at the level the defect actually lived: what the hash is
/// allowed to see. A producer pushes CONTENT and this side mints the node ids, so a hash that reads
/// them is a hash of when the push happened rather than of what the page says — it can never match
/// itself, the push endpoint can never report a no-op, and the publisher (which reads this hash as a
/// syndicated page's version) finds every page stale on every sweep.
/// </summary>
public sealed class SyndicatedContentHashTests
{
    private static readonly Locale En = new("en");

    private static Node APage(string heading, string body = "<p>A document database and event store.</p>") =>
        new SectionNode
        {
            Id = NodeId.New(),
            Children = NodeList.Of([
                new HeadingNode { Id = NodeId.New(), Level = 2, Text = LocalizedText.Of(En, heading) },
                new RichTextNode { Id = NodeId.New(), Html = LocalizedText.Of(En, body) },
                new ColumnsNode
                {
                    Id = NodeId.New(),
                    Ratios = [2, 1],
                    Children = NodeList.Of([
                        new StackNode { Id = NodeId.New() },
                        new StackNode { Id = NodeId.New() },
                    ]),
                },
            ]),
        };

    private static string Hash(Node node, string title = "JasperFx/marten") =>
        SyndicatedPageStore.HashOf(
            LocalizedText.Of(En, title), LocalizedText.Of(En, title), LocalizedText.Of(En, "One project."), node);

    [Fact]
    public void Two_trees_that_differ_only_in_node_ids_hash_the_same()
    {
        // Every id here is freshly minted, at every depth — including the implicit column cells, which are
        // the second place the wire parser mints them.
        Assert.NotEqual(APage("JasperFx/marten"), APage("JasperFx/marten"));   // different trees, by identity
        Assert.Equal(Hash(APage("JasperFx/marten")), Hash(APage("JasperFx/marten")));
    }

    [Fact]
    public void A_tree_whose_content_differs_hashes_differently()
    {
        // The other half: identity is excluded, everything a reader sees is not.
        Assert.NotEqual(Hash(APage("JasperFx/marten")), Hash(APage("JasperFx/wolverine")));
        Assert.NotEqual(
            Hash(APage("JasperFx/marten")),
            Hash(APage("JasperFx/marten", "<p>Now it says something else entirely.</p>")));
        Assert.NotEqual(Hash(APage("JasperFx/marten")), Hash(APage("JasperFx/marten"), title: "Marten"));
    }

    [Fact]
    public void Structure_is_content_even_when_every_node_is_empty()
    {
        // Blanking ids must not blank the SHAPE: two dividers are not one divider, and a stack of two is
        // not a stack of three. An id-blind hash that collapsed those would be worse than the churning one,
        // because it would go quiet about a change that is real.
        var two = new SectionNode
        {
            Id = NodeId.New(),
            Children = NodeList.Of([new DividerNode { Id = NodeId.New() }, new DividerNode { Id = NodeId.New() }]),
        };
        var three = new SectionNode
        {
            Id = NodeId.New(),
            Children = NodeList.Of([
                new DividerNode { Id = NodeId.New() },
                new DividerNode { Id = NodeId.New() },
                new DividerNode { Id = NodeId.New() },
            ]),
        };

        Assert.NotEqual(Hash(two), Hash(three));
    }

    [Fact]
    public void A_block_instances_override_keys_are_content_and_stay_in_the_hash()
    {
        // The one kind of node id that is NOT this page's to mint: an override is keyed by the DEFINITION's
        // node id, so which node it lands on is part of what the page says. Blanking those would hide a real
        // difference. (Nothing syndicates block instances today — the wire parser has no 'block' type — but
        // the hash is over the node union, and a rule that only holds for the types in use is not a rule.)
        var definitionNode = NodeId.New();
        var elsewhere = NodeId.New();
        var definition = BlockDefinitionId.New();

        Node Instance(NodeId target) => new BlockInstanceNode
        {
            Id = NodeId.New(),
            DefinitionId = definition,
            Overrides = OverrideSet.Empty.With(target, "text", En, "Overridden"),
        };

        Assert.NotEqual(Hash(Instance(definitionNode)), Hash(Instance(elsewhere)));
    }
}
