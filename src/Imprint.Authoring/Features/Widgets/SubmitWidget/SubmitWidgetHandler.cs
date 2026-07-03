using Imprint.Authoring.Domain.Widgets;
using Imprint.Authoring.Projections;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Widgets.SubmitWidget;

public sealed class SubmitWidgetHandler(IAggregateStore store, WidgetRegistry registry)
    : ICommandHandler<SubmitWidget>
{
    public async Task<Result> Handle(SubmitWidget cmd, CancellationToken ct)
    {
        // Cross-aggregate check via the read model (documented race): two pending
        // submissions may share a tag, and both could be approved in the same instant —
        // that collision is caught again at approve time, where the second approval
        // fails. We only guard APPROVED tags here. A collision with a *built-in*
        // filesystem widget is invisible to this slice; the merged EditorWidgetCatalog /
        // UI layer owns that check (built-ins can't be shadowed anyway).
        if (registry.IsApprovedTag(cmd.Tag))
        {
            return Result.Fail($"The tag '{cmd.Tag}' is already used by an approved widget. Choose a different tag.");
        }

        var submission = WidgetSubmission.Submit(
            cmd.WidgetSubmissionId, cmd.Tag, cmd.Name, cmd.Description, cmd.Placeholder,
            cmd.AspectRatio, cmd.Eager, cmd.Props, cmd.BundleSource, cmd.RequestedBy);
        await store.Save(submission, ct);
        return Result.Ok();
    }
}
