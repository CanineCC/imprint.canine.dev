using Imprint.Authoring.Domain.Schedules.Events;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Domain.Schedules;

public enum PageScheduleState
{
    /// <summary>Waiting for its moment.</summary>
    Pending,

    /// <summary>Cancelled before its moment.</summary>
    Cancelled,

    /// <summary>The scheduler published the page.</summary>
    Published,

    /// <summary>The moment came and the publish was refused; see the reason.</summary>
    Failed,
}

/// <summary>
/// When a page goes live, decided ahead. The scheduler publishes the page AS IT STANDS at that moment,
/// through the ordinary publish command, so every rule a pressed button obeys applies to it too.
/// </summary>
public sealed class PageSchedule : AggregateRoot
{
    public PageId PageId { get; private set; }
    public SiteId SiteId { get; private set; }
    public DateTimeOffset At { get; private set; }
    public PageScheduleState State { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }
    public string? FailureReason { get; private set; }

    public override string StreamId => StreamOf(PageId);

    public static string StreamOf(PageId page) => $"schedule-page-{page.Value:N}";

    public static PageSchedule For(PageId page, SiteId site, DateTimeOffset at)
    {
        var schedule = new PageSchedule();
        schedule.Raise(new PageScheduleSet(page, site, at));
        return schedule;
    }

    public bool IsDueAt(DateTimeOffset now) => State is PageScheduleState.Pending && At <= now;

    /// <summary>Sets a new moment: moves a waiting schedule, or starts a new wait after the last one ended.</summary>
    public void Move(DateTimeOffset at)
    {
        if (State is PageScheduleState.Pending && at == At)
        {
            return;
        }

        Raise(new PageScheduleSet(PageId, SiteId, at));
    }

    public void Cancel()
    {
        if (State is not PageScheduleState.Pending)
        {
            throw new DomainException("This schedule is not waiting any more: it has already gone out, failed or been cancelled.");
        }

        Raise(new PageScheduleCancelled());
    }

    public void Fulfil(DateTimeOffset publishedAt)
    {
        EnsurePending();
        Raise(new PageScheduleFulfilled(publishedAt));
    }

    public void Fail(string reason)
    {
        EnsurePending();
        Raise(new PageScheduleFailed(string.IsNullOrWhiteSpace(reason) ? "The publish was refused." : reason.Trim()));
    }

    private void EnsurePending()
    {
        if (State is not PageScheduleState.Pending)
        {
            throw new DomainException("This schedule is not waiting any more: it has already gone out, failed or been cancelled.");
        }
    }

    protected override void When(object @event)
    {
        switch (@event)
        {
            case PageScheduleSet e:
                PageId = e.PageId;
                SiteId = e.SiteId;
                At = e.At;
                State = PageScheduleState.Pending;
                PublishedAt = null;
                FailureReason = null;
                break;
            case PageScheduleCancelled:
                State = PageScheduleState.Cancelled;
                break;
            case PageScheduleFulfilled e:
                State = PageScheduleState.Published;
                PublishedAt = e.PublishedAt;
                break;
            case PageScheduleFailed e:
                State = PageScheduleState.Failed;
                FailureReason = e.Reason;
                break;
            default:
                throw new InvalidOperationException($"PageSchedule cannot fold {@event.GetType().Name}.");
        }
    }
}
