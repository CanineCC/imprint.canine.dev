using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Schedules;
using Imprint.Authoring.Domain.Schedules.Events;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Projections;

/// <summary>One page's publish schedule as the editor and the scheduler see it.</summary>
public sealed record ScheduledPage(
    PageId PageId, SiteId SiteId, DateTimeOffset At, PageScheduleState State,
    DateTimeOffset? PublishedAt = null, string? FailureReason = null)
{
    public bool IsDueAt(DateTimeOffset now) => State is PageScheduleState.Pending && At <= now;
}

/// <summary>Every page's publish schedule (stream <c>schedule-page-{id}</c>): the scheduler's work list.</summary>
public sealed class ScheduledPages : ReadModel
{
    private readonly Dictionary<PageId, ScheduledPage> _schedules = [];

    public ScheduledPage? Get(PageId page) => _schedules.GetValueOrDefault(page);

    /// <summary>The pages whose moment has passed and that are still waiting, earliest first.</summary>
    public IReadOnlyList<ScheduledPage> DueAt(DateTimeOffset now) =>
        [.. _schedules.Values.Where(s => s.IsDueAt(now)).OrderBy(s => s.At)];

    /// <summary>The pages of one site that are waiting, earliest first.</summary>
    public IReadOnlyList<ScheduledPage> PendingFor(SiteId site) =>
        [.. _schedules.Values.Where(s => s.SiteId == site && s.State is PageScheduleState.Pending).OrderBy(s => s.At)];

    public override void Apply(StoredEvent @event)
    {
        if (StreamIds.IdOf(@event.StreamId, "schedule-page-") is not { } guid)
        {
            return;
        }

        var page = PageId.From(guid);
        var current = _schedules.GetValueOrDefault(page);
        ScheduledPage? next = @event.Event switch
        {
            PageScheduleSet set => new ScheduledPage(set.PageId, set.SiteId, set.At, PageScheduleState.Pending),
            PageScheduleCancelled when current is not null => current with { State = PageScheduleState.Cancelled },
            PageScheduleFulfilled done when current is not null =>
                current with { State = PageScheduleState.Published, PublishedAt = done.PublishedAt },
            PageScheduleFailed failed when current is not null =>
                current with { State = PageScheduleState.Failed, FailureReason = failed.Reason },
            _ => current,
        };

        if (next is null)
        {
            return;
        }

        _schedules[page] = next;
        NotifyChanged();
    }

    public override void Reset() => _schedules.Clear();
}
