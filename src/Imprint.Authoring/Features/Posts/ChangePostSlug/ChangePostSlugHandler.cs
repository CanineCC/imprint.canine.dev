using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Posts;
using Imprint.Authoring.Projections;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Posts.ChangePostSlug;

public sealed class ChangePostSlugHandler(IAggregateStore store, PostList posts) : ICommandHandler<ChangePostSlug>
{
    public async Task<Result> Handle(ChangePostSlug cmd, CancellationToken ct)
    {
        _ = Slug.TryCreate(cmd.Slug, out var slug, out _);   // shape guaranteed by Validate()
        var post = await store.Load<Post>(cmd.PostId.Stream, ct);

        // Same accepted race as CreatePost, with the post itself excluded so re-saving an
        // unchanged slug is not a collision with itself.
        if (posts.SlugTaken(post.SiteId, slug, except: cmd.PostId))
        {
            return Result.Fail($"The slug '{slug}' is already used by another post on this site.");
        }

        post.ChangeSlug(slug);
        await store.Save(post, ct);
        return Result.Ok();
    }
}
