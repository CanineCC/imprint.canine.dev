using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Pages.Events;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Projections;

/// <summary>A page as the delivery plane sees it: the state that was current when it was published.</summary>
public sealed record PublishedPage(
    PageId Id,
    SiteId SiteId,
    Slug Slug,
    LocalizedText Title,
    LocalizedText MetaTitle,
    LocalizedText MetaDescription,
    PageTree Tree,
    long PublishedVersion)
{
    /// <summary>
    /// Where this page is served, relative to the site root — <c>about</c>, or a nested
    /// <c>registry/github/jasperfx/marten</c>. Empty for the home page.
    /// </summary>
    /// <remarks>
    /// An authored page's address is its slug, and a slug is deliberately flat: one segment of
    /// lower-case letters, digits and hyphens, so an editor cannot invent a folder structure by
    /// typing a slash. Pages that arrive from another system are not typed by an editor and DO carry
    /// a hierarchy — a survey belongs under its owner, which belongs under its host — so the address
    /// is modelled separately from the slug rather than by loosening what a slug may contain.
    /// <para>
    /// Everything downstream of path assignment already worked in strings, so this is the one place
    /// the distinction has to exist.
    /// </para>
    /// </remarks>
    public string PublicPath { get; init; } = Slug.Value ?? string.Empty;
}

/// <summary>
/// The publisher's source. Folds every page event through its own aggregate instances
/// (independently of <see cref="PageDrafts"/> — projections must not depend on each
/// other's fold order), and snapshots the state whenever <c>page.published</c> arrives:
/// because the global sequence is ordered, the folded state AT that moment is exactly
/// the state the publish covers. No stream re-reading, no time travel — ordering does
/// all the work. Everything in a snapshot is immutable, so a snapshot is a handful of
/// references.
/// </summary>
public sealed class PublishedContent : ReadModel
{
    private readonly Dictionary<PageId, Page> _drafts = [];
    private readonly Dictionary<PageId, PublishedPage> _published = [];

    public IReadOnlyCollection<PublishedPage> All => _published.Values;

    /// <summary>The published pages of one site — the per-site publisher's page source.</summary>
    public IReadOnlyList<PublishedPage> AllForSite(SiteId site) =>
        [.. _published.Values.Where(page => page.SiteId == site)];

    public PublishedPage? Get(PageId id) => _published.GetValueOrDefault(id);

    /// <summary>
    /// Every page of one site in its CURRENT DRAFT state — the preview plane's page source,
    /// published or not. The record's <c>PublishedVersion</c> carries the draft's aggregate
    /// version, so the preview folder's manifest staleness check re-renders a page whenever
    /// it is edited — the same mechanism a real publish uses, fed a faster-moving number.
    /// </summary>
    public IReadOnlyList<PublishedPage> AllForSiteWithDrafts(SiteId site) =>
    [
        .. _drafts.Values
            .Where(page => page.SiteId == site)
            .Select(page => new PublishedPage(
                page.Id, page.SiteId, page.Slug, page.Title, page.MetaTitle, page.MetaDescription,
                page.Tree, page.Version)),
    ];

    public override void Apply(StoredEvent @event)
    {
        if (StreamIds.IdOf(@event.StreamId, "page-") is not { } guid)
        {
            return;
        }

        var id = PageId.From(guid);
        switch (@event.Event)
        {
            case PageCreated:
                var created = new Page();
                created.LoadFrom([@event.Event]);
                _drafts[id] = created;
                return; // nothing published yet — no notification needed

            case PagePublished published when _drafts.TryGetValue(id, out var page):
                page.LoadFrom([@event.Event]);
                _published[id] = new PublishedPage(
                    id, page.SiteId, page.Slug, page.Title, page.MetaTitle, page.MetaDescription,
                    page.Tree, published.Version);
                break;

            case PageUnpublished when _drafts.TryGetValue(id, out var page):
                page.LoadFrom([@event.Event]);
                _published.Remove(id);
                break;

            case PageDeleted:
                _drafts.Remove(id);
                _published.Remove(id);
                break;

            default:
                if (!_drafts.TryGetValue(id, out var draft))
                {
                    throw new InvalidOperationException(
                        $"Page event {@event.StableId} for unknown page {id} — corrupt sequence?");
                }

                draft.LoadFrom([@event.Event]);
                return; // draft-only change: the published view is unaffected
        }

        NotifyChanged();
    }

    public override void Reset()
    {
        _drafts.Clear();
        _published.Clear();
    }
}
