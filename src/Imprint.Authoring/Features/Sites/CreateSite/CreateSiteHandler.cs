using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Projections;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.CreateSite;

public sealed class CreateSiteHandler(IAggregateStore store) : ICommandHandler<CreateSite>
{
    public async Task<Result> Handle(CreateSite cmd, CancellationToken ct)
    {
        // Multi-site: an owner may create any number of sites. Ownership is the command's
        // actor (recorded on the site.created envelope and surfaced by SiteOverview), so
        // no cross-site guard is needed here — each site is its own stream.
        var site = Site.Create(cmd.SiteId, cmd.Name, new Locale(cmd.DefaultLocale));
        await store.Save(site, ct);
        return Result.Ok();
    }
}
