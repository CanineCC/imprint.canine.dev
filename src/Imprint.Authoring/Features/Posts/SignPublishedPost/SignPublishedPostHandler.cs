using Imprint.Authoring.Domain.Posts;
using Imprint.Authoring.Projections;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Posts.SignPublishedPost;

/// <summary>
/// A signature, so the same rule as approving (see <c>ApprovePostReviewHandler</c>): the event's actor is the login
/// that signs, and a SERVICE identity is refused, or a key could sign in a person's name.
/// </summary>
public sealed class SignPublishedPostHandler(IAggregateStore store, EventMetadataProvider metadata, PostList posts)
    : ICommandHandler<SignPublishedPost>, ICommandHandler<SignPublishedPosts>
{
    public async Task<Result> Handle(SignPublishedPost cmd, CancellationToken ct)
    {
        if (Refused() is { } refused)
        {
            return refused;
        }

        var post = await store.Load<Post>(cmd.PostId.Stream, ct);
        post.SignPublished();
        await store.Save(post, ct);
        return Result.Ok();
    }

    public async Task<Result> Handle(SignPublishedPosts cmd, CancellationToken ct)
    {
        if (Refused() is { } refused)
        {
            return refused;
        }

        // Only what is live as published: an article with unpublished edits has nothing live to sign yet, and a
        // signed one is skipped by the aggregate. Each article is its own transaction, like PublishAllStale.
        foreach (var summary in posts.All(cmd.SiteId).Where(p => p.Status == PostStatus.Published))
        {
            var post = await store.Load<Post>(summary.Id.Stream, ct);
            post.SignPublished();
            await store.Save(post, ct);
        }

        return Result.Ok();
    }

    private Result? Refused() =>
        metadata.GetCurrent().Actor.StartsWith("service:", StringComparison.Ordinal)
            ? Result.Fail(
                "Signing is a signature, so it has to be done by a person signed in to the editor. " +
                "A service identity cannot sign.")
            : null;
}
