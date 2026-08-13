using Imprint.Authoring.Domain.Posts;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Posts.DeletePost;

public sealed class DeletePostHandler(IAggregateStore store) : ICommandHandler<DeletePost>
{
    public async Task<Result> Handle(DeletePost cmd, CancellationToken ct)
    {
        var post = await store.Load<Post>(cmd.PostId.Stream, ct);
        post.Delete();
        await store.Save(post, ct);
        return Result.Ok();
    }
}
