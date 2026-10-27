using Imprint.Authoring.Domain.Posts;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Posts.ClassifyPost;

public sealed class ClassifyPostHandler(IAggregateStore store) : ICommandHandler<ClassifyPost>
{
    public async Task<Result> Handle(ClassifyPost cmd, CancellationToken ct)
    {
        var post = await store.Load<Post>(cmd.PostId.Stream, ct);
        post.Classify(cmd.Kind, cmd.Products);
        await store.Save(post, ct);
        return Result.Ok();
    }
}
