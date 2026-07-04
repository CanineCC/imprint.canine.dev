using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Projections;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.SetFooter;

public sealed class SetFooterHandler(IAggregateStore store, PageList pages) : ICommandHandler<SetFooter>
{
    public async Task<Result> Handle(SetFooter cmd, CancellationToken ct)
    {
        // Same cross-aggregate check as navigation: every same-site page link must point
        // at a page that still exists. External links are passed through verbatim.
        var pageLinks = cmd.Groups
            .SelectMany(group => group.Links.Select(link => link.PageId))
            .OfType<PageId>();
        foreach (var pageId in pageLinks)
        {
            if (pages.Get(pageId) is null)
            {
                return Result.Fail("One of the footer links points at a page that no longer exists.");
            }
        }

        var site = await store.Load<Site>(cmd.SiteId.Stream, ct);
        site.SetFooter(cmd.Groups);
        await store.Save(site, ct);
        return Result.Ok();
    }
}
