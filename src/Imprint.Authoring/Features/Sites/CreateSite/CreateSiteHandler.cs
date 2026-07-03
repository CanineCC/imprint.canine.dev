using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Projections;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.CreateSite;

public sealed class CreateSiteHandler(IAggregateStore store, SiteOverview overview) : ICommandHandler<CreateSite>
{
    public async Task<Result> Handle(CreateSite cmd, CancellationToken ct)
    {
        // Single-site UX over a multi-site domain: the domain happily models many
        // sites, but this editor manages exactly one, so the slice — not the aggregate
        // — enforces it via the SiteOverview read model. Accepted race: two first-run
        // wizards submitting in the same instant could both pass; the second site is
        // simply never Current and never rendered — visible in history, harmless.
        if (overview.Current is not null)
        {
            return Result.Fail("This installation already has a site.");
        }

        var site = Site.Create(cmd.SiteId, cmd.Name, new Locale(cmd.DefaultLocale));
        await store.Save(site, ct);
        return Result.Ok();
    }
}
