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
    DateTimeOffset UpdatedAt)
{
    /// <summary>The tree to render for a locale, falling back to the site default the way every
    /// other text does — a post translated into one language still serves its other pages.</summary>
    public NodeList RootsFor(Locale locale, Locale defaultLocale) =>
        RootsByLocale.TryGetValue(locale, out var roots) ? roots
        : RootsByLocale.TryGetValue(defaultLocale, out var fallback) ? fallback
        : NodeList.Empty;

    /// <summary>The locales this post actually has a body for.</summary>
    public IReadOnlyCollection<Locale> Locales => [.. RootsByLocale.Keys];
}

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
            .Select(post => _published.GetValueOrDefault(post.Id) ?? DraftSnapshot(post, draftDate))
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
            case PostPublished:
                _published[id] = Snapshot(draft, @event.Metadata.TimestampUtc);
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
                }

                break;
        }
    }

    public override void Reset()
    {
        _drafts.Clear();
        _published.Clear();
    }

    /// <summary>
    /// A not-yet-published post as the preview should show it. Unlike <see cref="Snapshot"/> this
    /// runs against markdown nothing has vetted, so a locale whose body does not convert is left
    /// out entirely: the aggregate would have refused to publish it, and a preview that renders
    /// half of a broken post teaches the author the wrong thing about what they are about to ship.
    /// </summary>
    private static PublishedPost DraftSnapshot(Post post, DateTimeOffset at)
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
            roots, post.PublishedAt ?? at, at);
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
            roots, post.PublishedAt ?? at, at);
    }
}
