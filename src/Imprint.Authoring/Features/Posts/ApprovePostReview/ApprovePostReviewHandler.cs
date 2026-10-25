using Imprint.Authoring.Domain.Posts;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Posts.ApprovePostReview;

/// <summary>
/// Approval is a person's signature: the event's actor is the login that cleared the words, and the
/// event names the revision and hash of those words. Anyone may sign, the author included; what is
/// refused is a SERVICE identity (the authoring API key, the MCP server, any agent holding them),
/// because an approval that a key can produce documents nothing about who read the text.
/// </summary>
public sealed class ApprovePostReviewHandler(IAggregateStore store, EventMetadataProvider metadata)
    : ICommandHandler<ApprovePostReview>
{
    public async Task<Result> Handle(ApprovePostReview cmd, CancellationToken ct)
    {
        if (metadata.GetCurrent().Actor.StartsWith("service:", StringComparison.Ordinal))
        {
            return Result.Fail(
                "Approving is a signature, so it has to be done by a person signed in to the editor. " +
                "A service identity can prepare and submit a post, but cannot approve it.");
        }

        var post = await store.Load<Post>(cmd.PostId.Stream, ct);
        post.ApproveReview(cmd.PublishAt);
        await store.Save(post, ct);
        return Result.Ok();
    }
}
