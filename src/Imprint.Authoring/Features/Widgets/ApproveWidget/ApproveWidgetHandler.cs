using Imprint.Authoring.Domain.Widgets;
using Imprint.Authoring.Projections;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Widgets.ApproveWidget;

public sealed class ApproveWidgetHandler(IAggregateStore store, WidgetRegistry registry)
    : ICommandHandler<ApproveWidget>
{
    public async Task<Result> Handle(ApproveWidget cmd, CancellationToken ct)
    {
        var submission = await store.Load<WidgetSubmission>(cmd.WidgetSubmissionId.Stream, ct);

        // Re-check the tag collision at the moment of approval (documented race): the
        // approved set may have gained this tag since submit. Excludes this submission
        // itself. A built-in collision isn't visible here — the merged catalog owns it.
        if (registry.Approved.Any(s => s.Tag == submission.Tag && !s.Id.Equals(cmd.WidgetSubmissionId)))
        {
            return Result.Fail(
                $"The tag '{submission.Tag}' was approved for another widget in the meantime. Reject this one or ask for a new tag.");
        }

        submission.Approve(cmd.ApprovedBy);
        await store.Save(submission, ct);
        return Result.Ok();
    }
}
