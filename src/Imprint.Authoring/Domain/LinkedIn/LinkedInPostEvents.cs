using Imprint.EventSourcing;

namespace Imprint.Authoring.Domain.LinkedIn.Events;

// One article's LinkedIn post, in its own stream (linkedin-post-{postId}). The attempt is recorded BEFORE the call
// to LinkedIn, because LinkedIn's Posts API has no idempotency key: if the process dies or the call times out,
// the record says an attempt was made and its result is unknown, and nothing posts again on its own.

/// <summary>A post is being sent: who sends it, the hash of the signed sender and text, and the article's URL.</summary>
[EventType("linkedin-post.attempting")]
public sealed record LinkedInPostAttempting(PostId PostId, SiteId SiteId, string Sender, string TextSha256, string ArticleUrl, DateTimeOffset At);

/// <summary>LinkedIn accepted it: <paramref name="Urn"/> is the post's id (urn:li:share:… or urn:li:ugcPost:…).</summary>
[EventType("linkedin-post.posted")]
public sealed record LinkedInPostPosted(string Urn, DateTimeOffset At);

/// <summary>LinkedIn refused it. <paramref name="Retryable"/> only when the refusal proves nothing was posted
/// (rate limit, service unavailable); <paramref name="RetryAfter"/> is when to try again.</summary>
[EventType("linkedin-post.failed")]
public sealed record LinkedInPostFailed(string Reason, bool Retryable, DateTimeOffset At, DateTimeOffset? RetryAfter = null);

/// <summary>The call's result is not known (a timeout, a server error, a crash mid-call): it may or may not have
/// posted. Never retried automatically; a person checks the page and resolves it.</summary>
[EventType("linkedin-post.unknown")]
public sealed record LinkedInPostUnknown(string Reason, DateTimeOffset At);

/// <summary>A person checked LinkedIn after an unknown result: it is there (<paramref name="Urn"/>), or it is not
/// and may be sent again (null).</summary>
[EventType("linkedin-post.resolved")]
public sealed record LinkedInPostResolved(string? Urn, DateTimeOffset At);
