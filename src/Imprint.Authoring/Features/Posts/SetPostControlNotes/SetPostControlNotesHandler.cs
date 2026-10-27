using Imprint.Authoring.Domain.Posts;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Posts.SetPostControlNotes;

public sealed class SetPostControlNotesHandler(IAggregateStore store) : ICommandHandler<SetPostControlNotes>
{
    public async Task<Result> Handle(SetPostControlNotes cmd, CancellationToken ct)
    {
        var post = await store.Load<Post>(cmd.PostId.Stream, ct);
        post.SetControlNotes(cmd.Notes);
        await store.Save(post, ct);
        return Result.Ok();
    }
}
