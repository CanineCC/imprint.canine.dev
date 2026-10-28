using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Posts;
using Imprint.Authoring.Domain.Posts.Events;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Projections;

/// <summary>One post as it was the moment it was published — the publisher's source.</summary>
/// <param name="RootsByLocale">The page tree per locale, already converted and wrapped.
/// <para>Snapshotting the TREE rather than the markdown is deliberate, and is the opposite of what
/// the aggregate stores on purpose: the published site must be exactly what was approved at publish
/// time, so a later change to the converter cannot silently rewrite a post that is already live.
/// The markdown stays the authored truth in the log; this is the approved rendering of it.</para>
/// <para>Per LOCALE, and not one tree with localized text inside it the way an authored page works:
/// a translation is written independently, so its markdown can have a different number of
/// paragraphs, a list where the original has prose, its own code block. There is no node-for-node
/// correspondence to hang localized strings off, so the trees are kept apart.</para></param>
public sealed record PublishedPost(
    PostId Id,
    SiteId SiteId,
    Slug Slug,
    LocalizedText Title,
    LocalizedText MetaTitle,
    LocalizedText MetaDescription,
    IReadOnlyDictionary<Locale, NodeList> RootsByLocale,
    DateTimeOffset PublishedAt,
    DateTimeOffset UpdatedAt,
    int? Version = null,
    string? Sha256 = null,
    string? Kind = null,
    IReadOnlyList<string>? Products = null,
    string? SignedBy = null,
    DateTimeOffset? SignedAt = null,
    string? LinkedInText = null,
    string? LinkedInSender = null,
    PostReview? Review = null,
    IReadOnlyList<PublishedVersion>? Versions = null)
{
    /// <summary>Every version this post has been published at, newest first; empty, never null.
    /// A version is a set of words (see <c>post.published</c>), so a republish of the same words
    /// adds nothing here.</summary>
    public IReadOnlyList<PublishedVersion> Versions { get; init; } = Versions ?? [];

    /// <summary>The products as published, in canonical order; empty, never null.</summary>
    public IReadOnlyList<string> Products { get; init; } = Products ?? [];

    /// <summary>The tree to render for a locale, falling back to the site default the way every
    /// other text does — a post translated into one language still serves its other pages.</summary>
    public NodeList RootsFor(Locale locale, Locale defaultLocale) =>
        RootsByLocale.TryGetValue(locale, out var roots) ? roots
        : RootsByLocale.TryGetValue(defaultLocale, out var fallback) ? fallback
        : NodeList.Empty;

    /// <summary>The locales this post actually has a body for.</summary>
    public IReadOnlyCollection<Locale> Locales => [.. RootsByLocale.Keys];
}

/// <summary>One published version of a post: its number, when it first went out, and the sha256 of
/// its words.</summary>
public sealed record PublishedVersion(int Version, DateTimeOffset PublishedAt, string? Sha256);

/// <summary>
/// The publisher's post source. Folds post events through their own <see cref="Post"/> instances —
/// independently of <see cref="PostList"/>, because projections must not depend on each other's
/// fold order — and snapshots at <c>post.published</c>: the global sequence is ordered, so the
/// folded state at that moment is exactly the state the publish covers.
/// </summary>
public sealed class PublishedPosts : ReadModel
{
    private readonly Dictionary<PostId, Post> _drafts = [];
    private readonly Dictionary<PostId, PublishedPost> _published = [];

    // Who signed the words the post now holds, and when: the approving event's actor (a login) and
    // instant. The aggregate does not see metadata, so the projection keeps it beside the draft.
    private readonly Dictionary<PostId, (string By, DateTimeOffset At)> _signatures = [];

    // Every version each post has been published at, newest first (the document record's history).
    private readonly Dictionary<PostId, List<PublishedVersion>> _versions = [];

    /// <summary>The published posts of one site, newest first — the listing and the feed order.</summary>
    public IReadOnlyList<PublishedPost> AllForSite(SiteId site) =>
    [
        .. _published.Values
            .Where(post => post.SiteId == site)
            .OrderByDescending(post => post.PublishedAt)
            .ThenBy(post => post.Slug.Value, StringComparer.Ordinal),
    ];

    public PublishedPost? Get(PostId id) => _published.GetValueOrDefault(id);

    /// <summary>
    /// The published posts of one site PLUS its unpublished ones, each rendered from the markdown
    /// as it stands right now — the preview plane's source, and nobody else's.
    /// <para>This is the one place a post's tree is built from CURRENT markdown rather than from
    /// the snapshot taken at publish time, and that is the entire point: "see it before you
    /// publish it" cannot be answered by a projection whose rule is "only what was approved". A
    /// draft that does not convert is skipped rather than shown broken — the editor already lists
    /// its problems line by line, and half a post in a preview reads as a rendering bug.</para>
    /// </summary>
    public IReadOnlyList<PublishedPost> AllForSiteWithDrafts(SiteId site, DateTimeOffset draftDate) =>
    [
        .. _drafts.Values
            .Where(post => post.SiteId == site && !post.IsDeleted)
            .Select(post => _published.GetValueOrDefault(post.Id) ?? DraftSnapshot(post, draftDate, VersionsOf(post.Id)))
            .Where(post => post.RootsByLocale.Count > 0)
            .OrderByDescending(post => post.PublishedAt)
            .ThenBy(post => post.Slug.Value, StringComparer.Ordinal),
    ];

