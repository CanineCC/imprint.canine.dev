using Imprint.Authoring.Domain.Pages;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Pages.MoveNode;

public sealed class MoveNodeHandler(IAggregateStore store) : ICommandHandler<MoveNode>
{
    public async Task<Result> Handle(MoveNode cmd, CancellationToken ct)
    {
        var page = await store.Load<Page>(cmd.PageId.Stream, ct);
        page.MoveNode(cmd.NodeId, cmd.NewParentId, cmd.NewIndex);
        await store.Save(page, ct);
        return Result.Ok();
    }
}
