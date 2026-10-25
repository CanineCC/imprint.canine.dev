using Imprint.Authoring.Domain.Posts;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Posts.HoldPost;

public sealed class HoldPostHandler(IAggregateStore store) : ICommandHandler<HoldPost>
{
    public async Task<Result> Handle(HoldPost cmd, CancellationToken ct)
    {
        var post = await store.Load<Post>(cmd.PostId.Stream, ct);
        post.SetHold(cmd.Reason);
        await store.Save(post, ct);
        return Result.Ok();
    }
}
