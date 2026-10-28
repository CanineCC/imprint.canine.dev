using Imprint.EventSourcing;

namespace Imprint.Authoring.Domain.LinkedIn.Events;

// One standalone LinkedIn update, in its own stream (linkedin-update-{id}). Like an article's post, an attempt is
// recorded BEFORE the call to LinkedIn: the Posts API has no idempotency key, so a result that is not known stops
// the update until a person has checked LinkedIn.

/// <summary>An update is planned: who sends it, what it says, its images, and when it goes out.</summary>
[EventType("linkedin-update.scheduled")]
public sealed record LinkedInUpdateScheduled(
    LinkedInUpdateId Id, SiteId SiteId, string Sender, string Text, IReadOnlyList<LinkedInUpdateImage> Images,
    DateTimeOffset PublishAt, DateTimeOffset At)
{
    // A record compares a list by reference; the images are compared by content (as SiteLinkedInSendersChanged does).
    public bool Equals(LinkedInUpdateScheduled? other) =>
        other is not null && (Id, SiteId, Sender, Text, PublishAt, At) == (other.Id, other.SiteId, other.Sender, other.Text, other.PublishAt, other.At)
        && Images.SequenceEqual(other.Images);

    public override int GetHashCode() => HashCode.Combine(Id, SiteId, Sender, Text, PublishAt, At, Images.Count);
}

/// <summary>An update that has not gone out was changed; it is Scheduled again for <paramref name="PublishAt"/>.</summary>
[EventType("linkedin-update.edited")]
public sealed record LinkedInUpdateEdited(
    string Sender, string Text, IReadOnlyList<LinkedInUpdateImage> Images, DateTimeOffset PublishAt, DateTimeOffset At)
{
    public bool Equals(LinkedInUpdateEdited? other) =>
        other is not null && (Sender, Text, PublishAt, At) == (other.Sender, other.Text, other.PublishAt, other.At)
        && Images.SequenceEqual(other.Images);

    public override int GetHashCode() => HashCode.Combine(Sender, Text, PublishAt, At, Images.Count);
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
