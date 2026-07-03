using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Blocks;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Features.Pages.AddNode;
using Imprint.Authoring.Features.Pages.SetBlockOverride;
using Imprint.Authoring.Projections;

namespace Imprint.Authoring.Tests.Features.Pages;

public sealed class SetBlockOverrideTests
{
    private static readonly Locale En = new("en");

    private static Node BlockSpec(out HeadingNode heading, out RichTextNode text)
    {
        heading = new HeadingNode { Id = NodeId.New(), Text = LocalizedText.Of(En, "Block heading") };
        text = new RichTextNode { Id = NodeId.New(), Html = LocalizedText.Of(En, "<p>Block copy</p>") };
        return new StackNode { Id = NodeId.New(), Children = NodeList.Of(heading, text) };
    }

    private static async Task<(PageId PageId, BlockInstanceNode Instance)> PlaceInstance(
        AuthoringTestHost host, SiteId siteId, BlockDefinitionId definitionId)
    {
        var (pageId, sectionId) = await PagesHost.SeedPageWithSection(host, siteId);
        var instance = new BlockInstanceNode { Id = NodeId.New(), DefinitionId = definitionId };
        await host.Ok(new AddNode(pageId, sectionId, 0, instance));
        return (pageId, instance);
    }

    [Fact]
    public async Task SetBlockOverride_stores_the_override_in_the_draft()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var definition = await PagesHost.SeedBlock(host, "Card", BlockSpec(out var heading, out _));
        var (pageId, instance) = await PlaceInstance(host, siteId, definition.Id);

        await host.Ok(new SetBlockOverride(pageId, instance.Id, heading.Id, "text", "en", "My own words"));

        var updated = (BlockInstanceNode)host.Get<PageDrafts>().Get(pageId)!.Tree.Find(instance.Id)!;
        Assert.Equal("My own words", updated.Overrides.Get(heading.Id, "text", En));
    }

    [Fact]
    public async Task SetBlockOverride_with_null_value_clears_the_override()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var definition = await PagesHost.SeedBlock(host, "Card", BlockSpec(out var heading, out _));
        var (pageId, instance) = await PlaceInstance(host, siteId, definition.Id);
        await host.Ok(new SetBlockOverride(pageId, instance.Id, heading.Id, "text", "en", "My own words"));

        await host.Ok(new SetBlockOverride(pageId, instance.Id, heading.Id, "text", "en", null));

        var updated = (BlockInstanceNode)host.Get<PageDrafts>().Get(pageId)!.Tree.Find(instance.Id)!;
        Assert.Null(updated.Overrides.Get(heading.Id, "text", En));
    }

    [Fact]
    public async Task SetBlockOverride_for_a_deleted_definition_is_rejected()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        // The instance points at a definition the BlockLibrary has never seen —
        // indistinguishable from one deleted a moment ago.
        var (pageId, instance) = await PlaceInstance(host, siteId, BlockDefinitionId.New());

        var error = await host.Fails(new SetBlockOverride(pageId, instance.Id, NodeId.New(), "text", "en", "Hi"));
        Assert.Contains("definition no longer exists", error);
    }

    [Fact]
    public async Task SetBlockOverride_for_a_node_not_in_the_definition_is_rejected()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var definition = await PagesHost.SeedBlock(host, "Card", BlockSpec(out _, out _));
        var (pageId, instance) = await PlaceInstance(host, siteId, definition.Id);

        var error = await host.Fails(new SetBlockOverride(pageId, instance.Id, NodeId.New(), "text", "en", "Hi"));
        Assert.Contains("no longer part of the block", error);
    }

    [Fact]
    public async Task SetBlockOverride_for_a_field_the_target_node_does_not_carry_is_rejected()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var definition = await PagesHost.SeedBlock(host, "Card", BlockSpec(out var heading, out _));
        var (pageId, instance) = await PlaceInstance(host, siteId, definition.Id);

        var error = await host.Fails(new SetBlockOverride(pageId, instance.Id, heading.Id, "label", "en", "Click"));
        Assert.Contains("has no editable 'label' text", error);
    }

    [Fact]
    public async Task SetBlockOverride_on_a_missing_instance_surfaces_the_aggregate_error()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        await PagesHost.SeedBlock(host, "Card", BlockSpec(out var heading, out _));
        var (pageId, _) = await PagesHost.SeedPageWithSection(host, siteId);

        var error = await host.Fails(new SetBlockOverride(pageId, NodeId.New(), heading.Id, "text", "en", "Hi"));
        Assert.Contains("block instance no longer exists", error);
    }
}
