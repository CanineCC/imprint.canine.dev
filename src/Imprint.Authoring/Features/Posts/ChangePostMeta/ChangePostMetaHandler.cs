using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Posts;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Posts.ChangePostMeta;

public sealed class ChangePostMetaHandler(IAggregateStore store) : ICommandHandler<ChangePostMeta>
{
    public async Task<Result> Handle(ChangePostMeta cmd, CancellationToken ct)
    {
        var post = await store.Load<Post>(cmd.PostId.Stream, ct);
        post.ChangeMeta(new Locale(cmd.Locale), cmd.MetaTitle, cmd.MetaDescription);
        await store.Save(post, ct);
        return Result.Ok();
    }
}
