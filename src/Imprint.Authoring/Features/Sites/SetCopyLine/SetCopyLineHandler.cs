using Imprint.Authoring.Domain.Sites;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.SetCopyLine;

public sealed class SetCopyLineHandler(IAggregateStore store) : ICommandHandler<SetCopyLine>
{
    public async Task<Result> Handle(SetCopyLine cmd, CancellationToken ct)
    {
        var site = await store.Load<Site>(cmd.SiteId.Stream, ct);
        site.SetCopyLine(cmd.CopyLine);
        await store.Save(site, ct);
        return Result.Ok();
    }
}
