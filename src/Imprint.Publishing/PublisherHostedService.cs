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
        // Per-target guard: one site's publish failing (an unwritable folder, a render
        // error) must not stall the sites after it in the loop — publishing is a
        // projection, and a projection failure must not take the editing plane down with
        // it. The next catch-up pass retries the failed site.
        foreach (var target in ResolveTargets())
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                await publisher.Synchronize(target, ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Publishing site {SiteId} failed; will retry on the next change.", target.Site.Id);
            }
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
            // Capture the live environments list ONCE — Site.Environments is a field getter
            // a concurrent projection replay can reassign between a .Count and an index,
            // and this loop runs without the projection engine's gate.
            var environments = site.Environments;
            if (environments.Count > 0)
            {
                var environment = environments[0];
                try
                {
                    // Environment output is portable (root-relative, BaseUrl null): a single
                    // global BaseUrl would be wrong for every site but one (see
                    // SiteDeployService and multi-site-saas.md).
                    targets.Add(new PublishTarget(site, paths.Resolve(environment.Path), BaseUrl: null));
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
                // The legacy single-site fallback keeps the global BaseUrl, which is
                // correct precisely because there is exactly one site in this branch.
                targets.Add(new PublishTarget(site, options.OutputPath, options.BaseUrl));
            }
        }

        return targets;
    }
}
