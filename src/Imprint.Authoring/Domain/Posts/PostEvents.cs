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

[EventType("post.deleted")]
public sealed record PostDeleted;
