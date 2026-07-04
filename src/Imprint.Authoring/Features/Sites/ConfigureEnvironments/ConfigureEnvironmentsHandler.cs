using Imprint.Authoring.Domain.Sites;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.ConfigureEnvironments;

public sealed class ConfigureEnvironmentsHandler(IAggregateStore store) : ICommandHandler<ConfigureEnvironments>
{
    public async Task<Result> Handle(ConfigureEnvironments cmd, CancellationToken ct)
    {
        var site = await store.Load<Site>(cmd.SiteId.Stream, ct);
        site.SetEnvironments(cmd.Environments);
        await store.Save(site, ct);
        return Result.Ok();
    }
}
