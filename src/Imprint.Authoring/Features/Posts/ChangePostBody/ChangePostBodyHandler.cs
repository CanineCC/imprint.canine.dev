using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Posts;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Posts.ChangePostBody;

public sealed class ChangePostBodyHandler(IAggregateStore store) : ICommandHandler<ChangePostBody>
{
    public async Task<Result> Handle(ChangePostBody cmd, CancellationToken ct)
    {
        var post = await store.Load<Post>(cmd.PostId.Stream, ct);
        post.ChangeBody(new Locale(cmd.Locale), cmd.Markdown);
        await store.Save(post, ct);
        return Result.Ok();
    }
}
