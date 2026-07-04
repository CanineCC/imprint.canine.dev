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
    long PublishedVersion);

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
                    id, page.Slug, page.Title, page.MetaTitle, page.MetaDescription,
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
