using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Features.Pages;
using Imprint.Authoring.Features.Pages.AddNode;
using Imprint.Authoring.Features.Pages.CreatePage;
using Imprint.Authoring.Projections;

namespace Imprint.Authoring.Tests.Features.Pages;

/// <summary>
/// The shared <see cref="SectionPresets"/> catalog and the editor's insert path for it
/// (build client-side, insert through the undoable AddNode slice — see PagesHost.AddPreset).
/// The old dedicated <c>AddPreset</c> command was retired as dead, non-undoable drift; these
/// pin the surviving behavior it used to cover.
/// </summary>
public sealed class SectionPresetsTests
{
    [Fact]
    public async Task Hero_preset_lands_a_full_section_in_the_draft()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var pageId = PageId.New();
        await host.Ok(new CreatePage(pageId, siteId, "Home", "home", "en"));

        await PagesHost.AddPreset(host, pageId, 0, "hero");

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
    public async Task Preset_text_lands_in_the_sites_default_locale()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host, "da", "en");
        var pageId = PageId.New();
        await host.Ok(new CreatePage(pageId, siteId, "Hjem", "hjem", "da"));

        await PagesHost.AddPreset(host, pageId, 0, "hero");

        var tree = host.Get<PageDrafts>().Get(pageId)!.Tree;
        var heading = Assert.Single(tree.All().OfType<HeadingNode>());
        Assert.True(heading.Text.Has(new Locale("da")));
        Assert.False(heading.Text.Has(new Locale("en")));
    }

    [Fact]
    public void Unknown_preset_key_resolves_to_nothing()
    {
        Assert.Null(SectionPresets.Find("mega-hero"));
        Assert.NotNull(SectionPresets.Find("hero"));
    }

    [Fact]
    public async Task Inserting_a_preset_section_at_an_out_of_range_index_is_rejected()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var pageId = PageId.New();
        await host.Ok(new CreatePage(pageId, siteId, "Home", "home", "en"));

        var section = SectionPresets.Find("hero")!.Build(new Locale("en"));
        var error = await host.Fails(new AddNode(pageId, NodeId.Root, 3, section));
        Assert.Contains("outside the valid range", error);
    }
}
