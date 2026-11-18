using Imprint.Authoring.Domain.Sites;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.SetRobotsDisallowedPaths;

public sealed class SetRobotsDisallowedPathsHandler(IAggregateStore store)
    : ICommandHandler<SetRobotsDisallowedPaths>
{
    public async Task<Result> Handle(SetRobotsDisallowedPaths cmd, CancellationToken ct)
    {
        var site = await store.Load<Site>(cmd.SiteId.Stream, ct);
        site.SetRobotsDisallowedPaths(cmd.Paths);
        await store.Save(site, ct);
        return Result.Ok();
    }
}
