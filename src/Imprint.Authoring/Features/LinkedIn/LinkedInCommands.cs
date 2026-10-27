using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.LinkedIn;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.LinkedIn;

/// <summary>Record that a post is about to be sent: always BEFORE the call to LinkedIn.</summary>
public sealed record StartLinkedInPost(PostId PostId, SiteId SiteId, string Sender, string TextSha256, string ArticleUrl, DateTimeOffset At) : ICommand;

/// <summary>Record what LinkedIn answered to the open attempt.</summary>
public sealed record RecordLinkedInOutcome(PostId PostId, LinkedInSendResult Result, DateTimeOffset At) : ICommand;

/// <summary>A person's answer after an unknown result: the post's URN if it is on LinkedIn, or null if it is not.</summary>
public sealed record ResolveLinkedInPost(PostId PostId, string? Urn, DateTimeOffset At) : ICommand;

public sealed class StartLinkedInPostHandler(IAggregateStore store) : ICommandHandler<StartLinkedInPost>
{
    public async Task<Result> Handle(StartLinkedInPost cmd, CancellationToken ct)
    {
        var record = await store.LoadOrDefault<LinkedInPost>(LinkedInPost.StreamOf(cmd.PostId), ct);
        if (record is null)
        {
            record = LinkedInPost.Begin(cmd.PostId, cmd.SiteId, cmd.Sender, cmd.TextSha256, cmd.ArticleUrl, cmd.At);
        }
        else
        {
            record.Attempt(cmd.Sender, cmd.TextSha256, cmd.ArticleUrl, cmd.At);
        }

        await store.Save(record, ct);
        return Result.Ok();
    }
}

public sealed class RecordLinkedInOutcomeHandler(IAggregateStore store) : ICommandHandler<RecordLinkedInOutcome>
{
    public async Task<Result> Handle(RecordLinkedInOutcome cmd, CancellationToken ct)
    {
        var record = await store.Load<LinkedInPost>(LinkedInPost.StreamOf(cmd.PostId), ct);
        var result = cmd.Result;
        switch (result.Status)
        {
            case LinkedInSendStatus.Posted when result.Urn is { Length: > 0 } urn:
                record.Posted(urn, cmd.At);
                break;
            case LinkedInSendStatus.Refused:
                record.Failed(result.Reason ?? "LinkedIn refused the post.", result.Retryable, cmd.At,
                    result.RetryAfter is { } wait ? cmd.At + wait : null);
                break;
            default:
                // Posted without a URN is not something we can prove: it may have posted, and we cannot point at it.
                record.Unknown(result.Reason ?? "LinkedIn answered without saying which post it created.", cmd.At);
                break;
        }

        await store.Save(record, ct);
        return Result.Ok();
    }
}

public sealed class ResolveLinkedInPostHandler(IAggregateStore store) : ICommandHandler<ResolveLinkedInPost>
{
    public async Task<Result> Handle(ResolveLinkedInPost cmd, CancellationToken ct)
    {
        var record = await store.Load<LinkedInPost>(LinkedInPost.StreamOf(cmd.PostId), ct);
        record.Resolve(cmd.Urn, cmd.At);
        await store.Save(record, ct);
        return Result.Ok();
    }
}
