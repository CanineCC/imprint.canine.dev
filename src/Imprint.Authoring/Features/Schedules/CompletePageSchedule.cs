using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Schedules;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Schedules;

/// <summary>The scheduler's report on a due page: published at <paramref name="At"/>, or refused for <paramref name="Reason"/>.</summary>
public sealed record CompletePageSchedule(PageId PageId, bool Published, string? Reason, DateTimeOffset At) : ICommand;

public sealed class CompletePageScheduleHandler(IAggregateStore store) : ICommandHandler<CompletePageSchedule>
{
    public async Task<Result> Handle(CompletePageSchedule cmd, CancellationToken ct)
    {
        var schedule = await store.Load<PageSchedule>(PageSchedule.StreamOf(cmd.PageId), ct);
        if (cmd.Published)
        {
            schedule.Fulfil(cmd.At);
        }
        else
        {
            schedule.Fail(cmd.Reason ?? "The publish was refused.");
        }

        await store.Save(schedule, ct);
        return Result.Ok();
    }
}
