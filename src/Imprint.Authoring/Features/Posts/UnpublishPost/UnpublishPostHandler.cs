using Imprint.Authoring.Domain.Posts;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Posts.UnpublishPost;

public sealed class UnpublishPostHandler(IAggregateStore store) : ICommandHandler<UnpublishPost>
{
    public async Task<Result> Handle(UnpublishPost cmd, CancellationToken ct)
    {
        var post = await store.Load<Post>(cmd.PostId.Stream, ct);
        post.Unpublish();
        await store.Save(post, ct);
        return Result.Ok();
    }
}
