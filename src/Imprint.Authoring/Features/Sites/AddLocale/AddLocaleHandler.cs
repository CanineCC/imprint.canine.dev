using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.AddLocale;

public sealed class AddLocaleHandler(IAggregateStore store) : ICommandHandler<AddLocale>
{
    public async Task<Result> Handle(AddLocale cmd, CancellationToken ct)
    {
        var site = await store.Load<Site>(cmd.SiteId.Stream, ct);
        site.AddLocale(new Locale(cmd.Locale));
        await store.Save(site, ct);
        return Result.Ok();
    }
}
