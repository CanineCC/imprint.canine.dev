using Imprint.Authoring.Domain;
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
        // the published output never carries a broken link. Both top-level direct page
        // links and same-site page links inside a group's children are checked; external
        // links carry no PageId and are passed through verbatim.
        var pageLinks = cmd.Items
            .SelectMany(item => item.IsGroup
                ? item.Children.Select(child => child.PageId)
                : [item.PageId])
            .OfType<PageId>();
        foreach (var pageId in pageLinks)
        {
            if (pages.Get(pageId) is null)
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
