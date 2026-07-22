using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Projections;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.SetHeaderLogo;

public sealed class SetHeaderLogoHandler(IAggregateStore store, AssetLibrary assets)
    : ICommandHandler<SetHeaderLogo>
{
    public async Task<Result> Handle(SetHeaderLogo cmd, CancellationToken ct)
    {
        // A non-null logo must point at an asset that exists. Clearing (null) is always allowed.
        if (cmd.AssetId is { } id && assets.Get(id) is null)
        {
            return Result.Fail("The header logo points at an asset that no longer exists.");
        }

        var site = await store.Load<Site>(cmd.SiteId.Stream, ct);
        site.SetHeaderLogo(cmd.AssetId);
        await store.Save(site, ct);
        return Result.Ok();
    }
}
