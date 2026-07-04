using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Sites;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.CreateSiteFromTemplate;

/// <summary>
/// The seed-stream pattern: a template is executed as ordinary domain behaviors across
/// ordinary streams, so a templated site's history reads exactly like a hand-built one.
/// </summary>
public sealed class CreateSiteFromTemplateHandler(IAggregateStore store)
    : ICommandHandler<CreateSiteFromTemplate>
{
    public async Task<Result> Handle(CreateSiteFromTemplate cmd, CancellationToken ct)
    {
        if (SiteTemplates.Find(cmd.TemplateKey) is not { } template)
        {
            return Result.Fail(
                $"'{cmd.TemplateKey}' is not a site template. " +
                $"Available templates: {string.Join(", ", SiteTemplates.All.Select(t => t.Key))}.");
        }

        // Multi-site: an owner may create any number of sites (ownership is the actor on
        // the site.created envelope). No cross-site guard — each site is its own stream.
        // One logical onboarding operation across N streams (site + one per page),
        // deliberately built against IAggregateStore rather than nested commands.
        // It is NOT atomic: a mid-way failure leaves a visible, fixable partial site
        // (some pages missing, nav not set). Compensations would be ceremony for a
        // first-run flow the user simply re-runs or finishes by hand.
        var defaultLocale = new Locale(cmd.DefaultLocale);
        var site = Site.Create(cmd.SiteId, cmd.Name, defaultLocale);
        foreach (var extra in cmd.ExtraLocales)
        {
            site.AddLocale(new Locale(extra));
        }

        await store.Save(site, ct);

        var navigation = new List<NavigationItem>();
        foreach (var templatePage in template.Pages)
        {
            // Template slugs are already in Slug.Suggest form ('home', 'about', …);
            // a template that ships an invalid slug is a programmer error, not user input.
            if (!Slug.TryCreate(templatePage.Slug, out var slug, out var slugError))
            {
                throw new InvalidOperationException(
                    $"Template '{template.Key}' carries invalid slug '{templatePage.Slug}': {slugError}");
            }

            var page = Page.Create(PageId.New(), cmd.SiteId, slug, defaultLocale, templatePage.Title);
            var sections = templatePage.BuildSections(defaultLocale);
            for (var index = 0; index < sections.Count; index++)
            {
                page.AddNode(NodeId.Root, index, sections[index]);
            }

            await store.Save(page, ct);

            if (templatePage.InNavigation)
            {
                // Template order is navigation order; labels fall back to page titles.
                navigation.Add(NavigationItem.Page(page.Id));
            }
        }

        if (navigation.Count > 0)
        {
            site.SetNavigation(navigation);
            await store.Save(site, ct);
        }

        return Result.Ok();
    }
}
