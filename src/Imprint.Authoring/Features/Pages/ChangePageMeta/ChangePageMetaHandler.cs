using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Projections;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Pages.ChangePageMeta;

public sealed class ChangePageMetaHandler(IAggregateStore store, SiteOverview sites)
    : ICommandHandler<ChangePageMeta>
{
    public async Task<Result> Handle(ChangePageMeta cmd, CancellationToken ct)
    {
        var locale = new Locale(cmd.Locale);
        var page = await store.Load<Page>(cmd.PageId.Stream, ct);

        // Cross-aggregate check via the SiteOverview read model — same rule as every
        // locale-valued edit. Accepted race: a locale removed in this same instant
        // slips through, and the render ignores unknown locales.
        var site = sites.Get(page.SiteId);
        if (site is null || !site.Locales.Contains(locale))
        {
            return Result.Fail($"'{locale}' is not one of this site's locales.");
        }

        page.ChangeMeta(locale, cmd.MetaTitle, cmd.MetaDescription);
        await store.Save(page, ct);
        return Result.Ok();
    }
}
