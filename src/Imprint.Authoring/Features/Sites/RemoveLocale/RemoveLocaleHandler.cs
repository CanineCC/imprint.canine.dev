using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.RemoveLocale;

public sealed class RemoveLocaleHandler(IAggregateStore store) : ICommandHandler<RemoveLocale>
{
    public async Task<Result> Handle(RemoveLocale cmd, CancellationToken ct)
    {
        var site = await store.Load<Site>(cmd.SiteId.Stream, ct);
        site.RemoveLocale(new Locale(cmd.Locale));
        await store.Save(site, ct);
        return Result.Ok();
    }
}
