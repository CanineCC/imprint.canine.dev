using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Projections;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.SetHeaderActions;

public sealed class SetHeaderActionsHandler(IAggregateStore store, PageList pages) : ICommandHandler<SetHeaderActions>
{
    public async Task<Result> Handle(SetHeaderActions cmd, CancellationToken ct)
    {
        // A header action may point at a same-site page; that page must still exist.
        foreach (var action in new[] { cmd.Cta, cmd.Quiet })
        {
            if (action?.PageId is { } pageId && pages.Get(pageId) is null)
            {
                return Result.Fail("A header action points at a page that no longer exists.");
            }
        }

        var site = await store.Load<Site>(cmd.SiteId.Stream, ct);
        site.SetHeaderActions(cmd.Cta, cmd.Quiet);
        await store.Save(site, ct);
        return Result.Ok();
    }
}
