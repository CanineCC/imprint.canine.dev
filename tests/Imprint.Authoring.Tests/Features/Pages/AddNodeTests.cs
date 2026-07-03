using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Features.Pages.AddNode;
using Imprint.Authoring.Projections;

namespace Imprint.Authoring.Tests.Features.Pages;

public sealed class AddNodeTests
{
    [Fact]
    public async Task AddNode_section_at_root_lands_in_the_draft()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var (pageId, _) = await PagesHost.SeedPageWithSection(host, siteId);

        var section = new SectionNode { Id = NodeId.New(), Width = SectionWidth.Wide };
        await host.Ok(new AddNode(pageId, NodeId.Root, 1, section));

        var tree = host.Get<PageDrafts>().Get(pageId)!.Tree;
        Assert.Equal(2, tree.Roots.Count);
        Assert.Equal(section, tree.Roots[1]);
    }

    [Fact]
    public async Task AddNode_stack_at_root_surfaces_the_placement_error()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var (pageId, _) = await PagesHost.SeedPageWithSection(host, siteId);

        var error = await host.Fails(new AddNode(pageId, NodeId.Root, 0, new StackNode { Id = NodeId.New() }));
        Assert.Contains("Only sections can be placed directly on the page", error);
    }

    [Fact]
    public async Task AddNode_widget_with_declared_props_is_accepted()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var (pageId, sectionId) = await PagesHost.SeedPageWithSection(host, siteId);

        var widget = new WidgetNode
        {
            Id = NodeId.New(),
            Tag = "x-countdown",
            Props = PropBag.Empty.With("to", "2026-12-31").With("label", "Launch"),
        };
        await host.Ok(new AddNode(pageId, sectionId, 0, widget));

        Assert.Equal(widget, host.Get<PageDrafts>().Get(pageId)!.Tree.Find(widget.Id));
    }

    [Fact]
    public async Task AddNode_widget_with_unknown_tag_is_rejected()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var (pageId, sectionId) = await PagesHost.SeedPageWithSection(host, siteId);

        var widget = new WidgetNode { Id = NodeId.New(), Tag = "x-carousel" };
        var error = await host.Fails(new AddNode(pageId, sectionId, 0, widget));
        Assert.Equal("There is no 'x-carousel' widget installed.", error);
    }

    [Fact]
    public async Task AddNode_widget_with_undeclared_prop_is_rejected()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var (pageId, sectionId) = await PagesHost.SeedPageWithSection(host, siteId);

        var widget = new WidgetNode
        {
            Id = NodeId.New(),
            Tag = "x-countdown",
            Props = PropBag.Empty.With("speed", "fast"),
        };
        var error = await host.Fails(new AddNode(pageId, sectionId, 0, widget));
        Assert.Equal("The 'x-countdown' widget has no 'speed' setting.", error);
    }

    [Fact]
    public async Task AddNode_checks_widgets_nested_anywhere_in_the_spec()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var (pageId, _) = await PagesHost.SeedPageWithSection(host, siteId);

        var spec = new SectionNode
        {
            Id = NodeId.New(),
            Children = NodeList.Of(new StackNode
            {
                Id = NodeId.New(),
                Children = NodeList.Of(new WidgetNode { Id = NodeId.New(), Tag = "x-carousel" }),
            }),
        };
        var error = await host.Fails(new AddNode(pageId, NodeId.Root, 0, spec));
        Assert.Contains("x-carousel", error);
    }

    [Fact]
    public async Task AddNode_at_an_out_of_range_index_is_rejected()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var (pageId, _) = await PagesHost.SeedPageWithSection(host, siteId);

        var error = await host.Fails(new AddNode(pageId, NodeId.Root, 5, new SectionNode { Id = NodeId.New() }));
        Assert.Contains("outside the valid range", error);
    }
}
