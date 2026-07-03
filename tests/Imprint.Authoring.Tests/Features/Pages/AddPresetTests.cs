using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Features.Pages.AddPreset;
using Imprint.Authoring.Features.Pages.CreatePage;
using Imprint.Authoring.Projections;

namespace Imprint.Authoring.Tests.Features.Pages;

public sealed class AddPresetTests
{
    [Fact]
    public async Task AddPreset_hero_lands_a_full_section_in_the_draft()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var pageId = PageId.New();
        await host.Ok(new CreatePage(pageId, siteId, "Home", "home", "en"));

        await host.Ok(new AddPreset(pageId, 0, "hero"));

        var tree = host.Get<PageDrafts>().Get(pageId)!.Tree;
        var section = Assert.IsType<SectionNode>(Assert.Single(tree.Roots));
        Assert.Equal(SectionPadding.Large, section.Padding);

        var stack = Assert.IsType<StackNode>(Assert.Single(section.Children));
        Assert.Equal(3, stack.Children.Count);
        var heading = Assert.IsType<HeadingNode>(stack.Children[0]);
        Assert.Equal(1, heading.Level);
        Assert.Equal("Make something people remember", heading.Text.Get(new Locale("en")));
        Assert.IsType<RichTextNode>(stack.Children[1]);
        Assert.IsType<ButtonNode>(stack.Children[2]);
    }

    [Fact]
    public async Task AddPreset_text_lands_in_the_sites_default_locale()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host, "da", "en");
        var pageId = PageId.New();
        await host.Ok(new CreatePage(pageId, siteId, "Hjem", "hjem", "da"));

        await host.Ok(new AddPreset(pageId, 0, "hero"));

        var tree = host.Get<PageDrafts>().Get(pageId)!.Tree;
        var heading = Assert.Single(tree.All().OfType<HeadingNode>());
        Assert.True(heading.Text.Has(new Locale("da")));
        Assert.False(heading.Text.Has(new Locale("en")));
    }

    [Fact]
    public async Task AddPreset_with_unknown_key_is_rejected()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var pageId = PageId.New();
        await host.Ok(new CreatePage(pageId, siteId, "Home", "home", "en"));

        var error = await host.Fails(new AddPreset(pageId, 0, "mega-hero"));
        Assert.Equal("There is no 'mega-hero' section preset.", error);
    }

    [Fact]
    public async Task AddPreset_at_an_out_of_range_index_is_rejected()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var pageId = PageId.New();
        await host.Ok(new CreatePage(pageId, siteId, "Home", "home", "en"));

        var error = await host.Fails(new AddPreset(pageId, 3, "hero"));
        Assert.Contains("outside the valid range", error);
    }
}
