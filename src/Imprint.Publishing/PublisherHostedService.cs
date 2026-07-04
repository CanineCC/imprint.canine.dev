using Imprint.Authoring.Projections;
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
///
/// Multi-site: each site's published content auto-syncs to its <em>first</em>
/// environment (the promotion pipeline's lowest rung, e.g. "Test"); higher environments
/// are promotion-only and never written here. A site with no environments configured
/// falls back — only if it is the first-created site — to the globally configured
/// <see cref="PublishingOptions.OutputPath"/>, preserving single-site behavior exactly.
/// </summary>
public sealed class PublisherHostedService(
    SitePublisher publisher,
    ProjectionEngine projections,
    SiteOverview siteOverview,
    DeployPathResolver paths,
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
            foreach (var target in ResolveTargets())
            {
                await publisher.Synchronize(target, ct);
            }
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

    /// <summary>
    /// The (site, folder) targets the auto-sync keeps current: each site's first
    /// environment, or the legacy global output for the first site while it has none.
    /// A folder that cannot be resolved (misconfigured, outside the sandbox) is skipped
    /// with a warning — one bad site must not stall the others.
    /// </summary>
    private List<PublishTarget> ResolveTargets()
    {
        var sites = siteOverview.All;
        var currentId = siteOverview.Current?.Id;
        var targets = new List<PublishTarget>(sites.Count);
        foreach (var site in sites)
        {
            if (site.Environments.Count > 0)
            {
                var environment = site.Environments[0];
                try
                {
                    targets.Add(new PublishTarget(site, paths.Resolve(environment.Path), options.BaseUrl));
                }
                catch (Exception e) when (e is InvalidOperationException or ArgumentException)
                {
                    logger.LogWarning(
                        e, "Skipping auto-sync of site {SiteId} environment '{Environment}': {Reason}",
                        site.Id, environment.Name, e.Message);
                }
            }
            else if (site.Id == currentId && !string.IsNullOrWhiteSpace(options.OutputPath))
            {
                targets.Add(new PublishTarget(site, options.OutputPath, options.BaseUrl));
            }
        }

        return targets;
    }
}
