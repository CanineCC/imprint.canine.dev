using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Projections;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Pages.CreatePage;

public sealed class CreatePageHandler(IAggregateStore store, SiteOverview sites, PageList pageList)
    : ICommandHandler<CreatePage>
{
    public async Task<Result> Handle(CreatePage cmd, CancellationToken ct)
    {
        _ = Slug.TryCreate(cmd.Slug, out var slug, out _); // shape guaranteed by Validate()
        var locale = new Locale(cmd.Locale);

        // Cross-aggregate check via the SiteOverview read model. Accepted race: the
        // locale could be removed from the site in this same instant — the stray
        // translation is then simply never rendered (docs/architecture.md §Consistency).
        var site = sites.Get(cmd.SiteId);
        if (site is null)
        {
            return Result.Fail("The site no longer exists.");
        }

        if (!site.Locales.Contains(locale))
        {
            return Result.Fail($"'{locale}' is not one of this site's locales.");
        }

        // Cross-aggregate uniqueness via the PageList read model. Accepted race: two
        // editors can claim the same slug in the same instant and both pass — the
        // publisher detects the collision and fails that page's render with a visible
        // error (docs/publishing.md), so the site never silently serves the wrong page.
        if (pageList.SlugTaken(slug))
        {
            return Result.Fail($"The slug '{slug}' is already used by another page.");
        }

        var page = Page.Create(cmd.PageId, cmd.SiteId, slug, locale, cmd.Title);
        await store.Save(page, ct);
        return Result.Ok();
    }
}
