using Imprint.Authoring.Domain.Blocks;
using Imprint.Authoring.Projections;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Blocks.DeleteBlock;

public sealed class DeleteBlockHandler(IAggregateStore store, ContentUsage usage) : ICommandHandler<DeleteBlock>
{
    public async Task<Result> Handle(DeleteBlock cmd, CancellationToken ct)
    {
        // Cross-aggregate delete protection via the ContentUsage read model — the
        // BlockDefinition aggregate cannot see page streams. Accepted race: a page can
        // place a new instance in the instant after this check passes; the orphaned
        // instance renders as a visible "missing block" placeholder, never a crash.
        var instanceCount = usage.BlockInstanceCount(cmd.BlockId);
        if (instanceCount > 0)
        {
            var pageCount = usage.PagesUsingBlock(cmd.BlockId).Count;
            return Result.Fail(
                $"This block is placed {instanceCount} time(s) across {pageCount} page(s). " +
                "Remove or detach those instances first.");
        }

        var block = await store.Load<BlockDefinition>(cmd.BlockId.Stream, ct);
        block.Delete();
        await store.Save(block, ct);
        return Result.Ok();
    }
}
