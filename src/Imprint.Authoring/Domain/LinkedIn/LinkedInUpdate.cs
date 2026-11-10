using Imprint.Authoring.Domain.LinkedIn.Events;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Domain.LinkedIn;

public enum LinkedInUpdateState
{
    Scheduled,
    Cancelled,

    /// <summary>An attempt is recorded and its result not yet. Seen after a restart, it means unknown.</summary>
    Attempting,
    Posted,

    /// <summary>Refused; <see cref="LinkedInUpdate.Retryable"/> says whether it is sent again on its own.</summary>
    Failed,

    /// <summary>It may or may not have posted; a person must check before anything happens.</summary>
    Unknown,

    /// <summary>Not sent at its time; it waits for a new one.</summary>
    Missed,
}

/// <summary>Where an update's first comment stands. It has its own states beside the post's: nothing that happens
/// to the comment changes the post.</summary>
public enum LinkedInUpdateCommentState
{
    /// <summary>Waiting for its post to go out.</summary>
    Waiting,

    /// <summary>An attempt is recorded and its result not yet. Seen after a restart, it means unknown.</summary>
    Attempting,
    Posted,

    /// <summary>Refused; <see cref="LinkedInUpdate.CommentRetryable"/> says whether it is sent again on its own.</summary>
    Failed,

    /// <summary>It may or may not be there; never sent again on its own.</summary>
    Unknown,

    /// <summary>Not sent while its post was recent; a person adds it by hand if it should be there.</summary>
    Missed,
}

/// <summary>
/// A LinkedIn post of its own (not an article's announcement): text, images and a time, and optionally a first
/// comment posted under it, as the same sender, once it is out (usually the link). It can change until it is sent,
/// goes out at most once, and one whose time passed long ago is held back as missed instead of posted late.
/// </summary>
public sealed class LinkedInUpdate : AggregateRoot
{
    public const int MaxTextLength = 3000;
    public const int MaxImages = 9;

    /// <summary>What LinkedIn's comment box takes (its API documentation names no limit of its own).</summary>
    public const int MaxCommentLength = 1250;

    public LinkedInUpdateId Id { get; private set; }
    public SiteId SiteId { get; private set; }
    public string Sender { get; private set; } = "";
    public string Text { get; private set; } = "";
    public IReadOnlyList<LinkedInUpdateImage> Images { get; private set; } = [];
    public DateTimeOffset PublishAt { get; private set; }
    public LinkedInUpdateState State { get; private set; }
    public string? Urn { get; private set; }
    public string? Reason { get; private set; }
    public bool Retryable { get; private set; }
    public DateTimeOffset? RetryAfter { get; private set; }

    /// <summary>When LinkedIn's answer, or a person's check, said the post is out; the first comment's window starts here.</summary>
    public DateTimeOffset? PostedAt { get; private set; }

    /// <summary>The first comment's text; null when the update has none.</summary>
    public string? FirstComment { get; private set; }

    /// <summary>Null when the update has no first comment.</summary>
    public LinkedInUpdateCommentState? CommentState { get; private set; }
    public string? CommentUrn { get; private set; }
    public string? CommentReason { get; private set; }
    public bool CommentRetryable { get; private set; }
    public DateTimeOffset? CommentRetryAfter { get; private set; }

    public override string StreamId => Id.Stream;

    public static LinkedInUpdate Schedule(
        LinkedInUpdateId id, SiteId site, string sender, string text, IReadOnlyList<LinkedInUpdateImage> images,
        DateTimeOffset publishAt, DateTimeOffset now, string? firstComment = null)
    {
        var (key, body, pictures) = Checked(sender, text, images, publishAt, now);
        var update = new LinkedInUpdate();
        update.Raise(new LinkedInUpdateScheduled(id, site, key, body, pictures, publishAt, now, CheckedComment(firstComment)));
        return update;
    }

