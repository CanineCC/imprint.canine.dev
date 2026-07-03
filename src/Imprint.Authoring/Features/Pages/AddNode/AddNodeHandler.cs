using Imprint.Authoring.Domain.Pages;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Pages.AddNode;

public sealed class AddNodeHandler(IAggregateStore store, IWidgetCatalog widgets)
    : ICommandHandler<AddNode>
{
    public async Task<Result> Handle(AddNode cmd, CancellationToken ct)
    {
        // Widget tags and prop names are checked against the manifest here in the
        // slice; the aggregate is manifest-blind by design (see IWidgetCatalog).
        // Everything else about the spec — placement, ids, depth, budget — is the
        // aggregate's job.
        var widgetCheck = widgets.CheckWidgets(cmd.Spec);
        if (!widgetCheck.Succeeded)
        {
            return widgetCheck;
        }

        var page = await store.Load<Page>(cmd.PageId.Stream, ct);
        page.AddNode(cmd.ParentId, cmd.Index, cmd.Spec);
        await store.Save(page, ct);
        return Result.Ok();
    }
}
