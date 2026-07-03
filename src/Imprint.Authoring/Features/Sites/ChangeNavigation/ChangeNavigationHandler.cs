using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Projections;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.ChangeNavigation;

public sealed class ChangeNavigationHandler(IAggregateStore store, PageList pages) : ICommandHandler<ChangeNavigation>
{
    public async Task<Result> Handle(ChangeNavigation cmd, CancellationToken ct)
    {
        // Cross-aggregate check via the PageList read model — the Site aggregate
        // cannot see page streams. Accepted race: a page deleted this very instant
        // still enters the navigation as a dangling item, which the renderer skips —
        // the published output never carries a broken link.
        foreach (var item in cmd.Items)
        {
            if (pages.Get(item.PageId) is null)
            {
                return Result.Fail("One of the navigation entries points at a page that no longer exists.");
            }
        }

        var site = await store.Load<Site>(cmd.SiteId.Stream, ct);
        site.SetNavigation(cmd.Items);
        await store.Save(site, ct);
        return Result.Ok();
    }
}
