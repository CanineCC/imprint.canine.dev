using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Projections;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Pages.RevertPageToPublished;

/// <summary>
/// Reads the page's last-published node tree from <see cref="PublishedContent"/> and
/// replaces the draft content with it through the aggregate's <see cref="Page.RestoreContent"/>
/// primitive. A page that was never published has no snapshot to restore — that is a
/// user-facing failure, not a crash, so it surfaces as a toast (the editor also disables
/// the button in that state).
/// </summary>
public sealed class RevertPageToPublishedHandler(IAggregateStore store, PublishedContent published)
    : ICommandHandler<RevertPageToPublished>
{
    public async Task<Result> Handle(RevertPageToPublished cmd, CancellationToken ct)
    {
        if (published.Get(cmd.PageId) is not { } snapshot)
        {
            return Result.Fail("This page has never been published, so there is nothing to restore.");
        }

        var page = await store.Load<Page>(cmd.PageId.Stream, ct);
        page.RestoreContent(snapshot.Tree.Roots);
        await store.Save(page, ct);
        return Result.Ok();
    }
}
