using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Features.Pages.AddNode;
using Imprint.Authoring.Features.Pages.ChangeNodeProps;
using Imprint.Authoring.Projections;

namespace Imprint.Authoring.Tests.Features.Pages;

public sealed class ChangeNodePropsTests
{
    [Fact]
    public async Task ChangeNodeProps_updates_the_draft()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var (pageId, sectionId) = await PagesHost.SeedPageWithSection(host, siteId);

        await host.Ok(new ChangeNodeProps(pageId, new SectionNode { Id = sectionId, Width = SectionWidth.Full }));

        var section = (SectionNode)host.Get<PageDrafts>().Get(pageId)!.Tree.Find(sectionId)!;
        Assert.Equal(SectionWidth.Full, section.Width);
    }

    [Fact]
    public async Task ChangeNodeProps_cannot_change_the_node_type()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var (pageId, sectionId) = await PagesHost.SeedPageWithSection(host, siteId);

        var error = await host.Fails(new ChangeNodeProps(pageId, new StackNode { Id = sectionId }));
        Assert.Contains("cannot be changed into", error);
    }

    [Fact]
    public async Task ChangeNodeProps_widget_with_declared_props_is_accepted()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var (pageId, sectionId) = await PagesHost.SeedPageWithSection(host, siteId);
        var widget = new WidgetNode { Id = NodeId.New(), Tag = "x-countdown", Props = PropBag.Empty.With("to", "2026-12-31") };
        await host.Ok(new AddNode(pageId, sectionId, 0, widget));

        var changed = widget with { Props = widget.Props.With("label", "Launch day") };
        await host.Ok(new ChangeNodeProps(pageId, changed));

        Assert.Equal(changed, host.Get<PageDrafts>().Get(pageId)!.Tree.Find(widget.Id));
    }

    [Fact]
    public async Task ChangeNodeProps_widget_with_undeclared_prop_is_rejected()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var (pageId, sectionId) = await PagesHost.SeedPageWithSection(host, siteId);
        var widget = new WidgetNode { Id = NodeId.New(), Tag = "x-countdown", Props = PropBag.Empty.With("to", "2026-12-31") };
        await host.Ok(new AddNode(pageId, sectionId, 0, widget));

        var error = await host.Fails(new ChangeNodeProps(pageId, widget with { Props = widget.Props.With("theme", "dark") }));
        Assert.Equal("The 'x-countdown' widget has no 'theme' setting.", error);
    }
}
