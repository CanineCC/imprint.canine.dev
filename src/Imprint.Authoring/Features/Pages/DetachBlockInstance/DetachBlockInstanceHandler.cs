using Imprint.Authoring.Domain.Blocks;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Projections;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Pages.DetachBlockInstance;

public sealed class DetachBlockInstanceHandler(IAggregateStore store, BlockLibrary blocks)
    : ICommandHandler<DetachBlockInstance>
{
    public async Task<Result> Handle(DetachBlockInstance cmd, CancellationToken ct)
    {
        var page = await store.Load<Page>(cmd.PageId.Stream, ct);
        if (page.Tree.Find(cmd.InstanceId) is not BlockInstanceNode instance)
        {
            return Result.Fail("The block instance no longer exists on this page.");
        }

        // The definition comes from the BlockLibrary read model — the only view a
        // Pages slice has of another aggregate. Accepted race: a definition edit in
        // this same instant detaches the version the editor was looking at, which is
        // exactly what the user asked to keep. A deleted definition leaves nothing to
        // materialize, so that case fails with advice rather than an empty node.
        var definition = blocks.Get(instance.DefinitionId);
        if (definition is null)
        {
            return Result.Fail(
                "This block's definition has been deleted, so there is no content to detach — remove the element instead.");
        }

        // Resolve = the definition's subtree with this instance's overrides applied;
        // fresh ids because the materialized nodes start a life of their own here.
        var replacement = BlockContentResolver.WithFreshIds(
            BlockContentResolver.Resolve(definition.Spec, instance.Overrides));
        page.DetachBlockInstance(cmd.InstanceId, replacement);
        await store.Save(page, ct);
        return Result.Ok();
    }
}
