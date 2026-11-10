using System.Security.Cryptography;
using System.Text;
using Imprint.Authoring.Domain.LinkedIn;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Features.Assets;
using Imprint.Authoring.Features.LinkedIn;
using Imprint.Authoring.Projections;
using Imprint.EventSourcing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Imprint.Authoring.Features.LinkedInUpdates;

/// <summary>
/// Sends each LinkedIn update at its time, as its sender, with its images, so a plan made weeks ahead goes out
/// without anyone being awake for it (LinkedIn's API cannot hold a post for a later date itself).
///
/// <para>At most once, as for an article's post: the attempt is recorded before LinkedIn is called, and a result
/// that is not known is never sent again on its own. An update more than <see cref="LinkedInUpdateOptions.MaxLate"/>
/// past its time is held back as missed instead: a post written for a Tuesday morning should not appear on Friday
/// because the server was down.</para>
///
/// <para>An update's first comment goes out after its post, as the same sender, by the same rules and with a record
/// of its own: whatever happens to the comment, the post that went out stays Posted and is never sent again.</para>
/// </summary>
public sealed class DueLinkedInUpdatePoster(
    ICommandDispatcher dispatcher,
    ProjectionEngine projections,
    LinkedInUpdateList updates,
    SiteOverview sites,
    AssetLibrary assets,
    IMediaStore media,
    ILinkedInUpdatePoster linkedIn,
    LinkedInUpdateOptions options,
    ILogger<DueLinkedInUpdatePoster> logger,
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
                logger.LogError(ex, "LinkedIn update pass failed; retrying at the next tick.");
            }

            if (!await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                return;
            }
        }
    }

    public async Task PostDueAsync(CancellationToken ct)
    {
        await projections.CatchUp(ct);
        var now = _clock.GetUtcNow();
        foreach (var update in updates.All().Where(u => u.MaySendAt(now)))
        {
            var site = sites.Get(update.SiteId);
            var sender = site?.LinkedInSenders.FirstOrDefault(s => s.Key == update.Sender);
            var problem = site is { IsArchived: true }
                ? $"The site '{site.Name}' is archived: restore it, or cancel the update."
                : sender is null
                ? $"Sender '{update.Sender}' is no longer one of the site's LinkedIn senders."
                : !linkedIn.IsConfiguredFor(sender.OrganizationUrn)
                    ? $"Sender '{update.Sender}' is not connected to LinkedIn (or the connection has expired): connect it in the editor."
                    : null;

            // A late update is missed whatever else is wrong with it; within the window, a problem may still be
            // fixed (a connection renewed), so it waits.
            if (update.State is LinkedInUpdateState.Scheduled && now - update.PublishAt > options.MaxLate)
            {
                await Miss(update, problem ?? $"Its time passed more than {options.MaxLate.TotalHours:0} hours ago: give it a new time.", now);
                continue;
            }

            if (problem is not null)
            {
                logger.LogWarning("LinkedIn update {Id} waits: {Problem}", update.Id, problem);
                continue;
            }

            if (await Images(update, ct) is not { } images)
            {
                await Miss(update, "An image is no longer in the media library: choose the images again.", now);
                continue;
            }

            await Send(update, sender!, images, now, ct);
        }

        // Read after the posts above were recorded: a post that went out in this pass has its first comment sent
        // straight after it. A comment still waiting from an earlier pass (the server stopped in between, or a person
        // resolved an unclear post as posted), or refused with a wait, goes out here too.
        foreach (var update in updates.All().Where(u => u.MayCommentAt(now)))
        {
            await Comment(update, now, ct);
        }
    }

    private async Task Comment(LinkedInUpdateRow update, DateTimeOffset now, CancellationToken ct)
    {
        var site = sites.Get(update.SiteId);
        var sender = site?.LinkedInSenders.FirstOrDefault(s => s.Key == update.Sender);
        var problem = site is { IsArchived: true }
            ? $"The site '{site.Name}' is archived."
            : sender is null
            ? $"Sender '{update.Sender}' is no longer one of the site's LinkedIn senders."
            : !linkedIn.IsConfiguredFor(sender.OrganizationUrn)
                ? $"Sender '{update.Sender}' is not connected to LinkedIn (or the connection has expired): connect it in the editor."
                : null;

        // Like the post, the comment keeps to a window: one that cannot go out soon after its post is left for a
        // person, so a comment never turns up under a post hours after anyone expected it.
        if (now - (update.PostedAt ?? now) > options.MaxLate)
        {
            await MissComment(update, (problem is null ? "" : problem + " ") +
                $"Its post went out more than {options.MaxLate.TotalHours:0} hours ago: add the comment by hand if it should be there.", now);
            return;
        }

        if (problem is not null)
        {
            logger.LogWarning("LinkedIn update {Id}'s first comment waits: {Problem}", update.Id, problem);
            return;
        }

        var author = sender!;
        var started = await dispatcher.Dispatch(new StartLinkedInUpdateComment(update.Id, Sha256(update.FirstComment!), now), ct);
        if (!started.Succeeded)
        {
            logger.LogWarning("LinkedIn update {Id}'s first comment not started: {Errors}", update.Id, string.Join("; ", started.Errors));
            return;
        }

        LinkedInSendResult result;
        try
        {
            result = await linkedIn.CommentAsync(new LinkedInCommentRequest(author.OrganizationUrn, update.Urn!, update.FirstComment!), ct);
        }
        catch (Exception ex)
        {
            // Whatever happened, the request may have reached LinkedIn.
            result = new LinkedInSendResult(LinkedInSendStatus.Unknown, Reason: ex.GetType().Name + ": " + ex.Message);
        }

        var recorded = await dispatcher.Dispatch(new RecordLinkedInUpdateCommentOutcome(update.Id, result, _clock.GetUtcNow()), CancellationToken.None);
        logger.LogInformation("LinkedIn update {Id}'s first comment as {Sender}: {Status} {Urn}{Reason}", update.Id, author.Key, result.Status, result.Urn, result.Reason);
        if (!recorded.Succeeded)
        {
            logger.LogError("LinkedIn update {Id}'s first comment outcome not recorded: {Errors}", update.Id, string.Join("; ", recorded.Errors));
        }
    }

    private async Task MissComment(LinkedInUpdateRow update, string reason, DateTimeOffset now)
    {
        var missed = await dispatcher.Dispatch(new MissLinkedInUpdateComment(update.Id, reason, now), CancellationToken.None);
        logger.LogWarning("LinkedIn update {Id}'s first comment missed: {Reason}{Errors}", update.Id, reason,
            missed.Succeeded ? "" : " (not recorded: " + string.Join("; ", missed.Errors) + ")");
    }

    private async Task Send(LinkedInUpdateRow update, LinkedInSender sender, IReadOnlyList<LinkedInUpdateImageFile> images, DateTimeOffset now, CancellationToken ct)
    {
        var started = await dispatcher.Dispatch(new StartLinkedInUpdate(update.Id, Sha256(update), now), ct);
        if (!started.Succeeded)
        {
            logger.LogWarning("LinkedIn update {Id} not started: {Errors}", update.Id, string.Join("; ", started.Errors));
            return;
        }

        LinkedInSendResult result;
        try
        {
            result = await linkedIn.PostUpdateAsync(new LinkedInUpdateRequest(sender.OrganizationUrn, update.Text, images), ct);
        }
        catch (Exception ex)
        {
            // Whatever happened, the request may have reached LinkedIn.
            result = new LinkedInSendResult(LinkedInSendStatus.Unknown, Reason: ex.GetType().Name + ": " + ex.Message);
        }

        // Recorded even when the pass is being cancelled: a known outcome must not be lost.
        var recorded = await dispatcher.Dispatch(new RecordLinkedInUpdateOutcome(update.Id, result, _clock.GetUtcNow()), CancellationToken.None);
        logger.LogInformation("LinkedIn update {Id} as {Sender}: {Status} {Urn}{Reason}", update.Id, sender.Key, result.Status, result.Urn, result.Reason);
        if (!recorded.Succeeded)
        {
            logger.LogError("LinkedIn update {Id} outcome not recorded: {Errors}", update.Id, string.Join("; ", recorded.Errors));
        }
    }

    private async Task Miss(LinkedInUpdateRow update, string reason, DateTimeOffset now)
    {
        var missed = await dispatcher.Dispatch(new MissLinkedInUpdate(update.Id, reason, now), CancellationToken.None);
        logger.LogWarning("LinkedIn update {Id} missed: {Reason}{Errors}", update.Id, reason,
            missed.Succeeded ? "" : " (not recorded: " + string.Join("; ", missed.Errors) + ")");
    }

    private async Task<IReadOnlyList<LinkedInUpdateImageFile>?> Images(LinkedInUpdateRow update, CancellationToken ct)
    {
        var files = new List<LinkedInUpdateImageFile>();
        foreach (var image in update.Images)
        {
            if (assets.Get(image.AssetId) is not { } asset || asset.OriginalStorageKey.Length == 0)
            {
                return null;
            }

            await using var stream = await media.Open(asset.OriginalStorageKey, ct);
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, ct);
            files.Add(new LinkedInUpdateImageFile(buffer.ToArray(), asset.ContentType, image.Alt));
        }

        return files;
    }

    private static string Sha256(LinkedInUpdateRow update) =>
        Sha256(update.Sender + "\n" + update.Text + "\n" + string.Join(",", update.Images.Select(i => i.AssetId.Compact)));

    private static string Sha256(string text) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
}
