using Imprint.Authoring.Domain.Sites;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.SetSiteReviewer;

public sealed class SetSiteReviewerHandler(IAggregateStore store) : ICommandHandler<SetSiteReviewer>
{
    public async Task<Result> Handle(SetSiteReviewer cmd, CancellationToken ct)
    {
        var site = await store.Load<Site>(cmd.SiteId.Stream, ct);
        site.SetReviewer(cmd.Name, cmd.Email);
        await store.Save(site, ct);
        return Result.Ok();
    }
}
