using Imprint.Authoring.Domain.Sites;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.SetLlmsPreamble;

public sealed class SetLlmsPreambleHandler(IAggregateStore store)
    : ICommandHandler<SetLlmsPreamble>
{
    public async Task<Result> Handle(SetLlmsPreamble cmd, CancellationToken ct)
    {
        var site = await store.Load<Site>(cmd.SiteId.Stream, ct);
        site.SetLlmsPreamble(cmd.Preamble);
        await store.Save(site, ct);
        return Result.Ok();
    }
}
