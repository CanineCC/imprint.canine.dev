using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Posts;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Posts.PublishPost;

public sealed class PublishPostHandler(IAggregateStore store) : ICommandHandler<PublishPost>
{
    public async Task<Result> Handle(PublishPost cmd, CancellationToken ct)
    {
        var post = await store.Load<Post>(cmd.PostId.Stream, ct);
        // The system clock, read once here: the aggregate takes the instant as a parameter so
        // its own rules (first date wins) stay unit-testable without a clock abstraction.
        post.Publish(new Locale(cmd.Locale), TimeProvider.System.GetUtcNow());
        await store.Save(post, ct);
        return Result.Ok();
    }
}
