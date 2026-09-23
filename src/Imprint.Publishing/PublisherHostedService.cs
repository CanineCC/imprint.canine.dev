using Imprint.Authoring.Projections;
using Imprint.Authoring.Syndication;
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
    SyndicatedPageStore syndicated,
    ExternalContentSignal externalContent,
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
        void Wake()
        {
            if (pending.CurrentCount == 0)
            {
                pending.Release();
            }
        }

        void OnCaughtUp(long _) => Wake();

        projections.CaughtUp += OnCaughtUp;

        // Syndicated pages are not event-sourced, so they never reach the projection engine and would
        // otherwise wait for an unrelated authoring event to carry them out. A producer pushing a few
        // hundred pages lands inside one debounce window and publishes once, which is the same
        // behaviour a theme-editing session already gets.
        syndicated.Changed += Wake;

        // A third source: data a page renders from that lives in another service entirely. The
        // signal carries nothing — the publisher re-fetches and compares hashes — so this is simply
        // one more reason to look, debounced with the rest.
        externalContent.Changed += Wake;
        try
        {
            var stale = await TrySynchronize(stoppingToken);
            var debounce = TimeSpan.FromMilliseconds(Math.Max(0, options.DebounceMilliseconds));
            var attempt = 0;
            while (!stoppingToken.IsCancellationRequested)
            {
                // ★★ A STALE PUBLISH RETRIES ITSELF, and nothing else in this loop does. Every other
                // reason to publish is an EVENT — a page edit, a syndication push, a producer saying
                // "look again" — and an event that already happened will not happen twice. So a publish
                // that could not reach the service it bakes from would otherwise hold the last good
                // content forever and never try again: never blank, but permanently behind. Backing off
                // 1, 2, 4 … to five minutes, this keeps asking until a publish comes back clean.
                var waited = stale > 0
                    ? await pending.WaitAsync(RetryDelay(attempt), stoppingToken)
                    : await WaitForeverAsync(pending, stoppingToken);

                if (waited)
                {
                    // Quiet-period debounce: every further catch-up inside the window
                    // restarts the wait; publish only once the events stop arriving.
                    while (await pending.WaitAsync(debounce, stoppingToken))
                    {
                    }
                }

                var before = stale;
                stale = await TrySynchronize(stoppingToken);
                attempt = stale > 0 && before > 0 ? attempt + 1 : 0;
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
        finally
        {
            projections.CaughtUp -= OnCaughtUp;
            syndicated.Changed -= Wake;
            externalContent.Changed -= Wake;
        }
    }

    /// <summary>How long to wait before asking again after a publish that could not reach something.</summary>
    /// <remarks>★ 1, 2, 4, 8 … capped at five minutes. Short enough that a brief outage costs one
    /// stale interval, long enough that a service down for an afternoon is not hammered.</remarks>
    private static TimeSpan RetryDelay(int attempt) =>
        TimeSpan.FromSeconds(Math.Min(300, Math.Pow(2, Math.Min(attempt, 9))));

    /// <summary>Waits for work with no deadline, and always reports that it waited.</summary>
    private static async Task<bool> WaitForeverAsync(SemaphoreSlim pending, CancellationToken ct)
    {
        await pending.WaitAsync(ct).ConfigureAwait(false);
        return true;
    }

    /// <summary>Publishes every target and returns how many fragments could not be reached.</summary>
    private async Task<int> TrySynchronize(CancellationToken ct)
    {
        // Per-target guard: one site's publish failing (an unwritable folder, a render
        // error) must not stall the sites after it in the loop — publishing is a
        // projection, and a projection failure must not take the editing plane down with
        // it. The next catch-up pass retries the failed site.
        var stale = 0;
        foreach (var target in ResolveTargets())
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var report = await publisher.Synchronize(target, ct);
                stale += report.StaleBakes;
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

        if (stale > 0)
        {
            logger.LogWarning(
                "{Stale} widget fragment(s) could not be fetched; those pages kept the last bake that worked "
                + "and this publish will be retried until every fragment arrives.", stale);
        }

        return stale;
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
                    // The environment's own BaseUrl, when set, makes canonicals/hreflang/
                    // sitemap/robots absolute against that environment's public origin;
                    // unset keeps output root-relative and origin-portable. Never a single
                    // global BaseUrl, which would be wrong for every site but one (see
                    // SiteDeployService and multi-site-saas.md).
                    targets.Add(new PublishTarget(site, paths.Resolve(environment.Path), environment.BaseUrl));
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
