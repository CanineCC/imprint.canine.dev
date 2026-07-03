using Imprint.Authoring.Domain.Sites;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.RenameSite;

public sealed class RenameSiteHandler(IAggregateStore store) : ICommandHandler<RenameSite>
{
    public async Task<Result> Handle(RenameSite cmd, CancellationToken ct)
    {
        var site = await store.Load<Site>(cmd.SiteId.Stream, ct);
        site.Rename(cmd.Name);
        await store.Save(site, ct);
        return Result.Ok();
    }
}
