using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Blocks;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Features.Pages;
using Imprint.EventSourcing;
using Microsoft.Extensions.DependencyInjection;

namespace Imprint.Authoring.Tests.Features.Pages;

/// <summary>
/// Shared arrangement for Pages slice tests. Sites and block definitions belong to
/// other feature areas whose slices don't exist yet, so tests seed those aggregates
/// directly through the store and catch projections up by hand — the very same events
/// the future slices will produce, minus the command shell.
/// </summary>
internal static class PagesHost
{
    /// <summary>A host whose widget manifest declares <c>x-countdown(to, label)</c>.</summary>
    public static AuthoringTestHost Create() =>
        new(services => services.AddSingleton<IWidgetCatalog>(
            new FakeWidgetCatalog().Declare("x-countdown", "to", "label")));

    /// <summary>Seeds a site; the first locale is the default (defaults to "en").</summary>
    public static async Task<SiteId> SeedSite(AuthoringTestHost host, params string[] locales)
    {
        var id = SiteId.New();
        var site = Site.Create(id, "Test site", new Locale(locales.Length == 0 ? "en" : locales[0]));
        foreach (var extra in locales.Skip(1))
        {
            site.AddLocale(new Locale(extra));
        }

        await SaveAndCatchUp(host, site);
        return id;
    }

    public static async Task SetNavigation(AuthoringTestHost host, SiteId siteId, params PageId[] pages)
    {
        var site = await host.Get<IAggregateStore>().Load<Site>(siteId.Stream);
        site.SetNavigation([.. pages.Select(page => new NavigationItem(page, null))]);
        await SaveAndCatchUp(host, site);
    }

    public static async Task<BlockDefinition> SeedBlock(AuthoringTestHost host, string name, Node spec)
    {
        var definition = BlockDefinition.Define(BlockDefinitionId.New(), name, spec);
        await SaveAndCatchUp(host, definition);
        return definition;
    }

    /// <summary>Creates a page (default-locale "en" site required) with one empty root section.</summary>
    public static async Task<(PageId PageId, NodeId SectionId)> SeedPageWithSection(
        AuthoringTestHost host, SiteId siteId, string slug = "about", string title = "About")
    {
        var pageId = PageId.New();
        await host.Ok(new Authoring.Features.Pages.CreatePage.CreatePage(pageId, siteId, title, slug, "en"));
        var section = new SectionNode { Id = NodeId.New() };
        await host.Ok(new Authoring.Features.Pages.AddNode.AddNode(pageId, NodeId.Root, 0, section));
        return (pageId, section.Id);
    }

    private static async Task SaveAndCatchUp(AuthoringTestHost host, AggregateRoot aggregate)
    {
        await host.Get<IAggregateStore>().Save(aggregate);
        // Seeding bypasses the dispatcher, so projections are caught up by hand.
        await host.Get<ProjectionEngine>().CatchUp();
    }
}
