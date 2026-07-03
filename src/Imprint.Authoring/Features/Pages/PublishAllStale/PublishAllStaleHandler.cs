using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Projections;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Pages.PublishAllStale;

/// <summary>
/// Fan-out of <c>Publish</c> over every stale page. Page-at-a-time is correct here —
/// publishes are independent facts about independent streams, so each page gets its
/// own aggregate transaction and one page's failure must not abort its siblings.
/// </summary>
public sealed class PublishAllStaleHandler(IAggregateStore store, PageList pageList)
    : ICommandHandler<PublishAllStale>
{
    public async Task<Result> Handle(PublishAllStale command, CancellationToken ct)
    {
        var failures = new List<string>();

        // The stale set comes from the PageList read model. Accepted race: a page
        // edited, deleted or published in this same instant is decided against its
        // *stream* when loaded below, so the worst case is a per-page domain error in
        // the summary — never a wrong publish.
        foreach (var summary in pageList.All().Where(page => page.Status != PageStatus.Published))
        {
            try
            {
                var page = await store.Load<Page>(summary.Id.Stream, ct);
                page.Publish();
                await store.Save(page, ct);
            }
            catch (DomainException failure)
            {
                // The "handlers never catch DomainException" rule serves single-
                // aggregate slices, where the dispatcher's translation is the whole
                // story. This fan-out is the documented exception: catching per page
                // is what keeps the publishes independent; the collected messages
                // become one Result the editor can show as a publish report.
                failures.Add($"{summary.Slug}: {failure.Message}");
            }
        }

        return failures.Count == 0 ? Result.Ok() : Result.Fail([.. failures]);
    }
}
