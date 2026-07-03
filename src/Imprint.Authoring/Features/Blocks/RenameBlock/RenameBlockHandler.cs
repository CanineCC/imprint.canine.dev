using Imprint.Authoring.Domain.Blocks;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Blocks.RenameBlock;

public sealed class RenameBlockHandler(IAggregateStore store) : ICommandHandler<RenameBlock>
{
    public async Task<Result> Handle(RenameBlock cmd, CancellationToken ct)
    {
        var block = await store.Load<BlockDefinition>(cmd.BlockId.Stream, ct);
        block.Rename(cmd.Name);
        await store.Save(block, ct);
        return Result.Ok();
    }
}
