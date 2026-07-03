using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.ChangeDefaultLocale;

public sealed class ChangeDefaultLocaleHandler(IAggregateStore store) : ICommandHandler<ChangeDefaultLocale>
{
    public async Task<Result> Handle(ChangeDefaultLocale cmd, CancellationToken ct)
    {
        var site = await store.Load<Site>(cmd.SiteId.Stream, ct);
        site.ChangeDefaultLocale(new Locale(cmd.Locale));
        await store.Save(site, ct);
        return Result.Ok();
    }
}