    public override void Apply(StoredEvent @event)
    {
        if (@event.Event is PostCreated created)
        {
            var post = new Post();
            post.LoadFrom([created]);
            _drafts[created.PostId] = post;
            return;
        }

        if (StreamIds.IdOf(@event.StreamId, "post-") is not { } guid)
        {
            return;
        }

        var id = PostId.From(guid);
        if (!_drafts.TryGetValue(id, out var draft))
        {
            return;
        }

        draft.LoadFrom([@event.Event]);

        switch (@event.Event)
        {
            case PostReviewApproved:
                _signatures[id] = (@event.Metadata.Actor, @event.Metadata.TimestampUtc);
                break;

            case PostApprovalLapsed or PostChangesRequested or PostSubmittedForReview:
                _signatures.Remove(id);
                break;

            case PostPublished published:
                var versions = _versions.TryGetValue(id, out var list) ? list : _versions[id] = [];
                var number = published.Version ?? 1;
                if (versions.Count == 0 || versions[0].Version != number)
                {
                    versions.Insert(0, new PublishedVersion(number, published.PublishedAt, published.Sha256));
                }

                _published[id] = (Snapshot(draft, @event.Metadata.TimestampUtc) with { Versions = [.. versions] }) is var snapshot
                    && _signatures.TryGetValue(id, out var signed)
                    ? snapshot with { SignedBy = signed.By, SignedAt = signed.At }
                    : snapshot;
                NotifyChanged();
                break;

            case PostUnpublished or PostDeleted:
                // Withdrawal is immediate and total: the page must stop being served, so the
                // publisher's sweep sees the file as no longer desired on the next run.
                if (_published.Remove(id))
                {
                    NotifyChanged();
                }

                if (@event.Event is PostDeleted)
                {
                    _drafts.Remove(id);
                    _signatures.Remove(id);
                    _versions.Remove(id);
                }

                break;
        }
    }

    public override void Reset()
    {
        _drafts.Clear();
        _published.Clear();
        _signatures.Clear();
        _versions.Clear();
    }

    private IReadOnlyList<PublishedVersion> VersionsOf(PostId id) =>
        _versions.TryGetValue(id, out var versions) ? [.. versions] : [];

    /// <summary>
    /// A not-yet-published post as the preview should show it. Unlike <see cref="Snapshot"/> this
    /// runs against markdown nothing has vetted, so a locale whose body does not convert is left
    /// out entirely: the aggregate would have refused to publish it, and a preview that renders
    /// half of a broken post teaches the author the wrong thing about what they are about to ship.
    /// </summary>
    private static PublishedPost DraftSnapshot(Post post, DateTimeOffset at, IReadOnlyList<PublishedVersion> versions)
    {
        var roots = new Dictionary<Locale, NodeList>();
        foreach (var (locale, markdown) in post.Body.Values)
        {
            if (string.IsNullOrWhiteSpace(markdown))
            {
                continue;
            }

            var (rendered, problems) = PostContent.Render(markdown, locale);
            if (problems.Count == 0)
            {
                roots[locale] = rendered;
            }
        }

        return new PublishedPost(
            post.Id, post.SiteId, post.Slug, post.Title, post.MetaTitle, post.MetaDescription,
            roots, post.PublishedAt ?? at, at, post.PublishedVersion, post.PublishedSha256, post.Kind, post.Products,
            Review: post.Review, Versions: versions);
    }

    private static PublishedPost Snapshot(Post post, DateTimeOffset at)
    {
        // One tree per locale the post has a body for. A locale whose body is blank is skipped
        // rather than published empty: an untranslated post should fall back to the original,
        // which RootsFor does, not serve a blank page under a second language.
        var roots = new Dictionary<Locale, NodeList>();
        foreach (var (locale, markdown) in post.Body.Values)
        {
            if (!string.IsNullOrWhiteSpace(markdown))
            {
                roots[locale] = PostContent.Render(markdown, locale).Roots;
            }
        }

        return new PublishedPost(
            post.Id, post.SiteId, post.Slug, post.Title, post.MetaTitle, post.MetaDescription,
            roots, post.PublishedAt ?? at, at, post.PublishedVersion, post.PublishedSha256, post.Kind, post.Products,
            LinkedInText: post.LinkedInText, LinkedInSender: post.LinkedInSender);
    }
}
