using Imprint.Authoring.Domain.Sites;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.ChangeThemeToken;

public sealed class ChangeThemeTokenHandler(IAggregateStore store) : ICommandHandler<ChangeThemeToken>
{
    public async Task<Result> Handle(ChangeThemeToken cmd, CancellationToken ct)
    {
        var site = await store.Load<Site>(cmd.SiteId.Stream, ct);
        site.SetThemeToken(cmd.Token, cmd.Light, cmd.Dark);
        await store.Save(site, ct);
        return Result.Ok();
    }
}
