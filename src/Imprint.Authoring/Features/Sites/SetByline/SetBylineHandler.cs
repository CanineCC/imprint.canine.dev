using Imprint.Authoring.Domain.Sites;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.SetByline;

public sealed class SetBylineHandler(IAggregateStore store) : ICommandHandler<SetByline>
{
    public async Task<Result> Handle(SetByline cmd, CancellationToken ct)
    {
        var site = await store.Load<Site>(cmd.SiteId.Stream, ct);
        site.SetByline(cmd.Byline);
        await store.Save(site, ct);
        return Result.Ok();
    }
}
