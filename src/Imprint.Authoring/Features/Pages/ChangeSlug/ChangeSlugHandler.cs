using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Projections;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Pages.ChangeSlug;

public sealed class ChangeSlugHandler(IAggregateStore store, PageList pageList)
    : ICommandHandler<ChangeSlug>
{
    public async Task<Result> Handle(ChangeSlug cmd, CancellationToken ct)
    {
        _ = Slug.TryCreate(cmd.Slug, out var slug, out _); // shape guaranteed by Validate()

        // Load first so slug uniqueness is checked within THIS page's site — slugs are
        // unique per site, not globally (another site may legitimately hold the same slug).
        var page = await store.Load<Page>(cmd.PageId.Stream, ct);

        // Cross-aggregate uniqueness via the PageList read model, excluding this page
        // so renaming to the current slug stays a no-op. Accepted race: two editors
        // claiming the same slug in the same instant both pass — the publisher detects
        // the collision and fails that page's render visibly (docs/publishing.md).
        if (pageList.SlugTaken(page.SiteId, slug, except: cmd.PageId))
        {
            return Result.Fail($"The slug '{slug}' is already used by another page on this site.");
        }

        page.ChangeSlug(slug);
        await store.Save(page, ct);
        return Result.Ok();
    }
}
