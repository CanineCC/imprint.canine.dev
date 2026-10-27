using Imprint.EventSourcing;

namespace Imprint.Authoring.Domain.Schedules.Events;

// A page's publish schedule, kept in its own stream (schedule-page-{id}) so that deciding WHEN a page
// goes live is not an edit to the page: the page's version, and its "Modified" badge, move only when
// its content does.

/// <summary>The page goes live at <paramref name="At"/> (absolute); set, or moved, while it waits.</summary>
[EventType("page-schedule.set")]
public sealed record PageScheduleSet(PageId PageId, SiteId SiteId, DateTimeOffset At);

[EventType("page-schedule.cancelled")]
public sealed record PageScheduleCancelled;

/// <summary>The scheduler published the page at <paramref name="PublishedAt"/>.</summary>
[EventType("page-schedule.fulfilled")]
public sealed record PageScheduleFulfilled(DateTimeOffset PublishedAt);

/// <summary>The moment came and the publish was refused; the reason is the publish command's own.</summary>
[EventType("page-schedule.failed")]
public sealed record PageScheduleFailed(string Reason);
