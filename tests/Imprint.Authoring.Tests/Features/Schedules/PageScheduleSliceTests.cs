using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Schedules;
using Imprint.Authoring.Features.Pages.CreatePage;
using Imprint.Authoring.Features.Pages.PublishPage;
using Imprint.Authoring.Features.Schedules;
using Imprint.Authoring.Projections;
using Imprint.Authoring.Tests.Features.Pages;
using Imprint.EventSourcing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Imprint.Authoring.Tests.Features.Schedules;

/// <summary>
/// A page scheduled for a date and a time goes live when the moment passes, through the ordinary
/// publish command, and the schedule says how it went. Scheduling is not an edit to the page.
/// </summary>
public sealed class PageScheduleSliceTests
{
    private static readonly DateTimeOffset Monday9 = new(2026, 11, 2, 9, 0, 0, TimeSpan.FromHours(1));

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;
        public override DateTimeOffset GetUtcNow() => Now.ToUniversalTime();
    }

    private static async Task<(AuthoringTestHost Host, PageId Page)> APage()
    {
        var host = PagesHost.Create();
        var site = await PagesHost.SeedSite(host);
        var page = PageId.New();
        await host.Ok(new CreatePage(page, site, "Launch", "launch", "en"));
        await PagesHost.AddPreset(host, page, 0, "hero");
        return (host, page);
    }

    private static DuePagePublisher Scheduler(AuthoringTestHost host, FixedClock clock) =>
        new(host.Dispatcher, host.Get<ProjectionEngine>(), host.Get<ScheduledPages>(),
            NullLogger<DuePagePublisher>.Instance, clock);

    [Fact]
    public async Task A_scheduled_page_waits_and_is_not_an_edit_to_the_page()
    {
        var (host, page) = await APage();
        await using var _ = host;
        var versionBefore = host.Get<PageList>().Get(page)!.Version;

        await host.Ok(new SchedulePagePublish(page, Monday9));

        var scheduled = host.Get<ScheduledPages>().Get(page)!;
        Assert.Equal(Monday9, scheduled.At);
        Assert.Equal(PageScheduleState.Pending, scheduled.State);
        Assert.Equal(versionBefore, host.Get<PageList>().Get(page)!.Version);
    }

    [Fact]
    public async Task The_scheduler_publishes_the_page_when_its_moment_passes_and_not_before()
    {
        var (host, page) = await APage();
        await using var _ = host;
        await host.Ok(new SchedulePagePublish(page, Monday9));
        var clock = new FixedClock(Monday9.AddMinutes(-1));
        var scheduler = Scheduler(host, clock);

        await scheduler.PublishDueAsync(CancellationToken.None);
        Assert.Equal(PageStatus.Draft, host.Get<PageList>().Get(page)!.Status);

        clock.Now = Monday9.AddSeconds(20);
        await scheduler.PublishDueAsync(CancellationToken.None);

        Assert.Equal(PageStatus.Published, host.Get<PageList>().Get(page)!.Status);
        var done = host.Get<ScheduledPages>().Get(page)!;
        Assert.Equal(PageScheduleState.Published, done.State);
        Assert.Equal(Monday9.AddSeconds(20), done.PublishedAt);
    }

    [Fact]
    public async Task A_refused_publish_is_recorded_with_its_reason_and_not_retried()
    {
        var (host, page) = await APage();
        await using var _ = host;
        await host.Ok(new PublishPage(page));
        await host.Ok(new SchedulePagePublish(page, Monday9));
        var clock = new FixedClock(Monday9.AddMinutes(1));

        await Scheduler(host, clock).PublishDueAsync(CancellationToken.None);

        var failed = host.Get<ScheduledPages>().Get(page)!;
        Assert.Equal(PageScheduleState.Failed, failed.State);
        Assert.Contains("no changes to publish", failed.FailureReason, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Scheduling_with_no_instant_cancels_a_waiting_schedule()
    {
        var (host, page) = await APage();
        await using var _ = host;
        await host.Ok(new SchedulePagePublish(page, Monday9));

        await host.Ok(new SchedulePagePublish(page, null));

        Assert.Equal(PageScheduleState.Cancelled, host.Get<ScheduledPages>().Get(page)!.State);
        await Scheduler(host, new FixedClock(Monday9.AddDays(1))).PublishDueAsync(CancellationToken.None);
        Assert.Equal(PageStatus.Draft, host.Get<PageList>().Get(page)!.Status);
    }

    [Fact]
    public async Task Cancelling_a_page_that_was_never_scheduled_does_nothing()
    {
        var (host, page) = await APage();
        await using var _ = host;

        await host.Ok(new SchedulePagePublish(page, null));

        Assert.Null(host.Get<ScheduledPages>().Get(page));
    }

    [Fact]
    public async Task Scheduling_a_page_that_does_not_exist_is_refused()
    {
        await using var host = PagesHost.Create();

        var error = await host.Fails(new SchedulePagePublish(PageId.New(), Monday9));

        Assert.Contains("page", error, StringComparison.OrdinalIgnoreCase);
    }
}
