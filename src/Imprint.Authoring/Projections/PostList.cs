using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Posts;
using Imprint.Authoring.Domain.Posts.Events;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Projections;

public enum PostStatus
{
    /// <summary>Never published, or withdrawn.</summary>
    Draft,

    /// <summary>With the reviewer, waiting for an answer.</summary>
    InReview,

    /// <summary>The reviewer sent it back with a reason.</summary>
    ChangesRequested,

    /// <summary>Cleared, but with no date — "yes, but not yet".</summary>
    Approved,

    /// <summary>Cleared with a date in the future: waiting for its moment, not for a person.</summary>
    Scheduled,

    /// <summary>Published and unchanged since.</summary>
    Published,

    /// <summary>Published, but edited since — the live post is behind the draft.</summary>
    Modified,
}

/// <summary>One post as the editor index and the published blog index both see it.</summary>
/// <param name="PublishedAt">When it first went live; null while it is a draft. This is the
/// instant the listing sorts by and a reader cites — deliberately not <paramref name="UpdatedAt"/>,
/// which moves every time somebody fixes a typo.</param>
public sealed record PostSummary(
    PostId Id,
    SiteId SiteId,
    Slug Slug,
    LocalizedText Title,
    DateTimeOffset? PublishedAt,
    long Version,
    long? PublishedVersion,
    DateTimeOffset UpdatedAt,
    PostReview Review = PostReview.None,
    DateTimeOffset? PublishAt = null,
    string? ReviewNote = null)
{
    /// <summary>
    /// One word for where the post stands. Live beats everything — a published post that is
    /// waiting on a re-review is still on the internet, and saying "In review" about it would be
    /// a lie a reader could disprove. Below that, the review states come before the scheduling
    /// ones, because a post waiting on a PERSON is not waiting on a clock.
    ///
    /// <para><paramref name="siteRequiresReview"/> is why this takes the site's policy: a future
    /// date on a site that reviews is a PROPOSAL, and calling it "Scheduled" would promise the
    /// author something no clock will deliver — nothing publishes it until a person says so.</para>
    /// </summary>
    public PostStatus StatusAt(DateTimeOffset now, bool siteRequiresReview = false) => (PublishedVersion, Review) switch
    {
        (null, PostReview.Pending) => PostStatus.InReview,
        (null, PostReview.ChangesRequested) => PostStatus.ChangesRequested,
        (null, PostReview.Approved) when PublishAt is { } at && at > now => PostStatus.Scheduled,
        (null, PostReview.Approved) => PostStatus.Approved,
        (null, _) when !siteRequiresReview && PublishAt is { } future && future > now => PostStatus.Scheduled,
        (null, _) => PostStatus.Draft,
        (var published, _) when Version > published => PostStatus.Modified,
        _ => PostStatus.Published,
    };

    /// <summary>The status now, on a site that does not review. For callers with no clock or policy to offer.</summary>
    public PostStatus Status => StatusAt(DateTimeOffset.UtcNow);

    public bool IsLive => PublishedAt is not null;

    /// <summary>Ready to go out on its own, with no further human step — what the scheduler looks for.</summary>
    public bool IsDueAt(DateTimeOffset now, bool siteRequiresReview) =>
        !IsLive
        && PublishAt is { } at
        && at <= now
        && (!siteRequiresReview || Review is PostReview.Approved);
}

/// <summary>
/// The blog's list read model: the editor's index, the published index behind the listing page
/// and the feed, and the slug-uniqueness oracle for the slices (an accepted eventual-consistency
/// race — docs/architecture.md §Consistency).
/// </summary>
public sealed class PostList : ReadModel
{
    private sealed record Entry(
        SiteId SiteId,
        Slug Slug,
        LocalizedText Title,
        DateTimeOffset? PublishedAt,
        long Version,
        long? PublishedVersion,
        DateTimeOffset UpdatedAt,
        PostReview Review = PostReview.None,
        DateTimeOffset? PublishAt = null,
        string? ReviewNote = null);

    private readonly Dictionary<PostId, Entry> _posts = [];

    /// <summary>Every post of a site, newest publication first, drafts after (they have no date yet).</summary>
    public IReadOnlyList<PostSummary> All(SiteId site) =>
        Ordered(_posts.Where(pair => pair.Value.SiteId == site).Select(pair => Summarize(pair.Key, pair.Value)));

