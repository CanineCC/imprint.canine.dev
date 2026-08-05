using Imprint.Authoring.Domain.Sites;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.SetLlmsExcludedPaths;

public sealed class SetLlmsExcludedPathsHandler(IAggregateStore store)
    : ICommandHandler<SetLlmsExcludedPaths>
{
    public async Task<Result> Handle(SetLlmsExcludedPaths cmd, CancellationToken ct)
    {
        var site = await store.Load<Site>(cmd.SiteId.Stream, ct);
        site.SetLlmsExcludedPaths(cmd.Paths);
        await store.Save(site, ct);
        return Result.Ok();
    }
}
