using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Posts;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Posts.ChangePostTitle;

public sealed class ChangePostTitleHandler(IAggregateStore store) : ICommandHandler<ChangePostTitle>
{
    public async Task<Result> Handle(ChangePostTitle cmd, CancellationToken ct)
    {
        var post = await store.Load<Post>(cmd.PostId.Stream, ct);
        post.ChangeTitle(new Locale(cmd.Locale), cmd.Title);
        await store.Save(post, ct);
        return Result.Ok();
    }
}
