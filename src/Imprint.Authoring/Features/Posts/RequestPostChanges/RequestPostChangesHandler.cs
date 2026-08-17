using Imprint.Authoring.Domain.Posts;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Posts.RequestPostChanges;

public sealed class RequestPostChangesHandler(IAggregateStore store) : ICommandHandler<RequestPostChanges>
{
    public async Task<Result> Handle(RequestPostChanges cmd, CancellationToken ct)
    {
        var post = await store.Load<Post>(cmd.PostId.Stream, ct);
        post.RequestChanges(cmd.Reason);
        await store.Save(post, ct);
        return Result.Ok();
    }
}
