using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Projections;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.SetFavicon;

public sealed class SetFaviconHandler(IAggregateStore store, AssetLibrary assets)
    : ICommandHandler<SetFavicon>
{
    public async Task<Result> Handle(SetFavicon cmd, CancellationToken ct)
    {
        // A non-null favicon must point at an asset that exists — otherwise the published
        // <link rel="icon"> would resolve to nothing. Clearing (null) is always allowed.
        if (cmd.AssetId is { } id && assets.Get(id) is null)
        {
            return Result.Fail("The favicon points at an asset that no longer exists.");
        }

        var site = await store.Load<Site>(cmd.SiteId.Stream, ct);
        site.SetFavicon(cmd.AssetId);
        await store.Save(site, ct);
        return Result.Ok();
    }
}
