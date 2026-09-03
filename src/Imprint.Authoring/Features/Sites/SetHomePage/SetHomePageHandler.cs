using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Projections;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.SetHomePage;

public sealed class SetHomePageHandler(IAggregateStore store, PageList pages)
    : ICommandHandler<SetHomePage>
{
    public async Task<Result> Handle(SetHomePage cmd, CancellationToken ct)
    {
        // A site root that points at nothing would publish no "/" at all, so the page has to
        // exist. It also has to belong to THIS site: pointing one site's root at another
        // site's page is the cross-site version of the bug this command exists to prevent.
        if (cmd.PageId is { } id)
        {
            var page = pages.Get(id);
            if (page is null)
            {
                return Result.Fail("The home page points at a page that no longer exists.");
            }

            if (!pages.All(cmd.SiteId).Any(candidate => candidate.Id == id))
            {
                return Result.Fail("The home page must be a page of this site.");
            }
        }

        var site = await store.Load<Site>(cmd.SiteId.Stream, ct);
        site.SetHomePage(cmd.PageId);
        await store.Save(site, ct);
        return Result.Ok();
    }
}
