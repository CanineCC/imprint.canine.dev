using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Projections;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.SetSocialImage;

public sealed class SetSocialImageHandler(IAggregateStore store, AssetLibrary assets)
    : ICommandHandler<SetSocialImage>
{
    public async Task<Result> Handle(SetSocialImage cmd, CancellationToken ct)
    {
        // A non-null share image must point at an asset that exists — a dangling og:image
        // is worse than none, because a platform that cannot fetch it shows a broken card
        // rather than falling back to the text-only one. Clearing (null) is always allowed.
        if (cmd.AssetId is { } id && assets.Get(id) is null)
        {
            return Result.Fail("The share image points at an asset that no longer exists.");
        }

        var site = await store.Load<Site>(cmd.SiteId.Stream, ct);
        site.SetSocialImage(cmd.AssetId);
        await store.Save(site, ct);
        return Result.Ok();
    }
}
