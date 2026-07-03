using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Projections;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Pages.ChangePageTitle;

public sealed class ChangePageTitleHandler(IAggregateStore store, SiteOverview sites)
    : ICommandHandler<ChangePageTitle>
{
    public async Task<Result> Handle(ChangePageTitle cmd, CancellationToken ct)
    {
        var locale = new Locale(cmd.Locale);
        var page = await store.Load<Page>(cmd.PageId.Stream, ct);

        // Cross-aggregate check via the SiteOverview read model. Accepted race: a
        // locale removed in this same instant slips through — the render simply
        // ignores unknown locales (docs/architecture.md §Consistency).
        var site = sites.Get(page.SiteId);
        if (site is null || !site.Locales.Contains(locale))
        {
            return Result.Fail($"'{locale}' is not one of this site's locales.");
        }

        page.ChangeTitle(locale, cmd.Title);
        await store.Save(page, ct);
        return Result.Ok();
    }
}
