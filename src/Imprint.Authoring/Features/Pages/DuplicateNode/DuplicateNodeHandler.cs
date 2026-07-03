using Imprint.Authoring.Domain.Pages;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Pages.DuplicateNode;

public sealed class DuplicateNodeHandler(IAggregateStore store) : ICommandHandler<DuplicateNode>
{
    public async Task<Result> Handle(DuplicateNode cmd, CancellationToken ct)
    {
        var page = await store.Load<Page>(cmd.PageId.Stream, ct);
        page.DuplicateNode(cmd.NodeId);
        await store.Save(page, ct);
        return Result.Ok();
    }
}
