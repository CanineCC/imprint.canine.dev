using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Schedules;
using Imprint.Authoring.Projections;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Schedules;

/// <summary>Schedule a page to go live at <paramref name="At"/>, move its schedule, or (null) cancel it.</summary>
public sealed record SchedulePagePublish(PageId PageId, DateTimeOffset? At) : ICommand;

public sealed class SchedulePagePublishHandler(IAggregateStore store, PageList pages) : ICommandHandler<SchedulePagePublish>
{
    public async Task<Result> Handle(SchedulePagePublish cmd, CancellationToken ct)
    {
        if (pages.SiteOf(cmd.PageId) is not { } site)
        {
            return Result.Fail("The page no longer exists.");
        }

        var schedule = await store.LoadOrDefault<PageSchedule>(PageSchedule.StreamOf(cmd.PageId), ct);
        if (cmd.At is not { } at)
        {
            // "No time" is a cancel, and cancelling what is not waiting is simply nothing to do.
            if (schedule is { State: PageScheduleState.Pending })
            {
                schedule.Cancel();
                await store.Save(schedule, ct);
            }

            return Result.Ok();
        }

        if (schedule is null)
        {
            schedule = PageSchedule.For(cmd.PageId, site, at);
        }
        else
        {
            schedule.Move(at);
        }

        await store.Save(schedule, ct);
        return Result.Ok();
    }
}