    /// <summary>Only what a reader may see — what the listing page and the feed render.</summary>
    public IReadOnlyList<PostSummary> Published(SiteId site) =>
        [.. All(site).Where(post => post.IsLive)];

    public PostSummary? Get(PostId id) =>
        _posts.TryGetValue(id, out var entry) ? Summarize(id, entry) : null;

    /// <summary>Whether a slug is already used <em>within the given site</em> — slugs are unique
    /// per site, not globally.</summary>
    public bool SlugTaken(SiteId site, Slug slug, PostId? except = null) =>
        _posts.Any(pair => pair.Value.SiteId == site && pair.Value.Slug == slug && pair.Key != except);

    public override void Apply(StoredEvent @event)
    {
        if (@event.Event is PostCreated created)
        {
            Slug.TryCreate(created.Slug, out var slug, out _);
            _posts[created.PostId] = new Entry(
                created.SiteId,
                slug,
                LocalizedText.Of(created.InitialLocale, created.Title),
                PublishedAt: null,
                Version: @event.StreamVersion,
                PublishedVersion: null,
                @event.Metadata.TimestampUtc);
            NotifyChanged();
            return;
        }

        if (StreamIds.IdOf(@event.StreamId, "post-") is not { } guid)
        {
            return;
        }

        var id = PostId.From(guid);
        if (!_posts.TryGetValue(id, out var entry))
        {
            return;
        }

        if (@event.Event is PostDeleted)
        {
            // Dropped from the model entirely rather than flagged: everything that reads this
            // is a list of posts somebody may open or a reader may see, and a deleted post is
            // neither. Its stream is still in the log if it must ever be recovered.
            _posts.Remove(id);
            NotifyChanged();
            return;
        }

        entry = entry with { Version = @event.StreamVersion, UpdatedAt = @event.Metadata.TimestampUtc };
        entry = @event.Event switch
        {
            PostSlugChanged changed when Slug.TryCreate(changed.Slug, out var slug, out _) => entry with { Slug = slug },
            PostTitleChanged changed => entry with { Title = entry.Title.With(changed.Locale, changed.Title) },
            // PublishedVersion is the stream position of the publish event itself, so "any event
            // after it" is what makes the post Modified.
            // The VERSION advances (that is what clears "Modified" after a re-publish) but the
            // DATE keeps its first value — the same first-wins rule the aggregate folds by, and
            // it has to be repeated here because a projection folds the log, not the aggregate.
            PostPublished published => entry with
            {
                PublishedAt = entry.PublishedAt ?? published.PublishedAt,
                PublishedVersion = @event.StreamVersion,
            },
            PostUnpublished => entry with { PublishedAt = null, PublishedVersion = null },
            PostPublishDateSet dated => entry with { PublishAt = dated.PublishAt },
            PostSubmittedForReview submitted => entry with
            {
                Review = PostReview.Pending,
                PublishAt = submitted.ProposedPublishAt,
                ReviewNote = submitted.Note,
            },
            PostReviewApproved approved => entry with
            {
                Review = PostReview.Approved,
                PublishAt = approved.PublishAt,
                ReviewNote = null,
            },
            PostChangesRequested sentBack => entry with
            {
                Review = PostReview.ChangesRequested,
                ReviewNote = sentBack.Reason,
            },
            PostApprovalLapsed => entry with { Review = PostReview.None },
            _ => entry,
        };

        _posts[id] = entry;
        NotifyChanged();
    }

    public override void Reset() => _posts.Clear();

    private static IReadOnlyList<PostSummary> Ordered(IEnumerable<PostSummary> summaries) =>
    [
        .. summaries
            .OrderByDescending(post => post.PublishedAt ?? DateTimeOffset.MaxValue)
            .ThenBy(post => post.Slug.Value, StringComparer.Ordinal),
    ];

    private static PostSummary Summarize(PostId id, Entry entry) =>
        new(id, entry.SiteId, entry.Slug, entry.Title, entry.PublishedAt, entry.Version, entry.PublishedVersion,
            entry.UpdatedAt, entry.Review, entry.PublishAt, entry.ReviewNote);
}
