using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Schedules;
using Imprint.Authoring.Domain.Schedules.Events;
using Imprint.TestKit;

namespace Imprint.Authoring.Tests.Domain.Schedules;

/// <summary>
/// When a page goes live, decided ahead: a date and a time, changeable and cancellable while it waits,
/// and an outcome once the moment has passed (published, or failed with the reason). It lives in its
/// own stream so that scheduling a page is not an edit to the page.
/// </summary>
public sealed class PageScheduleTests
{
    private static readonly PageId Page = PageId.New();
    private static readonly SiteId Site = SiteId.New();
    private static readonly DateTimeOffset Monday9 = new(2026, 11, 2, 9, 0, 0, TimeSpan.FromHours(1));

    [Fact]
    public void Scheduling_records_the_page_and_the_instant()
    {
        var schedule = PageSchedule.For(Page, Site, Monday9);

        Assert.Equal(new PageScheduleSet(Page, Site, Monday9), Assert.Single(schedule.UncommittedEvents));
        Assert.Equal(Monday9, schedule.At);
        Assert.Equal(PageScheduleState.Pending, schedule.State);
        Assert.Equal($"schedule-page-{Page.Value:N}", schedule.StreamId);
    }

    [Fact]
    public void A_pending_schedule_can_be_moved() =>
        AggregateSpec.For<PageSchedule>()
            .Given(new PageScheduleSet(Page, Site, Monday9))
            .When(s => s.Move(Monday9.AddDays(1)))
            .ThenRaised(new PageScheduleSet(Page, Site, Monday9.AddDays(1)));

    [Fact]
    public void Moving_to_the_same_instant_records_nothing() =>
        AggregateSpec.For<PageSchedule>()
            .Given(new PageScheduleSet(Page, Site, Monday9))
            .When(s => s.Move(Monday9))
            .ThenNothing();

    [Fact]
    public void A_pending_schedule_can_be_cancelled()
    {
        var outcome = AggregateSpec.For<PageSchedule>()
            .Given(new PageScheduleSet(Page, Site, Monday9))
            .When(s => s.Cancel());

        outcome.ThenRaised(new PageScheduleCancelled());
        Assert.Equal(PageScheduleState.Cancelled, outcome.Aggregate.State);
    }

    [Fact]
    public void A_cancelled_schedule_can_be_set_again()
    {
        var outcome = AggregateSpec.For<PageSchedule>()
            .Given(new PageScheduleSet(Page, Site, Monday9), new PageScheduleCancelled())
            .When(s => s.Move(Monday9.AddDays(7)));

        outcome.ThenRaised(new PageScheduleSet(Page, Site, Monday9.AddDays(7)));
        Assert.Equal(PageScheduleState.Pending, outcome.Aggregate.State);
    }

    [Fact]
    public void Due_means_pending_and_the_moment_has_passed()
    {
        var schedule = new PageSchedule();
        schedule.LoadFrom([new PageScheduleSet(Page, Site, Monday9)]);

        Assert.False(schedule.IsDueAt(Monday9.AddSeconds(-1)));
        Assert.True(schedule.IsDueAt(Monday9));
    }

    [Fact]
    public void Fulfilling_records_when_it_went_out_and_ends_the_wait()
    {
        var outcome = AggregateSpec.For<PageSchedule>()
            .Given(new PageScheduleSet(Page, Site, Monday9))
            .When(s => s.Fulfil(Monday9.AddSeconds(20)));

        outcome.ThenRaised(new PageScheduleFulfilled(Monday9.AddSeconds(20)));
        Assert.Equal(PageScheduleState.Published, outcome.Aggregate.State);
        Assert.False(outcome.Aggregate.IsDueAt(Monday9.AddDays(1)));
    }

    [Fact]
    public void A_failed_publish_records_the_reason_and_ends_the_wait()
    {
        var outcome = AggregateSpec.For<PageSchedule>()
            .Given(new PageScheduleSet(Page, Site, Monday9))
            .When(s => s.Fail("There are no changes to publish."));

        outcome.ThenRaised(new PageScheduleFailed("There are no changes to publish."));
        Assert.Equal(PageScheduleState.Failed, outcome.Aggregate.State);
        Assert.Equal("There are no changes to publish.", outcome.Aggregate.FailureReason);
    }

    [Fact]
    public void A_schedule_that_already_went_out_cannot_be_cancelled() =>
        AggregateSpec.For<PageSchedule>()
            .Given(new PageScheduleSet(Page, Site, Monday9), new PageScheduleFulfilled(Monday9))
            .When(s => s.Cancel())
            .ThenFails("already");

    [Fact]
    public void A_page_that_went_out_can_be_scheduled_again_for_its_next_publish()
    {
        var outcome = AggregateSpec.For<PageSchedule>()
            .Given(new PageScheduleSet(Page, Site, Monday9), new PageScheduleFulfilled(Monday9))
            .When(s => s.Move(Monday9.AddDays(7)));

        outcome.ThenRaised(new PageScheduleSet(Page, Site, Monday9.AddDays(7)));
        Assert.Equal(PageScheduleState.Pending, outcome.Aggregate.State);
    }
}
