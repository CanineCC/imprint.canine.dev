using Imprint.Authoring.Domain.Posts;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Projections;
using Imprint.EventSourcing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Imprint.Authoring.Features.LinkedIn;

/// <summary>
/// Posts each article's LinkedIn text when the article goes live, so a scheduled publish and its announcement
/// happen together without anyone being awake for it.
///
/// <para>What goes out is the published snapshot, never the draft: the text and sender as they were when the
/// article was published, and only when that publish was signed (setting the text lapses the approval, so a
/// signed publish covers it). The link is the article on the site's Production address.</para>
///
/// <para>At most once: the attempt is recorded before LinkedIn is called, because LinkedIn has no idempotency
/// key. A result that is not known (a timeout, a server error, a crash between the two records) is never sent
/// again on its own; a person checks the page and resolves it.</para>
/// </summary>
public sealed class DueLinkedInPoster(
    ICommandDispatcher dispatcher,
    ProjectionEngine projections,
    PublishedPosts published,
    SiteOverview sites,
    LinkedInPosts linkedInPosts,
    ILinkedInPoster linkedIn,
    LinkedInPostingOptions options,
    ILogger<DueLinkedInPoster> logger,
    TimeProvider? clock = null) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    private readonly TimeProvider _clock = clock ?? TimeProvider.System;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PostDueAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "LinkedIn pass failed; retrying at the next tick.");
            }

            if (!await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                return;
            }
        }
    }

    public async Task PostDueAsync(CancellationToken ct)
    {
        if (!linkedIn.IsConfigured)
        {
            return;
        }

        await projections.CatchUp(ct);
        var now = _clock.GetUtcNow();

        foreach (var site in sites.Active)
        {
            var baseUrl = ProductionBaseUrl(site);
            if (baseUrl is null)
            {
                continue;
            }

            var room = options.MaxPerSitePerHour - linkedInPosts.AttemptsSince(site.Id, now - TimeSpan.FromHours(1));
            var due = published.AllForSite(site.Id)
                .Where(post => post.SignedBy is not null
                    && !post.SignedAfterGoingLive
                    && post.LinkedInText is { Length: > 0 }
                    && post.PublishedAt <= now
                    && now - post.PublishedAt <= options.MaxAge
                    && (linkedInPosts.Get(post.Id)?.MaySendAt(now) ?? true))
                .OrderBy(post => post.PublishedAt)
                .ThenBy(post => post.Slug.Value, StringComparer.Ordinal);

            foreach (var post in due)
            {
                if (room <= 0)
                {
                    break;
                }

                var sender = site.LinkedInSenders.FirstOrDefault(s => s.Key == post.LinkedInSender);
                if (sender is null)
                {
                    logger.LogWarning("'{Slug}' names LinkedIn sender '{Sender}', which the site no longer has.",
                        post.Slug.Value, post.LinkedInSender);
                    continue;
                }

                room--;
                await Send(site, sender, post, baseUrl, now, ct);
            }
        }
    }

    private async Task Send(Site site, LinkedInSender sender, PublishedPost post, string baseUrl, DateTimeOffset now, CancellationToken ct)
    {
        var url = $"{baseUrl}/{PostPrefix(site.Kind)}{post.Slug.Value}/";
        var started = await dispatcher.Dispatch(new StartLinkedInPost(
            post.Id, site.Id, sender.Key, Post.LinkedInSha256(sender.Key, post.LinkedInText)!, url, now), ct);
        if (!started.Succeeded)
        {
            logger.LogWarning("LinkedIn post for '{Slug}' not started: {Errors}", post.Slug.Value, string.Join("; ", started.Errors));
            return;
        }

        var request = new LinkedInPostRequest(
            sender.OrganizationUrn,
            post.LinkedInText!,
            url,
            post.Title.Resolve(site.DefaultLocale, site.DefaultLocale),
            post.MetaDescription.Resolve(site.DefaultLocale, site.DefaultLocale));

        LinkedInSendResult result;
        try
        {
            result = await linkedIn.PostAsync(request, ct);
        }
        catch (Exception ex)
        {
            // Whatever happened, the request may have reached LinkedIn.
            result = new LinkedInSendResult(LinkedInSendStatus.Unknown, Reason: ex.GetType().Name + ": " + ex.Message);
        }

        // Recorded even when the pass is being cancelled: an open attempt with no outcome reads as unknown anyway,
        // but a known outcome must not be lost.
        var recorded = await dispatcher.Dispatch(new RecordLinkedInOutcome(post.Id, result, _clock.GetUtcNow()), CancellationToken.None);
        logger.LogInformation("LinkedIn post for '{Slug}' as {Sender}: {Status} {Urn}{Reason}",
            post.Slug.Value, sender.Key, result.Status, result.Urn, result.Reason);
        if (!recorded.Succeeded)
        {
            logger.LogError("LinkedIn outcome for '{Slug}' not recorded: {Errors}", post.Slug.Value, string.Join("; ", recorded.Errors));
        }
    }

    /// <summary>The Production environment's public origin, without a trailing slash; null when none is set.</summary>
    private static string? ProductionBaseUrl(Site site) =>
        site.Environments
            .FirstOrDefault(e => string.Equals(e.Name, "Production", StringComparison.OrdinalIgnoreCase))
            ?.BaseUrl?.TrimEnd('/') is { Length: > 0 } url ? url : null;

    // The same rule as Imprint.Publishing.BlogPaths.PostPrefix (which this assembly cannot reference): a post
    // stream serves its posts at the root, a site with a blog under blog/.
    private static string PostPrefix(SiteKind kind) => kind.IsPostStream() ? "" : "blog/";
}
