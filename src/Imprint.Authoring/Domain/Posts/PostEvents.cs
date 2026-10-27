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
/// one nor any date at all, and the signature: <paramref name="Revision"/> is the body revision the
/// approval covers (how many body edits the post had) and <paramref name="Sha256"/> the hash of
/// exactly those words (<see cref="Post.BodySha256"/>). The event's actor is WHO approved; these two
/// say WHICH text. Both are null on approvals recorded before 2026-10-25.</summary>
[EventType("post.review-approved")]
public sealed record PostReviewApproved(DateTimeOffset? PublishAt, int? Revision = null, string? Sha256 = null);

[EventType("post.changes-requested")]
public sealed record PostChangesRequested(string Reason);

/// <summary>Approval withdrawn by an edit: the words that were cleared are no longer the words on
/// the page, so the sign-off cannot still stand.</summary>
[EventType("post.approval-lapsed")]
public sealed record PostApprovalLapsed;

/// <summary>A publish hold: a reason the post must not go live yet (figures awaiting a measurement, a
/// claim awaiting its source), or null to lift it. While held, nothing publishes the post, the
/// schedule included. It is the blocker field the resource-center review asked for.</summary>
[EventType("post.hold-set")]
public sealed record PostHoldSet(string? Reason);

/// <summary>What kind of article the post is and which products it is about (see
/// <see cref="ArticleClassification"/>). Both are already normalized: a known kind, and the products
/// de-duplicated in canonical order, so an equal classification compares equal.</summary>
[EventType("post.classified")]
public sealed record PostClassified(string Kind, IReadOnlyList<string> Products)
{
    // A list-typed positional member defeats synthesized record equality (reference compare), and
    // events must compare by value for Given/When/Then and round-trip tests: equality is by sequence.
    public bool Equals(PostClassified? other) =>
        other is not null && Kind == other.Kind && Products.SequenceEqual(other.Products);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Kind);
        foreach (var product in Products)
        {
            hash.Add(product);
        }

        return hash.ToHashCode();
    }
}

[EventType("post.deleted")]
public sealed record PostDeleted;
