using Imprint.Authoring.Domain.LinkedIn.Events;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Domain.LinkedIn;

public enum LinkedInPostState
{
    /// <summary>Nothing sent yet, or a person confirmed an unknown attempt did not post.</summary>
    None,

    /// <summary>An attempt is recorded and its result not yet. Seen after a restart, it means unknown.</summary>
    Attempting,

    Posted,

    /// <summary>Refused; <see cref="LinkedInPost.Retryable"/> says whether it may be sent again.</summary>
    Failed,

    /// <summary>It may or may not have posted; a person must check before anything is sent again.</summary>
    Unknown,
}

/// <summary>One article's LinkedIn post, and the rule that it goes out at most once.</summary>
public sealed class LinkedInPost : AggregateRoot
{
    public PostId PostId { get; private set; }
    public SiteId SiteId { get; private set; }
    public LinkedInPostState State { get; private set; }
    public string? Urn { get; private set; }
    public string? Reason { get; private set; }
    public bool Retryable { get; private set; }
    public DateTimeOffset? RetryAfter { get; private set; }
    public DateTimeOffset? AttemptedAt { get; private set; }

    public override string StreamId => StreamOf(PostId);

    public static string StreamOf(PostId post) => $"linkedin-post-{post.Value:N}";

    /// <summary>Whether a new attempt may be made at <paramref name="now"/>: never after a post, never while an
    /// attempt is open or its result unknown, and after a refusal only when it was retryable and its time has come.</summary>
    public bool MaySendAt(DateTimeOffset now) => State switch
    {
        LinkedInPostState.None => true,
        LinkedInPostState.Failed => Retryable && (RetryAfter is null || RetryAfter <= now),
        _ => false,
    };

    public static LinkedInPost Begin(PostId post, SiteId site, string sender, string textSha256, string articleUrl, DateTimeOffset at)
    {
        var record = new LinkedInPost();
        record.Raise(new LinkedInPostAttempting(post, site, sender, textSha256, articleUrl, at));
        return record;
    }

    public void Attempt(string sender, string textSha256, string articleUrl, DateTimeOffset at)
    {
        if (!MaySendAt(at))
        {
            throw new DomainException(State switch
            {
                LinkedInPostState.Posted => "This article's LinkedIn post has already gone out.",
                LinkedInPostState.Unknown or LinkedInPostState.Attempting =>
                    "The last attempt's result is unknown: check the LinkedIn page and resolve it before sending again.",
                _ => "This LinkedIn post may not be sent again yet.",
            });
        }

        Raise(new LinkedInPostAttempting(PostId, SiteId, sender, textSha256, articleUrl, at));
    }

    public void Posted(string urn, DateTimeOffset at)
    {
        EnsureAttempting();
        Raise(new LinkedInPostPosted(urn, at));
    }

    public void Failed(string reason, bool retryable, DateTimeOffset at, DateTimeOffset? retryAfter = null)
    {
        EnsureAttempting();
        Raise(new LinkedInPostFailed(reason, retryable, at, retryable ? retryAfter : null));
    }

    public void Unknown(string reason, DateTimeOffset at)
    {
        EnsureAttempting();
        Raise(new LinkedInPostUnknown(reason, at));
    }

    /// <summary>A person's answer to an unknown result: the post is on LinkedIn (its URN), or it is not (null).</summary>
    public void Resolve(string? urn, DateTimeOffset at)
    {
        if (State is not (LinkedInPostState.Unknown or LinkedInPostState.Attempting))
        {
            throw new DomainException("Only an attempt whose result is unknown can be resolved.");
        }

        Raise(new LinkedInPostResolved(string.IsNullOrWhiteSpace(urn) ? null : urn.Trim(), at));
    }

    private void EnsureAttempting()
    {
        if (State is not LinkedInPostState.Attempting)
        {
            throw new DomainException("There is no open attempt to record a result for.");
        }
    }

    protected override void When(object @event)
    {
        switch (@event)
        {
            case LinkedInPostAttempting e:
                PostId = e.PostId;
                SiteId = e.SiteId;
                State = LinkedInPostState.Attempting;
                AttemptedAt = e.At;
                Reason = null;
                Retryable = false;
                RetryAfter = null;
                break;
            case LinkedInPostPosted e:
                State = LinkedInPostState.Posted;
                Urn = e.Urn;
                break;
            case LinkedInPostFailed e:
                State = LinkedInPostState.Failed;
                Reason = e.Reason;
                Retryable = e.Retryable;
                RetryAfter = e.RetryAfter;
                break;
            case LinkedInPostUnknown e:
                State = LinkedInPostState.Unknown;
                Reason = e.Reason;
                break;
            case LinkedInPostResolved e:
                State = e.Urn is null ? LinkedInPostState.None : LinkedInPostState.Posted;
                Urn = e.Urn;
                break;
            default:
                throw new InvalidOperationException($"LinkedInPost cannot fold {@event.GetType().Name}.");
        }
    }
}
