using Imprint.EventSourcing;

namespace Imprint.Authoring.Domain.LinkedIn.Events;

// One standalone LinkedIn update, in its own stream (linkedin-update-{id}). Like an article's post, an attempt is
// recorded BEFORE the call to LinkedIn: the Posts API has no idempotency key, so a result that is not known stops
// the update until a person has checked LinkedIn.

/// <summary>An update is planned: who sends it, what it says, its images, and when it goes out; and the first
/// comment posted under it once it is out (null for none).</summary>
/// <remarks><paramref name="FirstComment"/> was added on 2026-11-10 as an optional last field: an update stored
/// before then has none in its JSON and reads as null, so the stored events replay unchanged.</remarks>
[EventType("linkedin-update.scheduled")]
public sealed record LinkedInUpdateScheduled(
    LinkedInUpdateId Id, SiteId SiteId, string Sender, string Text, IReadOnlyList<LinkedInUpdateImage> Images,
    DateTimeOffset PublishAt, DateTimeOffset At, string? FirstComment = null)
{
    // A record compares a list by reference; the images are compared by content (as SiteLinkedInSendersChanged does).
    public bool Equals(LinkedInUpdateScheduled? other) =>
        other is not null && (Id, SiteId, Sender, Text, PublishAt, At, FirstComment) == (other.Id, other.SiteId, other.Sender, other.Text, other.PublishAt, other.At, other.FirstComment)
        && Images.SequenceEqual(other.Images);

    public override int GetHashCode() => HashCode.Combine(Id, SiteId, Sender, Text, PublishAt, At, Images.Count, FirstComment);
}

/// <summary>An update that has not gone out was changed; it is Scheduled again for <paramref name="PublishAt"/>.
/// Everything is restated, the first comment too (null for none; optional for the reason the scheduled event gives).</summary>
[EventType("linkedin-update.edited")]
public sealed record LinkedInUpdateEdited(
    string Sender, string Text, IReadOnlyList<LinkedInUpdateImage> Images, DateTimeOffset PublishAt, DateTimeOffset At,
    string? FirstComment = null)
{
    public bool Equals(LinkedInUpdateEdited? other) =>
        other is not null && (Sender, Text, PublishAt, At, FirstComment) == (other.Sender, other.Text, other.PublishAt, other.At, other.FirstComment)
        && Images.SequenceEqual(other.Images);

    public override int GetHashCode() => HashCode.Combine(Sender, Text, PublishAt, At, Images.Count, FirstComment);
}

[EventType("linkedin-update.cancelled")]
public sealed record LinkedInUpdateCancelled(DateTimeOffset At);

/// <summary>The update is being sent: the hash of the sender and text that go out.</summary>
[EventType("linkedin-update.attempting")]
public sealed record LinkedInUpdateAttempting(string TextSha256, DateTimeOffset At);

/// <summary>LinkedIn accepted it: <paramref name="Urn"/> is the post's id.</summary>
[EventType("linkedin-update.posted")]
public sealed record LinkedInUpdatePosted(string Urn, DateTimeOffset At);

/// <summary>LinkedIn refused it; <paramref name="Retryable"/> only when the refusal proves nothing was posted.</summary>
[EventType("linkedin-update.failed")]
public sealed record LinkedInUpdateFailed(string Reason, bool Retryable, DateTimeOffset At, DateTimeOffset? RetryAfter = null);

/// <summary>The call's result is not known: it may have posted. Never retried on its own.</summary>
[EventType("linkedin-update.unknown")]
public sealed record LinkedInUpdateUnknown(string Reason, DateTimeOffset At);

/// <summary>Its time passed too long ago to send it as planned (nothing was running, or a person said an unknown
/// attempt did not post); it waits for a new time.</summary>
[EventType("linkedin-update.missed")]
public sealed record LinkedInUpdateMissed(string Reason, DateTimeOffset At);

/// <summary>A person checked LinkedIn after an unknown result: it is there (<paramref name="Urn"/>), or it is not
/// (null, and the update waits for a new time).</summary>
[EventType("linkedin-update.resolved")]
public sealed record LinkedInUpdateResolved(string? Urn, DateTimeOffset At);

// The first comment, once its post is out. Its own record beside the post's: whatever happens to the comment, the
// post that went out stays Posted and is never sent again. The same at-most-once rule as the post: the attempt is
// recorded before LinkedIn is called, and a result that is not known is never sent again on its own.

/// <summary>The first comment is being sent: the hash of its text.</summary>
[EventType("linkedin-update.comment-attempting")]
public sealed record LinkedInUpdateCommentAttempting(string TextSha256, DateTimeOffset At);

/// <summary>LinkedIn made the comment: <paramref name="Urn"/> is its id.</summary>
[EventType("linkedin-update.comment-posted")]
public sealed record LinkedInUpdateCommentPosted(string Urn, DateTimeOffset At);

/// <summary>LinkedIn refused the comment; <paramref name="Retryable"/> only when the refusal proves no comment was made.</summary>
[EventType("linkedin-update.comment-failed")]
public sealed record LinkedInUpdateCommentFailed(string Reason, bool Retryable, DateTimeOffset At, DateTimeOffset? RetryAfter = null);

/// <summary>The comment call's result is not known: it may be there. Never sent again on its own.</summary>
[EventType("linkedin-update.comment-unknown")]
public sealed record LinkedInUpdateCommentUnknown(string Reason, DateTimeOffset At);

/// <summary>The comment could not go out while its post was recent; it is left for a person to add by hand.</summary>
[EventType("linkedin-update.comment-missed")]
public sealed record LinkedInUpdateCommentMissed(string Reason, DateTimeOffset At);
