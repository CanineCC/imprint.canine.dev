using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Features.Pages.AddNode;
using Imprint.Authoring.Features.Pages.DuplicateNode;
using Imprint.Authoring.Features.Pages.MoveNode;
using Imprint.Authoring.Features.Pages.RemoveNode;
using Imprint.Authoring.Projections;

namespace Imprint.Authoring.Tests.Features.Pages;

public sealed class MoveRemoveDuplicateNodeTests
{
    [Fact]
    public async Task MoveNode_reorders_sections_in_the_draft()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var (pageId, firstSectionId) = await PagesHost.SeedPageWithSection(host, siteId);
        var second = new SectionNode { Id = NodeId.New() };
        await host.Ok(new AddNode(pageId, NodeId.Root, 1, second));

        await host.Ok(new MoveNode(pageId, second.Id, NodeId.Root, 0));

        var roots = host.Get<PageDrafts>().Get(pageId)!.Tree.Roots;
        Assert.Equal([second.Id, firstSectionId], roots.Select(node => node.Id));
    }

    [Fact]
    public async Task MoveNode_into_its_own_descendant_is_rejected()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var (pageId, sectionId) = await PagesHost.SeedPageWithSection(host, siteId);
        var outer = new StackNode { Id = NodeId.New(), Children = NodeList.Of(new StackNode { Id = NodeId.New() }) };
        await host.Ok(new AddNode(pageId, sectionId, 0, outer));

        var error = await host.Fails(new MoveNode(pageId, outer.Id, outer.Children[0].Id, 0));
        Assert.Contains("cannot be moved into itself or its own content", error);
    }

    [Fact]
    public async Task RemoveNode_removes_the_node_from_the_draft()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var (pageId, sectionId) = await PagesHost.SeedPageWithSection(host, siteId);
        var heading = new HeadingNode { Id = NodeId.New(), Text = LocalizedText.Of(new Locale("en"), "Hi") };
        await host.Ok(new AddNode(pageId, sectionId, 0, heading));

        await host.Ok(new RemoveNode(pageId, heading.Id));

        Assert.Null(host.Get<PageDrafts>().Get(pageId)!.Tree.Find(heading.Id));
    }

    [Fact]
    public async Task RemoveNode_of_an_unknown_node_is_rejected()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var (pageId, _) = await PagesHost.SeedPageWithSection(host, siteId);

        var error = await host.Fails(new RemoveNode(pageId, NodeId.New()));
        Assert.Contains("no longer exists on this page", error);
    }

    [Fact]
    public async Task DuplicateNode_inserts_a_copy_with_fresh_ids_after_the_source()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var (pageId, sectionId) = await PagesHost.SeedPageWithSection(host, siteId);
        var heading = new HeadingNode { Id = NodeId.New(), Text = LocalizedText.Of(new Locale("en"), "Hi") };
        await host.Ok(new AddNode(pageId, sectionId, 0, heading));

        await host.Ok(new DuplicateNode(pageId, heading.Id, NodeId.New()));

        var section = (SectionNode)host.Get<PageDrafts>().Get(pageId)!.Tree.Find(sectionId)!;
        Assert.Equal(2, section.Children.Count);
        var copy = Assert.IsType<HeadingNode>(section.Children[1]);
        Assert.NotEqual(heading.Id, copy.Id);
        Assert.Equal(heading.Text, copy.Text);
    }

    [Fact]
    public async Task DuplicateNode_of_a_managed_column_cell_is_rejected()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var (pageId, sectionId) = await PagesHost.SeedPageWithSection(host, siteId);
        var columns = new ColumnsNode
        {
            Id = NodeId.New(),
            Ratios = [1, 1],
            Children = NodeList.Of(new StackNode { Id = NodeId.New() }, new StackNode { Id = NodeId.New() }),
        };
        await host.Ok(new AddNode(pageId, sectionId, 0, columns));

        var error = await host.Fails(new DuplicateNode(pageId, columns.Children[0].Id, NodeId.New()));
        Assert.Contains("Column cells cannot be duplicated", error);
    }
}
