using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Blocks;
using Imprint.Authoring.Domain.Pages;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Blocks.DefineBlockFromNode;

/// <summary>
/// One logical operation, two aggregates, two transactions (docs/domain-model.md §6):
/// the definition stream is committed first, then the page swap (remove + insert
/// instance) commits as one append — atomic within the page stream. If the page
/// append fails, the handler compensates by deleting the just-created definition.
/// </summary>
public sealed class DefineBlockFromNodeHandler(IAggregateStore store) : ICommandHandler<DefineBlockFromNode>
{
    public async Task<Result> Handle(DefineBlockFromNode cmd, CancellationToken ct)
    {
        var page = await store.Load<Page>(cmd.PageId.Stream, ct);
        var node = page.Tree.Find(cmd.NodeId);
        if (node is null)
        {
            return Result.Fail("The element no longer exists on this page.");
        }

        if (node is SectionNode)
        {
            return Result.Fail("A whole section cannot become a block — pick the content inside it instead.");
        }

        if (node is BlockInstanceNode)
        {
            return Result.Fail("This element is already a block instance.");
        }

        if (Placement.IsManagedCell(page.Tree, cmd.NodeId))
        {
            return Result.Fail(
                "A column cell cannot become a block — pick the whole columns element or the content inside the cell.");
        }

        var parent = page.Tree.ParentOf(cmd.NodeId);
        var parentId = parent?.Id ?? NodeId.Root;
        var siblings = parent is null ? page.Tree.Roots : ((IContainerNode)parent).Children;
        var index = siblings.IndexOf(cmd.NodeId);

        // The definition gets its own ids: overrides are keyed by definition node ids,
        // and those must never collide with ids still living on this (or any) page.
        var definition = BlockDefinition.Define(
            cmd.NewBlockId, cmd.Name, BlockContentResolver.WithFreshIds(node));

        // Both page mutations happen in memory before anything is persisted, so a
        // domain rejection here costs nothing to undo.
        page.RemoveNode(cmd.NodeId);
        page.AddNode(parentId, index, new BlockInstanceNode { Id = NodeId.New(), DefinitionId = cmd.NewBlockId });

        await store.Save(definition, ct);
        try
        {
            await store.Save(page, ct);
        }
        catch
        {
            // Compensation: the definition exists but no instance ever will — delete
            // it and rethrow so the dispatcher reports (or retries) the original
            // failure. CancellationToken.None because the compensation must run even
            // when the failure *is* a cancellation.
            definition.Delete();
            await store.Save(definition, CancellationToken.None);
            throw;
        }

        return Result.Ok();
    }
}
