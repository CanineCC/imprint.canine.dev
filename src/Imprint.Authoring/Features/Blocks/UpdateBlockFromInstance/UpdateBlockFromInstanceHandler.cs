using Imprint.Authoring.Domain.Blocks;
using Imprint.Authoring.Domain.Pages;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Blocks.UpdateBlockFromInstance;

public sealed class UpdateBlockFromInstanceHandler(IAggregateStore store) : ICommandHandler<UpdateBlockFromInstance>
{
    public async Task<Result> Handle(UpdateBlockFromInstance cmd, CancellationToken ct)
    {
        var page = await store.Load<Page>(cmd.PageId.Stream, ct);
        var node = page.Tree.Find(cmd.InstanceId);
        if (node is null)
        {
            return Result.Fail("The block instance no longer exists on this page.");
        }

        if (node is not BlockInstanceNode instance)
        {
            return Result.Fail($"A {node.DisplayName} is not a block instance.");
        }

        if (instance.Overrides.Count == 0)
        {
            // The instance shows the definition verbatim — there is nothing to push,
            // and a spec-changed event here would only mark every page stale for free.
            return Result.Ok();
        }

        var definition = await store.Load<BlockDefinition>(instance.DefinitionId.Stream, ct);

        // What this instance currently renders becomes the definition for everyone.
        var resolved = BlockContentResolver.Resolve(definition.Spec, instance.Overrides);
        definition.ChangeSpec(resolved);
        await store.Save(definition, ct);

        // Clearing the overrides matters: they are now baked into the definition, and
        // a stale override would silently shadow every future edit of that field on
        // the definition — the instance would look "stuck" for no visible reason.
        // Two streams, not atomic: if this append fails the definition already carries
        // the pushed content and the instance keeps rendering exactly what it rendered
        // before (override == new definition value) — benign, fixed by re-running.
        foreach (var entry in instance.Overrides.Entries.ToList())
        {
            page.SetBlockOverride(cmd.InstanceId, entry.DefinitionNodeId, entry.Field, entry.Locale, null);
        }

        await store.Save(page, ct);
        return Result.Ok();
    }
}
