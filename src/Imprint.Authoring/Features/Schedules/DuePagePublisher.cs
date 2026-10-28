using Imprint.Authoring.Features.Pages.PublishPage;
using Imprint.Authoring.Projections;
using Imprint.EventSourcing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Imprint.Authoring.Features.Schedules;

/// <summary>
/// The clock half of page scheduling: a page with a scheduled moment is published when that moment
/// passes, through the ordinary <see cref="PublishPage"/> command, so a scheduled publish obeys every
/// rule a pressed button does. The outcome is recorded on the schedule (published, or refused with
/// the command's own reason), so the editor can say what happened; a refusal is not retried, because
/// a page that could not be published at 09:00 should not go out unannounced at 09:30.
/// Polling, like <see cref="Posts.PublishDuePosts.DuePostPublisher"/>: a timer set weeks ahead does not
/// survive a deploy.
/// </summary>
public sealed class DuePagePublisher(
    ICommandDispatcher dispatcher,
    ProjectionEngine projections,
    ScheduledPages schedules,
    ILogger<DuePagePublisher> logger,
    TimeProvider? clock = null) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

    private readonly TimeProvider _clock = clock ?? TimeProvider.System;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishDueAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Scheduled-page pass failed; retrying at the next tick.");
            }

            if (!await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                return;
            }
        }
    }

    /// <summary>One pass: publish every page whose moment has passed, and record each outcome.</summary>
    public async Task PublishDueAsync(CancellationToken ct)
    {
        await projections.CatchUp(ct);
        var now = _clock.GetUtcNow();
        foreach (var due in schedules.DueAt(now))
        {
            var result = await dispatcher.Dispatch(new PublishPage(due.PageId), ct);
            var reason = result.Succeeded ? null : string.Join("; ", result.Errors);
            var recorded = await dispatcher.Dispatch(new CompletePageSchedule(due.PageId, result.Succeeded, reason, now), ct);
            if (result.Succeeded)
            {
                logger.LogInformation("Published page {Page} on schedule (due {Due:u}).", due.PageId, due.At);
            }
            else
            {
                logger.LogWarning("Scheduled publish of page {Page} refused: {Reason}", due.PageId, reason);
            }

            if (!recorded.Succeeded)
            {
                logger.LogError("Could not record the outcome of page {Page}'s schedule: {Errors}",
                    due.PageId, string.Join("; ", recorded.Errors));
            }
        }
    }
}
