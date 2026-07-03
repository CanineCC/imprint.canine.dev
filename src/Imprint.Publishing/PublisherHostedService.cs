using Imprint.EventSourcing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Imprint.Publishing;

/// <summary>
/// Runs the file-system projection: one full synchronize at startup (the output may
/// have been deleted or the events replayed), then a debounced synchronize whenever
/// the projection engine catches up on new events — debounced so a theme-editing
/// session re-renders once, not per keystroke. The publisher never throws at the
/// editor: failures are logged and land in the report/manifest, and the next event
/// simply tries again.
/// </summary>
public sealed class PublisherHostedService(
    SitePublisher publisher,
    ProjectionEngine projections,
    PublishingOptions options,
    ILogger<PublisherHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // One pending-work token is enough: N catch-ups during a pass still mean
        // exactly one more pass.
        var pending = new SemaphoreSlim(0);
        void OnCaughtUp(long _)
        {
            if (pending.CurrentCount == 0)
            {
                pending.Release();
            }
        }

        projections.CaughtUp += OnCaughtUp;
        try
        {
            await TrySynchronize(stoppingToken);
            var debounce = TimeSpan.FromMilliseconds(Math.Max(0, options.DebounceMilliseconds));
            while (!stoppingToken.IsCancellationRequested)
            {
                await pending.WaitAsync(stoppingToken);

                // Quiet-period debounce: every further catch-up inside the window
                // restarts the wait; publish only once the events stop arriving.
                while (await pending.WaitAsync(debounce, stoppingToken))
                {
                }

                await TrySynchronize(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
        finally
        {
            projections.CaughtUp -= OnCaughtUp;
        }
    }

    private async Task TrySynchronize(CancellationToken ct)
    {
        try
        {
            await publisher.Synchronize(ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            // Never out of the service: publishing is a projection, and a projection
            // failure must not take the editing plane down with it.
            logger.LogError(e, "Publishing failed; will retry on the next change.");
        }
    }
}
