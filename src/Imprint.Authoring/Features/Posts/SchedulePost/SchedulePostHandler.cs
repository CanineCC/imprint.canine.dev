using Imprint.Authoring.Domain.Posts;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Posts.SchedulePost;

public sealed class SchedulePostHandler(IAggregateStore store) : ICommandHandler<SchedulePost>
{
    public async Task<Result> Handle(SchedulePost cmd, CancellationToken ct)
    {
        var post = await store.Load<Post>(cmd.PostId.Stream, ct);
        post.SetPublishDate(cmd.PublishAt);
        await store.Save(post, ct);
        return Result.Ok();
    }
}
