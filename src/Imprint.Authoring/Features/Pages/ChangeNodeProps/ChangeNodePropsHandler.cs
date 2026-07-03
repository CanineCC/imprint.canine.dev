using Imprint.Authoring.Domain.Pages;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Pages.ChangeNodeProps;

public sealed class ChangeNodePropsHandler(IAggregateStore store, IWidgetCatalog widgets)
    : ICommandHandler<ChangeNodeProps>
{
    public async Task<Result> Handle(ChangeNodeProps cmd, CancellationToken ct)
    {
        // Same manifest check as AddNode: editing a widget's props travels through
        // this slice, and the aggregate is manifest-blind by design. For containers
        // the aggregate ignores the replacement's children, so this walk can never
        // let a widget in through a side door.
        var widgetCheck = widgets.CheckWidgets(cmd.Replacement);
        if (!widgetCheck.Succeeded)
        {
            return widgetCheck;
        }

        var page = await store.Load<Page>(cmd.PageId.Stream, ct);
        page.ChangeNodeProps(cmd.Replacement);
        await store.Save(page, ct);
        return Result.Ok();
    }
}
