using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Projections;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.SetPostOrder;

public sealed class SetPostOrderHandler(IAggregateStore store, PostList posts) : ICommandHandler<SetPostOrder>
{
    public async Task<Result> Handle(SetPostOrder cmd, CancellationToken ct)
    {
        var mine = posts.All(cmd.SiteId).Select(p => p.Id).ToHashSet();
        if (cmd.Posts.Where(id => !mine.Contains(id)).ToList() is [var stranger, ..])
        {
            return Result.Fail($"{stranger.Compact} is not a post of this site.");
        }

        var site = await store.Load<Site>(cmd.SiteId.Stream, ct);
        site.SetPostOrder(cmd.Posts);
        await store.Save(site, ct);
        return Result.Ok();
    }
}
