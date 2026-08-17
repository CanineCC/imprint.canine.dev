using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Posts;
using Imprint.Authoring.Projections;
using Imprint.EventSourcing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Imprint.Authoring.Features.Posts.PublishDuePosts;

/// <summary>
/// The clock half of scheduling: a post with an agreed date goes live when that moment passes,
/// without anyone being awake for it.
///
/// <para>It publishes through the ordinary <see cref="PublishPost.PublishPost"/> command, which
/// is the entire point — the review gate, the widget check and the markdown verdict all apply to
/// a scheduled publish exactly as they do to a pressed button. A scheduler that appended
/// <c>post.published</c> itself would be a second, weaker way to publish.</para>
///
/// <para>Polling rather than timers-per-post: a timer set for a date months away does not survive
/// a deploy, and the estate deploys on every push. A minute of lateness on a blog post is not a
/// defect; a post that silently never goes out because the process restarted is.</para>
/// </summary>
public sealed class DuePostPublisher(
    ICommandDispatcher dispatcher,
    ProjectionEngine projections,
    PostList posts,
    SiteOverview sites,
    ILogger<DuePostPublisher> logger,
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
                await PublishDue(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                // One bad pass must not end the schedule for every future post.
                logger.LogError(ex, "Scheduled-post pass failed; retrying at the next tick.");
            }

            if (!await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                return;
            }
        }
    }

    private async Task PublishDue(CancellationToken ct)
    {
        // Read models are caught up first: a post scheduled seconds ago from another circuit is
        // not yet visible here otherwise, and the pass would skip its own moment.
        await projections.CatchUp(ct);
        var now = _clock.GetUtcNow();

        foreach (var site in sites.All)
        {
            foreach (var post in posts.All(site.Id))
            {
                if (!post.IsDueAt(now, site.HasReviewer))
                {
                    continue;
                }

                var result = await dispatcher.Dispatch(new PublishPost.PublishPost(post.Id, site.DefaultLocale.Value), ct);
                if (result.Succeeded)
                {
                    logger.LogInformation(
                        "Published '{Slug}' on schedule (due {Due:u}).", post.Slug.Value, post.PublishAt);
                }
                else
                {
                    // Left scheduled on purpose: the date has passed, so the next pass tries
                    // again, and the editor shows the post still waiting rather than pretending
                    // it went out. A body that stopped converting is the usual cause, and the
                    // author sees the same errors in the editor.
                    logger.LogWarning(
                        "Scheduled publish of '{Slug}' refused: {Errors}",
                        post.Slug.Value, string.Join("; ", result.Errors));
                }
            }
        }
    }
}
