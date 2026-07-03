using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Projections;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Pages.SetBlockOverride;

public sealed class SetBlockOverrideHandler(IAggregateStore store, BlockLibrary blocks)
    : ICommandHandler<SetBlockOverride>
{
    public async Task<Result> Handle(SetBlockOverride cmd, CancellationToken ct)
    {
        var page = await store.Load<Page>(cmd.PageId.Stream, ct);

        // The definition lives in another aggregate, so this check goes through the
        // BlockLibrary read model. Belt and braces, not load-bearing: renders ignore
        // overrides for unknown definition node ids, so the accepted race — the
        // definition edited or deleted in this same instant — leaves a dormant
        // override, never a broken page. When the instance itself is missing, the
        // aggregate below raises the authoritative error.
        if (page.Tree.Find(cmd.InstanceId) is BlockInstanceNode instance)
        {
            var definition = blocks.Get(instance.DefinitionId);
            if (definition is null)
            {
                return Result.Fail("This block's definition no longer exists — detach or remove the block instead.");
            }

            var target = PageTree.Flatten(definition.Spec).FirstOrDefault(node => node.Id == cmd.DefinitionNodeId);
            if (target is null)
            {
                return Result.Fail("That element is no longer part of the block.");
            }

            if (!CarriesField(target, cmd.Field))
            {
                return Result.Fail($"{target.DisplayName} has no editable '{cmd.Field}' text.");
            }
        }

        page.SetBlockOverride(cmd.InstanceId, cmd.DefinitionNodeId, cmd.Field, new Locale(cmd.Locale), cmd.Value);
        await store.Save(page, ct);
        return Result.Ok();
    }

    // Mirrors the locale-valued fields BlockContentResolver overlays at render.
    private static bool CarriesField(Node node, string field) => (node, field) switch
    {
        (HeadingNode, "text") => true,
        (RichTextNode, "html") => true,
        (ButtonNode, "label") => true,
        (ImageNode, "alt") => true,
        (SvgNode, "alt") => true,
        _ => false,
    };
}
