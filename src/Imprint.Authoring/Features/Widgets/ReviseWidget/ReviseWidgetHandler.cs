using Imprint.Authoring.Domain.Widgets;
using Imprint.Authoring.Projections;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Widgets.ReviseWidget;

public sealed class ReviseWidgetHandler(IAggregateStore store, WidgetRegistry registry)
    : ICommandHandler<ReviseWidget>
{
    public async Task<Result> Handle(ReviseWidget cmd, CancellationToken ct)
    {
        // Same documented race as SubmitWidget: only an approved tag owned by a *different*
        // submission is a collision (the revised submission is pending/rejected, so its own
        // tag is never in the approved set, but exclude it by id to be exact). Built-in
        // collisions are the merged catalog's concern, not this slice's.
        if (registry.Approved.Any(s => s.Tag == cmd.Tag && !s.Id.Equals(cmd.WidgetSubmissionId)))
        {
            return Result.Fail($"The tag '{cmd.Tag}' is already used by an approved widget. Choose a different tag.");
        }

        var submission = await store.Load<WidgetSubmission>(cmd.WidgetSubmissionId.Stream, ct);
        submission.Revise(
            cmd.Tag, cmd.Name, cmd.Description, cmd.Placeholder,
            cmd.AspectRatio, cmd.Eager, cmd.Props, cmd.BundleSource);
        await store.Save(submission, ct);
        return Result.Ok();
    }
}
