using Imprint.Authoring.Domain.Widgets;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Widgets.RejectWidget;

public sealed class RejectWidgetHandler(IAggregateStore store) : ICommandHandler<RejectWidget>
{
    public async Task<Result> Handle(RejectWidget cmd, CancellationToken ct)
    {
        var submission = await store.Load<WidgetSubmission>(cmd.WidgetSubmissionId.Stream, ct);
        submission.Reject(cmd.RejectedBy, cmd.Reason);
        await store.Save(submission, ct);
        return Result.Ok();
    }
}
