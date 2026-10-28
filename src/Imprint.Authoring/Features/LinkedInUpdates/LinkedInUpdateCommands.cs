using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.LinkedIn;
using Imprint.Authoring.Features.LinkedIn;
using Imprint.Authoring.Projections;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.LinkedInUpdates;

/// <summary>Plan a LinkedIn update on a site: the sender key, the text, the images (media assets with alt text) and
/// the time it goes out.</summary>
public sealed record ScheduleLinkedInUpdate(
    LinkedInUpdateId Id, SiteId SiteId, string Sender, string Text, IReadOnlyList<LinkedInUpdateImage> Images,
    DateTimeOffset PublishAt) : ICommand;

/// <summary>Change an update that has not gone out (or was missed, or refused for good): it is scheduled again.</summary>
public sealed record EditLinkedInUpdate(
    LinkedInUpdateId Id, string Sender, string Text, IReadOnlyList<LinkedInUpdateImage> Images, DateTimeOffset PublishAt) : ICommand;

public sealed record CancelLinkedInUpdate(LinkedInUpdateId Id) : ICommand;

/// <summary>A person's answer after an unknown result: the post's URN if it is on LinkedIn, or null if it is not.</summary>
public sealed record ResolveLinkedInUpdate(LinkedInUpdateId Id, string? Urn) : ICommand;

/// <summary>Record that an update is about to be sent: always BEFORE the call to LinkedIn.</summary>
public sealed record StartLinkedInUpdate(LinkedInUpdateId Id, string TextSha256, DateTimeOffset At) : ICommand;

public sealed record RecordLinkedInUpdateOutcome(LinkedInUpdateId Id, LinkedInSendResult Result, DateTimeOffset At) : ICommand;

public sealed record MissLinkedInUpdate(LinkedInUpdateId Id, string Reason, DateTimeOffset At) : ICommand;

/// <summary>The checks that need read models: the sender is one of the site's, and every image is in the library.
/// Both may change after the check (a sender removed, an asset deleted); the scheduler checks again when it sends.</summary>
internal static class LinkedInUpdateChecks
{
    public static string? Problem(SiteOverview sites, AssetLibrary assets, SiteId siteId, string sender, IReadOnlyList<LinkedInUpdateImage> images)
    {
        var site = sites.Get(siteId);
        if (site is null)
        {
            return "There is no such site.";
        }

        var key = (sender ?? "").Trim().ToLowerInvariant();
        if (site.LinkedInSenders.All(s => s.Key != key))
        {
            var known = site.LinkedInSenders.Count == 0
                ? "none is set up on this site yet"
                : "use one of: " + string.Join(", ", site.LinkedInSenders.Select(s => s.Key));
            return $"'{sender}' is not a LinkedIn sender of this site ({known}).";
        }

        var missing = (images ?? []).FirstOrDefault(i => assets.Get(i.AssetId) is null);
        return missing is null ? null : $"Image {missing.AssetId.Compact} is not in the media library.";
    }
}

public sealed class ScheduleLinkedInUpdateHandler(IAggregateStore store, SiteOverview sites, AssetLibrary assets)
    : ICommandHandler<ScheduleLinkedInUpdate>
{
    public async Task<Result> Handle(ScheduleLinkedInUpdate cmd, CancellationToken ct)
    {
        if (LinkedInUpdateChecks.Problem(sites, assets, cmd.SiteId, cmd.Sender, cmd.Images) is { } problem)
        {
            return Result.Fail(problem);
        }

        var update = LinkedInUpdate.Schedule(cmd.Id, cmd.SiteId, cmd.Sender, cmd.Text, cmd.Images, cmd.PublishAt, TimeProvider.System.GetUtcNow());
        await store.Save(update, ct);
        return Result.Ok();
    }
}

public sealed class EditLinkedInUpdateHandler(IAggregateStore store, SiteOverview sites, AssetLibrary assets)
    : ICommandHandler<EditLinkedInUpdate>
{
    public async Task<Result> Handle(EditLinkedInUpdate cmd, CancellationToken ct)
    {
        var update = await store.Load<LinkedInUpdate>(cmd.Id.Stream, ct);
        if (LinkedInUpdateChecks.Problem(sites, assets, update.SiteId, cmd.Sender, cmd.Images) is { } problem)
        {
            return Result.Fail(problem);
        }

        update.Edit(cmd.Sender, cmd.Text, cmd.Images, cmd.PublishAt, TimeProvider.System.GetUtcNow());
        await store.Save(update, ct);
        return Result.Ok();
    }
}

public sealed class CancelLinkedInUpdateHandler(IAggregateStore store) : ICommandHandler<CancelLinkedInUpdate>
{
    public async Task<Result> Handle(CancelLinkedInUpdate cmd, CancellationToken ct)
    {
        var update = await store.Load<LinkedInUpdate>(cmd.Id.Stream, ct);
        update.Cancel(TimeProvider.System.GetUtcNow());
        await store.Save(update, ct);
        return Result.Ok();
    }
}

public sealed class ResolveLinkedInUpdateHandler(IAggregateStore store) : ICommandHandler<ResolveLinkedInUpdate>
{
    public async Task<Result> Handle(ResolveLinkedInUpdate cmd, CancellationToken ct)
    {
        var update = await store.Load<LinkedInUpdate>(cmd.Id.Stream, ct);
        update.Resolve(cmd.Urn, TimeProvider.System.GetUtcNow());
        await store.Save(update, ct);
        return Result.Ok();
    }
}

public sealed class StartLinkedInUpdateHandler(IAggregateStore store) : ICommandHandler<StartLinkedInUpdate>
{
    public async Task<Result> Handle(StartLinkedInUpdate cmd, CancellationToken ct)
    {
        var update = await store.Load<LinkedInUpdate>(cmd.Id.Stream, ct);
        update.Attempt(cmd.TextSha256, cmd.At);
        await store.Save(update, ct);
        return Result.Ok();
    }
}

public sealed class RecordLinkedInUpdateOutcomeHandler(IAggregateStore store) : ICommandHandler<RecordLinkedInUpdateOutcome>
{
    public async Task<Result> Handle(RecordLinkedInUpdateOutcome cmd, CancellationToken ct)
    {
        var update = await store.Load<LinkedInUpdate>(cmd.Id.Stream, ct);
        var result = cmd.Result;
        switch (result.Status)
        {
            case LinkedInSendStatus.Posted when result.Urn is { Length: > 0 } urn:
                update.Posted(urn, cmd.At);
                break;
            case LinkedInSendStatus.Refused:
                update.Failed(result.Reason ?? "LinkedIn refused the update.", result.Retryable, cmd.At,
                    result.RetryAfter is { } wait ? cmd.At + wait : null);
                break;
            default:
                // Posted without a URN cannot be proven or pointed at: treat it as unknown.
                update.Unknown(result.Reason ?? "LinkedIn answered without saying which post it created.", cmd.At);
                break;
        }

        await store.Save(update, ct);
        return Result.Ok();
    }
}

public sealed class MissLinkedInUpdateHandler(IAggregateStore store) : ICommandHandler<MissLinkedInUpdate>
{
    public async Task<Result> Handle(MissLinkedInUpdate cmd, CancellationToken ct)
    {
        var update = await store.Load<LinkedInUpdate>(cmd.Id.Stream, ct);
        update.Miss(cmd.Reason, cmd.At);
        await store.Save(update, ct);
        return Result.Ok();
    }
}