    /// <summary>Change what goes out and when, the first comment included (null or blank for none). Allowed until an
    /// attempt is made, and again after a refusal that will not be retried or a missed time: the update is then
    /// Scheduled for the new time.</summary>
    public void Edit(
        string sender, string text, IReadOnlyList<LinkedInUpdateImage> images, DateTimeOffset publishAt, DateTimeOffset now,
        string? firstComment = null)
    {
        if (!Changeable)
        {
            throw new DomainException($"This update cannot be changed: it is {State.ToString().ToLowerInvariant()}.");
        }

        var (key, body, pictures) = Checked(sender, text, images, publishAt, now);
        Raise(new LinkedInUpdateEdited(key, body, pictures, publishAt, now, CheckedComment(firstComment)));
    }

    public void Cancel(DateTimeOffset now)
    {
        if (!Changeable)
        {
            throw new DomainException($"This update cannot be cancelled: it is {State.ToString().ToLowerInvariant()}.");
        }

        Raise(new LinkedInUpdateCancelled(now));
    }

    /// <summary>Whether it may be sent at <paramref name="now"/>: a scheduled update whose time has come, or a
    /// retryable refusal whose wait is over. Never after a post, while an attempt is open or unknown, or once missed.</summary>
    public bool MaySendAt(DateTimeOffset now) => State switch
    {
        LinkedInUpdateState.Scheduled => PublishAt <= now,
        LinkedInUpdateState.Failed => Retryable && (RetryAfter is null || RetryAfter <= now),
        _ => false,
    };

    public void Attempt(string textSha256, DateTimeOffset now)
    {
        if (!MaySendAt(now))
        {
            throw new DomainException(State switch
            {
                LinkedInUpdateState.Posted => "This update has already gone out.",
                LinkedInUpdateState.Unknown or LinkedInUpdateState.Attempting =>
                    "The last attempt's result is unknown: check LinkedIn and resolve it before anything is sent.",
                LinkedInUpdateState.Scheduled => "This update is not due yet.",
                _ => $"This update may not be sent: it is {State.ToString().ToLowerInvariant()}.",
            });
        }

        Raise(new LinkedInUpdateAttempting(textSha256, now));
    }

    public void Posted(string urn, DateTimeOffset at)
    {
        EnsureAttempting();
        Raise(new LinkedInUpdatePosted(urn, at));
    }

    public void Failed(string reason, bool retryable, DateTimeOffset at, DateTimeOffset? retryAfter = null)
    {
        EnsureAttempting();
        Raise(new LinkedInUpdateFailed(reason, retryable, at, retryable ? retryAfter : null));
    }

    public void Unknown(string reason, DateTimeOffset at)
    {
        EnsureAttempting();
        Raise(new LinkedInUpdateUnknown(reason, at));
    }

    /// <summary>Hold back an update that is due but too late to send as planned.</summary>
    public void Miss(string reason, DateTimeOffset at)
    {
        if (State is not (LinkedInUpdateState.Scheduled or LinkedInUpdateState.Failed))
        {
            throw new DomainException($"Only a scheduled update can be missed; this one is {State.ToString().ToLowerInvariant()}.");
        }

        Raise(new LinkedInUpdateMissed(reason, at));
    }

    /// <summary>A person's answer to an unknown result: the post is on LinkedIn (its URN), or it is not (null).</summary>
    public void Resolve(string? urn, DateTimeOffset at)
    {
        if (State is not LinkedInUpdateState.Unknown)
        {
            throw new DomainException("This update's result is not unknown, so there is nothing to resolve.");
        }

        Raise(new LinkedInUpdateResolved(string.IsNullOrWhiteSpace(urn) ? null : urn.Trim(), at));
    }

    /// <summary>Whether the first comment may be sent at <paramref name="now"/>: its post is out with a known URN,
    /// and the comment has not been tried yet, or was refused in a way that may be repeated and its wait is over.
    /// How late it may still go out is the scheduler's rule, as for the post.</summary>
    public bool MayCommentAt(DateTimeOffset now) =>
        State is LinkedInUpdateState.Posted && Urn is { Length: > 0 } && CommentState switch
        {
            LinkedInUpdateCommentState.Waiting => true,
            LinkedInUpdateCommentState.Failed => CommentRetryable && (CommentRetryAfter is null || CommentRetryAfter <= now),
            _ => false,
        };

