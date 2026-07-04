using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Features.Sites.CreateSite;
using Imprint.Authoring.Features.Sites.CreateSiteFromTemplate;
using Imprint.Authoring.Projections;

namespace Imprint.Authoring.Tests.Features.Sites;

public sealed class CreateSiteFromTemplateTests
{
    private static readonly Locale En = new("en");

    [Theory]
    [InlineData("launch")]
    [InlineData("blank")]
    public async Task Template_creates_site_pages_and_navigation_end_to_end(string templateKey)
    {
        await using var host = new AuthoringTestHost();
        var siteId = SiteId.New();
        var template = SiteTemplates.Find(templateKey)!;

        await host.Ok(new CreateSiteFromTemplate(siteId, "Fresh site", "en", templateKey, []));

        // Site: created with the requested identity.
        var site = host.Get<SiteOverview>().Current;
        Assert.NotNull(site);
        Assert.Equal(siteId, site.Id);
        Assert.Equal("Fresh site", site.Name);
        Assert.Equal(En, site.DefaultLocale);

        // Pages: one per template page, listed under the template's slugs and titles.
        var pageList = host.Get<PageList>();
        Assert.Equal(template.Pages.Count, pageList.All().Count);

        var drafts = host.Get<PageDrafts>();
        foreach (var templatePage in template.Pages)
        {
            var summary = Assert.Single(pageList.All(), p => p.Slug.Value == templatePage.Slug);
            Assert.Equal(templatePage.Title, summary.Title.Get(En));

            // The draft tree matches the template's sections structurally. Ids cannot
            // match (both sides mint fresh ones), so both trees are compared with ids
            // stripped — everything else (types, props, text) must be identical.
            var expected = templatePage.BuildSections(En).Select(Anonymize).ToList();
            var actual = drafts.Get(summary.Id)!.Tree.Roots.Select(Anonymize).ToList();
            Assert.Equal(expected.Count, actual.Count);
            Assert.Equal(expected, actual);
        }

        // Navigation: the InNavigation pages, in template order, labels falling back
        // to page titles.
        var expectedNavSlugs = template.Pages.Where(p => p.InNavigation).Select(p => p.Slug).ToList();
        var actualNavSlugs = site.Navigation
            .Select(item => pageList.Get(item.PageId!.Value)!.Slug.Value)
            .ToList();
        Assert.Equal(expectedNavSlugs, actualNavSlugs);
        Assert.All(site.Navigation, item => Assert.Null(item.Label));
    }

    [Fact]
    public async Task Launch_template_home_page_is_the_nav_first_home()
    {
        await using var host = new AuthoringTestHost();
        await host.Ok(new CreateSiteFromTemplate(SiteId.New(), "Fresh site", "en", "launch", []));

        var home = host.Get<PageList>().Home;
        Assert.NotNull(home);
        Assert.Equal("home", home.Slug.Value);
    }

    [Fact]
    public async Task Extra_locales_are_added_after_the_default()
    {
        await using var host = new AuthoringTestHost();

        await host.Ok(new CreateSiteFromTemplate(SiteId.New(), "Fresh site", "en", "blank", ["da", "de-AT"]));

        var site = host.Get<SiteOverview>().Current!;
        Assert.Equal([En, new Locale("da"), new Locale("de-AT")], site.Locales);
        Assert.Equal(En, site.DefaultLocale);
    }

    [Fact]
    public async Task Unknown_template_key_is_rejected_and_creates_nothing()
    {
        await using var host = new AuthoringTestHost();

        var error = await host.Fails(new CreateSiteFromTemplate(SiteId.New(), "Fresh site", "en", "brochure", []));

        Assert.Contains("not a site template", error);
        Assert.Null(host.Get<SiteOverview>().Current);
        Assert.Empty(host.Get<PageList>().All());
    }

    [Fact]
    public async Task A_second_site_can_be_created_alongside_the_first()
    {
        // Multi-site: an owner may create many sites. Both coexist; the first stays Current
        // for the single-site call sites still being migrated.
        await using var host = new AuthoringTestHost();
        var first = SiteId.New();
        await host.Ok(new CreateSite(first, "First", "en"));

        await host.Ok(new CreateSiteFromTemplate(SiteId.New(), "Second", "en", "blank", []));

        Assert.Equal(2, host.Get<SiteOverview>().All.Count);
        Assert.Equal(first, host.Get<SiteOverview>().Current?.Id);
    }

    [Fact]
    public async Task Invalid_extra_locale_fails_validation_before_anything_is_created()
    {
        await using var host = new AuthoringTestHost();

        var error = await host.Fails(new CreateSiteFromTemplate(SiteId.New(), "Fresh site", "en", "blank", ["nope!"]));

        Assert.Contains("not a valid locale tag", error);
        Assert.Null(host.Get<SiteOverview>().Current);
    }

    [Fact]
    public async Task Duplicate_extra_locale_is_rejected_by_the_aggregate_before_any_save()
    {
        await using var host = new AuthoringTestHost();

        var error = await host.Fails(new CreateSiteFromTemplate(SiteId.New(), "Fresh site", "en", "blank", ["en"]));

        Assert.Contains("already on this site", error);
        // The duplicate is caught while the site is still in memory — no partial site.
        Assert.Null(host.Get<SiteOverview>().Current);
        Assert.Empty(host.Get<PageList>().All());
    }

    private static Node Anonymize(Node node)
    {
        var stripped = node switch
        {
            IContainerNode container => (Node)container.WithChildren(
                NodeList.Of([.. container.Children.Select(Anonymize)])),
            _ => node,
        };
        return stripped with { Id = default };
    }
}
