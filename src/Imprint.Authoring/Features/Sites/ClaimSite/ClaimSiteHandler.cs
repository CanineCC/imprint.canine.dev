using Imprint.Authoring.Domain.Sites;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.ClaimSite;

public sealed class ClaimSiteHandler(IAggregateStore store) : ICommandHandler<ClaimSite>
{
    public async Task<Result> Handle(ClaimSite cmd, CancellationToken ct)
    {
        var site = await store.Load<Site>(cmd.SiteId.Stream, ct);
        site.ClaimOwnership();
        await store.Save(site, ct);
        return Result.Ok();
    }
}
