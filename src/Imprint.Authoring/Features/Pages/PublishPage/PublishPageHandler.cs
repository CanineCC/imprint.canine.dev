using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Sites;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Pages.PublishPage;

/// <summary>
/// A straight pass-through (after the site's own say: an archived site publishes nothing), and it must stay that way: <c>Page.Publish</c> computes
/// the published version as "the position this event will occupy", which is only
/// sound while the publish event is the sole event of its commit — the aggregate
/// asserts exactly that.
/// </summary>
public sealed class PublishPageHandler(IAggregateStore store) : ICommandHandler<PublishPage>
{
    public async Task<Result> Handle(PublishPage cmd, CancellationToken ct)
    {
        var page = await store.Load<Page>(cmd.PageId.Stream, ct);
        (await store.Load<Site>(page.SiteId.Stream, ct)).EnsureMayPublish();
        page.Publish();
        await store.Save(page, ct);
        return Result.Ok();
    }
}