    public void AttemptComment(string textSha256, DateTimeOffset now)
    {
        if (!MayCommentAt(now))
        {
            throw new DomainException(CommentState switch
            {
                null => "This update has no first comment.",
                LinkedInUpdateCommentState.Posted => "The first comment is already on LinkedIn.",
                LinkedInUpdateCommentState.Attempting or LinkedInUpdateCommentState.Unknown =>
                    "The first comment's last attempt has an unknown result: check LinkedIn before anything is sent.",
                LinkedInUpdateCommentState.Waiting => "The first comment waits for its post to go out.",
                _ => $"The first comment may not be sent: it is {CommentState.ToString()!.ToLowerInvariant()}.",
            });
        }

        Raise(new LinkedInUpdateCommentAttempting(textSha256, now));
    }

    public void CommentPosted(string urn, DateTimeOffset at)
    {
        EnsureCommentAttempting();
        Raise(new LinkedInUpdateCommentPosted(urn, at));
    }

    public void CommentFailed(string reason, bool retryable, DateTimeOffset at, DateTimeOffset? retryAfter = null)
    {
        EnsureCommentAttempting();
        Raise(new LinkedInUpdateCommentFailed(reason, retryable, at, retryable ? retryAfter : null));
    }

    public void CommentUnknown(string reason, DateTimeOffset at)
    {
        EnsureCommentAttempting();
        Raise(new LinkedInUpdateCommentUnknown(reason, at));
    }

    /// <summary>Leave a first comment that could not go out while its post was recent for a person to add by hand.</summary>
    public void MissComment(string reason, DateTimeOffset at)
    {
        if (!(CommentState is LinkedInUpdateCommentState.Waiting || (CommentState is LinkedInUpdateCommentState.Failed && CommentRetryable)))
        {
            throw new DomainException(CommentState is null
                ? "This update has no first comment."
                : $"Only a waiting first comment can be missed; this one is {CommentState.ToString()!.ToLowerInvariant()}.");
        }

        Raise(new LinkedInUpdateCommentMissed(reason, at));
    }

    private bool Changeable => State is LinkedInUpdateState.Scheduled or LinkedInUpdateState.Missed
        || (State is LinkedInUpdateState.Failed && !Retryable);

    private static string? CheckedComment(string? firstComment)
    {
        var comment = (firstComment ?? "").Trim();
        if (comment.Length > MaxCommentLength)
        {
            throw new DomainException($"A LinkedIn comment can be at most {MaxCommentLength:N0} characters; this one has {comment.Length:N0}.");
        }

        return comment.Length == 0 ? null : comment;
    }

    private void EnsureCommentAttempting()
    {
        if (CommentState is not LinkedInUpdateCommentState.Attempting)
        {
            throw new DomainException("There is no open attempt at the first comment to record a result for.");
        }
    }

    private void ResetComment(string? firstComment)
    {
        FirstComment = firstComment;
        CommentState = firstComment is null ? null : LinkedInUpdateCommentState.Waiting;
        CommentUrn = null;
        CommentReason = null;
        CommentRetryable = false;
        CommentRetryAfter = null;
    }

    private static (string Sender, string Text, IReadOnlyList<LinkedInUpdateImage> Images) Checked(
        string sender, string text, IReadOnlyList<LinkedInUpdateImage> images, DateTimeOffset publishAt, DateTimeOffset now)
    {
        var key = (sender ?? "").Trim().ToLowerInvariant();
        var body = (text ?? "").Trim();
        if (key.Length == 0)
        {
            throw new DomainException("An update needs a sender.");
        }

        if (body.Length == 0)
        {
            throw new DomainException("An update needs text.");
        }

        if (body.Length > MaxTextLength)
        {
            throw new DomainException($"LinkedIn allows at most 3,000 characters; this text has {body.Length:N0}.");
        }

        var pictures = (images ?? []).Select(i => i with { Alt = (i.Alt ?? "").Trim() }).ToList();
        if (pictures.Count > MaxImages)
        {
            throw new DomainException($"An update can carry at most {MaxImages} images; this one has {pictures.Count}.");
        }

        if (pictures.Any(i => i.Alt.Length == 0))
        {
            throw new DomainException("Every image needs alt text, for people who cannot see it.");
        }

        if (pictures.Select(i => i.AssetId).Distinct().Count() != pictures.Count)
        {
            throw new DomainException("The same image is on the update twice.");
        }

        if (publishAt < now)
        {
            throw new DomainException("The publish time is in the past: give a time from now on.");
        }

        return (key, body, pictures);
    }

