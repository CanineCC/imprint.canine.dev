using Imprint.Authoring.Domain.Sites;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.ArchiveSite;

public sealed class ArchiveSiteHandler(IAggregateStore store) : ICommandHandler<ArchiveSite>, ICommandHandler<RestoreSite>
{
    public async Task<Result> Handle(ArchiveSite cmd, CancellationToken ct)
    {
        var site = await store.Load<Site>(cmd.SiteId.Stream, ct);
        site.Archive();
        await store.Save(site, ct);
        return Result.Ok();
    }

    public async Task<Result> Handle(RestoreSite cmd, CancellationToken ct)
    {
        var site = await store.Load<Site>(cmd.SiteId.Stream, ct);
        site.Restore();
        await store.Save(site, ct);
        return Result.Ok();
    }
}
