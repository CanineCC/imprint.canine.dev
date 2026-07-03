using Imprint.Authoring.Domain.Pages;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Pages.RemoveNode;

public sealed class RemoveNodeHandler(IAggregateStore store) : ICommandHandler<RemoveNode>
{
    public async Task<Result> Handle(RemoveNode cmd, CancellationToken ct)
    {
        var page = await store.Load<Page>(cmd.PageId.Stream, ct);
        page.RemoveNode(cmd.NodeId);
        await store.Save(page, ct);
        return Result.Ok();
    }
}
