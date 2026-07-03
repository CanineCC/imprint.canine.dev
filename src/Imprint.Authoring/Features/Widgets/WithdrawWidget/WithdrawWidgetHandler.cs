using Imprint.Authoring.Domain.Widgets;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Widgets.WithdrawWidget;

public sealed class WithdrawWidgetHandler(IAggregateStore store) : ICommandHandler<WithdrawWidget>
{
    public async Task<Result> Handle(WithdrawWidget cmd, CancellationToken ct)
    {
        var submission = await store.Load<WidgetSubmission>(cmd.WidgetSubmissionId.Stream, ct);
        submission.Withdraw();
        await store.Save(submission, ct);
        return Result.Ok();
    }
}