    private void EnsureAttempting()
    {
        if (State is not LinkedInUpdateState.Attempting)
        {
            throw new DomainException("There is no open attempt to record a result for.");
        }
    }

    protected override void When(object @event)
    {
        switch (@event)
        {
            case LinkedInUpdateScheduled e:
                Id = e.Id;
                SiteId = e.SiteId;
                Sender = e.Sender;
                Text = e.Text;
                Images = e.Images;
                PublishAt = e.PublishAt;
                State = LinkedInUpdateState.Scheduled;
                ResetComment(e.FirstComment);
                break;
            case LinkedInUpdateEdited e:
                Sender = e.Sender;
                Text = e.Text;
                Images = e.Images;
                PublishAt = e.PublishAt;
                State = LinkedInUpdateState.Scheduled;
                Reason = null;
                Retryable = false;
                RetryAfter = null;
                ResetComment(e.FirstComment);
                break;
            case LinkedInUpdateCancelled:
                State = LinkedInUpdateState.Cancelled;
                break;
            case LinkedInUpdateAttempting:
                State = LinkedInUpdateState.Attempting;
                Reason = null;
                Retryable = false;
                RetryAfter = null;
                break;
            case LinkedInUpdatePosted e:
                State = LinkedInUpdateState.Posted;
                Urn = e.Urn;
                PostedAt = e.At;
                break;
            case LinkedInUpdateFailed e:
                State = LinkedInUpdateState.Failed;
                Reason = e.Reason;
                Retryable = e.Retryable;
                RetryAfter = e.RetryAfter;
                break;
            case LinkedInUpdateUnknown e:
                State = LinkedInUpdateState.Unknown;
                Reason = e.Reason;
                break;
            case LinkedInUpdateMissed e:
                State = LinkedInUpdateState.Missed;
                Reason = e.Reason;
                break;
            case LinkedInUpdateResolved e:
                State = e.Urn is null ? LinkedInUpdateState.Missed : LinkedInUpdateState.Posted;
                Urn = e.Urn;
                Reason = e.Urn is null ? "Not posted, by a person's check: give it a new time." : null;
                PostedAt = e.Urn is null ? null : e.At;
                break;
            case LinkedInUpdateCommentAttempting:
                CommentState = LinkedInUpdateCommentState.Attempting;
                CommentReason = null;
                CommentRetryable = false;
                CommentRetryAfter = null;
                break;
            case LinkedInUpdateCommentPosted e:
                CommentState = LinkedInUpdateCommentState.Posted;
                CommentUrn = e.Urn;
                break;
            case LinkedInUpdateCommentFailed e:
                CommentState = LinkedInUpdateCommentState.Failed;
                CommentReason = e.Reason;
                CommentRetryable = e.Retryable;
                CommentRetryAfter = e.RetryAfter;
                break;
            case LinkedInUpdateCommentUnknown e:
                CommentState = LinkedInUpdateCommentState.Unknown;
                CommentReason = e.Reason;
                break;
            case LinkedInUpdateCommentMissed e:
                CommentState = LinkedInUpdateCommentState.Missed;
                CommentReason = e.Reason;
                CommentRetryable = false;
                CommentRetryAfter = null;
                break;
            default:
                throw new InvalidOperationException($"LinkedInUpdate cannot fold {@event.GetType().Name}.");
        }
    }
}
