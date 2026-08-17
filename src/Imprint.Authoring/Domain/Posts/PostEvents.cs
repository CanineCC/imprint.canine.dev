using Imprint.EventSourcing;

namespace Imprint.Authoring.Domain.Posts.Events;

// The Post aggregate's event union. A post's authored state is MARKDOWN, so the body event
// carries the source text and nothing derived: the node tree is a projection of it, and
// storing the tree here would freeze one converter's output into the log forever — a later
// fix to the converter could then never reach a post already written.

[EventType("post.created")]
public sealed record PostCreated(PostId PostId, SiteId SiteId, string Slug, Locale InitialLocale, string Title);

[EventType("post.title-changed")]
public sealed record PostTitleChanged(Locale Locale, string Title);

[EventType("post.slug-changed")]
public sealed record PostSlugChanged(string Slug);

[EventType("post.meta-changed")]
public sealed record PostMetaChanged(Locale Locale, string? MetaTitle, string? MetaDescription);

[EventType("post.body-changed")]
public sealed record PostBodyChanged(Locale Locale, string Markdown);

// The instant a reader sorts and cites by. Raised once: re-publishing after an edit is an
// update, not a new post, so the date does not move because a typo was fixed.
[EventType("post.published")]
public sealed record PostPublished(DateTimeOffset PublishedAt);

[EventType("post.unpublished")]
public sealed record PostUnpublished;

// Review and scheduling. A post is written by one person and cleared by another, and the two
// decisions it carries are separable: WHETHER the words may go out, and WHEN. They are separate
// events for that reason — a reviewer moving the date is not a re-approval of the prose, and an
// author moving it before submitting is not a review at all.

/// <summary>The intended go-live instant, absolute. Null is a deliberate "to be decided": a post
/// can be approved without a date and simply wait for one.</summary>
[EventType("post.publish-date-set")]
public sealed record PostPublishDateSet(DateTimeOffset? PublishAt);

[EventType("post.submitted-for-review")]
public sealed record PostSubmittedForReview(DateTimeOffset? ProposedPublishAt, string? Note);

/// <summary>Sign-off. Carries the date the reviewer settled on, which may be neither the proposed
/// one nor any date at all.</summary>
[EventType("post.review-approved")]
public sealed record PostReviewApproved(DateTimeOffset? PublishAt);

[EventType("post.changes-requested")]
public sealed record PostChangesRequested(string Reason);

/// <summary>Approval withdrawn by an edit: the words that were cleared are no longer the words on
/// the page, so the sign-off cannot still stand.</summary>
[EventType("post.approval-lapsed")]
public sealed record PostApprovalLapsed;

[EventType("post.deleted")]
public sealed record PostDeleted;
