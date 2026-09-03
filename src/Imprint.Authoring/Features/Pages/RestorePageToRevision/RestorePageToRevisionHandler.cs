using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Projections;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Pages.RestorePageToRevision;

/// <summary>
/// Replays the page's own stream up to the requested version, takes the tree that replay produces, and
/// hands it to <see cref="Page.RestoreContent"/> — the same primitive revert-to-published uses.
///
/// <para>★ The restore is APPENDED, never rewritten. The stream keeps every revision including the ones
/// being undone, and the restore itself becomes the next revision, so "we put it back" is as auditable
/// as the change that made it necessary. An event-sourced CMS already holds every version by
/// construction; until this existed, nothing could reach them.</para>
///
/// <para>Only content is restored — not the slug, title, meta or published state. Those are separate
/// decisions with their own commands, and quietly reverting a slug would break every inbound link to
/// the page as a side effect of a content rollback.</para>
/// </summary>
public sealed class RestorePageToRevisionHandler(IAggregateStore store, IEventStore events, PageList pages)
    : ICommandHandler<RestorePageToRevision>
{
    public async Task<Result> Handle(RestorePageToRevision cmd, CancellationToken ct)
    {
        if (pages.Get(cmd.PageId) is null)
        {
            return Result.Fail("The page no longer exists.");
        }

        if (cmd.Version < 1)
        {
            return Result.Fail("Revisions are numbered from 1.");
        }

        var history = await events.ReadStream(cmd.PageId.Stream, cmd.Version, ct);
        if (history.Count == 0)
        {
            return Result.Fail("This page has no history to restore from.");
        }

        // ReadStream is bounded, not exact: asking for version 900 of a 12-revision page returns all 12
        // rather than failing. Restoring "the latest" while believing you restored revision 900 is the
        // silent wrong answer here, so the gap is checked and named.
        var newest = history[^1].StreamVersion;
        if (newest < cmd.Version)
        {
            return Result.Fail($"This page has {newest} revision(s); there is no revision {cmd.Version}.");
        }

        var asOf = new Page();
        asOf.LoadFrom(history.Select(e => e.Event));

        var page = await store.Load<Page>(cmd.PageId.Stream, ct);
        page.RestoreContent(asOf.Tree.Roots);
        await store.Save(page, ct);
        return Result.Ok();
    }
}
