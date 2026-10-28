using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Schedules.Events;

namespace Imprint.Authoring.Tests.Domain.Schedules;

public sealed class PageScheduleEventSamples : IEventSampleProvider
{
    public IEnumerable<object> Samples
    {
        get
        {
            yield return new PageScheduleSet(PageId.New(), SiteId.New(), new DateTimeOffset(2026, 11, 2, 9, 0, 0, TimeSpan.FromHours(1)));
            yield return new PageScheduleCancelled();
            yield return new PageScheduleFulfilled(new DateTimeOffset(2026, 11, 2, 8, 0, 20, TimeSpan.Zero));
            yield return new PageScheduleFailed("There are no changes to publish.");
        }
    }
}
