using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Projections;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Pages.DeletePage;

public sealed class DeletePageHandler(IAggregateStore store, PageList pageList)
    : ICommandHandler<DeletePage>
{
    public async Task<Result> Handle(DeletePage cmd, CancellationToken ct)
    {
        // Both guards consult the PageList read model. Accepted race: navigation can
        // change (or another page appear) in this same instant. Harm is bounded either
        // way — a dangling navigation item is skipped by the renderer, and the
        // publisher removes a deleted page's files, never breaking others.
        var summary = pageList.Get(cmd.PageId);
        if (summary is null)
        {
            return Result.Fail("The page no longer exists.");
        }

        if (summary.IsInNavigation)
        {
            return Result.Fail("This page is in the site navigation. Remove it from the navigation first.");
        }

        // Product decision, not an invariant: an empty site renders nothing at all,
        // and refusing to delete the last page is friendlier than publishing a blank
        // site. The editor manages a single site, so the global count is the right
        // scope for "only page".
        if (pageList.All().Count <= 1)
        {
            return Result.Fail("This is the only page on the site — create another page before deleting this one.");
        }

        var page = await store.Load<Page>(cmd.PageId.Stream, ct);
        page.Delete();
        await store.Save(page, ct);
        return Result.Ok();
    }
}
