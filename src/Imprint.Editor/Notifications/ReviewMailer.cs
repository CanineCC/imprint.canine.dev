using Imprint.Authoring.Domain.Posts;
using Imprint.Authoring.Projections;
using Imprint.EventSourcing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Imprint.Editor.Notifications;

/// <summary>
/// Tells the reviewer that something is waiting for them. Driven by the event log rather than by
/// the button that was pressed, so a submission made through the authoring API or a script sends
/// the same mail as one made in the editor.
///
/// <para>The cursor starts at the position the log had reached when this service started — after
/// the startup replay, which folds every historical submission through
/// <see cref="PostReviewQueue"/>. Without that watermark, every restart would mail the reviewer
/// about every post ever submitted.</para>
/// </summary>
public sealed class ReviewMailer(
    ProjectionEngine projections,
    PostReviewQueue queue,
    PostList posts,
    SiteOverview sites,
    SmtpRelay relay,
    IConfiguration configuration,
    ILogger<ReviewMailer> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(15);

    private long _cursor;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Everything already in the log is history, not news.
        _cursor = projections.Position;
        logger.LogInformation("Review notifications start from log position {Position}.", _cursor);

        using var timer = new PeriodicTimer(Interval);
        while (await SafeWait(timer, stoppingToken))
        {
            try
            {
                await Notify(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Review notification pass failed; retrying at the next tick.");
            }
        }
    }

    private static async Task<bool> SafeWait(PeriodicTimer timer, CancellationToken ct)
    {
        try
        {
            return await timer.WaitForNextTickAsync(ct);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private async Task Notify(CancellationToken ct)
    {
        foreach (var request in queue.Since(_cursor))
        {
            // The cursor advances whether or not the mail lands. A relay that is down would
            // otherwise re-send the same notice every fifteen seconds until it comes back — and
            // the reviewer's inbox is not a retry queue. The submission is visible in the editor
            // regardless, which is the durable half of "you have something to review".
            _cursor = request.Position;

            if (posts.Get(request.PostId) is not { } post || sites.Get(post.SiteId) is not { } site)
            {
                continue;
            }

            if (site.ReviewerEmail is not { Length: > 0 } reviewer)
            {
                logger.LogWarning(
                    "Post '{Slug}' was submitted for review but '{Site}' has no reviewer configured.",
                    post.Slug.Value, site.Name);
                continue;
            }

            var title = post.Title.Resolve(site.DefaultLocale, site.DefaultLocale);
            await relay.Send(
                [reviewer],
                $"[{site.Name}] Review requested: {title}",
                Body(request, title, site.Name),
                replyTo: request.SubmittedBy.Contains('@') ? request.SubmittedBy : null,
                ct);
        }
    }

    private string Body(PostReviewRequest request, string title, string siteName)
    {
        var link = $"{(configuration["ImprintBaseUrl"] ?? "").TrimEnd('/')}/posts/{request.PostId.Compact}";
        var when = request.ProposedPublishAt is { } at
            ? $"{EditorialTime.ForAuthor(at)} ({EditorialTime.Zone.Id})"
            : "to be decided — you choose";

        return $"""
            {request.SubmittedBy} has sent a post on {siteName} for public relations review.

            Post:            {title}
            Proposed go-live: {when}
            {(string.IsNullOrWhiteSpace(request.Note) ? "" : $"Note from the author: {request.Note}\n")}
            Open it to read it, set or change the date, and approve or send it back:

                {link}

            Nothing is published until you approve it. Approving without a date keeps it waiting
            until someone sets one.
            """;
    }
}
