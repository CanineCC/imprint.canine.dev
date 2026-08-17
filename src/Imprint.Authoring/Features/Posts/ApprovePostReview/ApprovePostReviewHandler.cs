using Imprint.Authoring.Domain.Posts;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Posts.ApprovePostReview;

public sealed class ApprovePostReviewHandler(IAggregateStore store) : ICommandHandler<ApprovePostReview>
{
    public async Task<Result> Handle(ApprovePostReview cmd, CancellationToken ct)
    {
        var post = await store.Load<Post>(cmd.PostId.Stream, ct);
        post.ApproveReview(cmd.PublishAt);
        await store.Save(post, ct);
        return Result.Ok();
    }
}
