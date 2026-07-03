using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Pages.Events;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Projections;

/// <summary>
/// The editor canvas's read model: the current draft state of every page.
///
/// Deliberately unorthodox: it holds <see cref="Page"/> aggregate instances and folds
/// each event through the aggregate's own <c>When</c> (via <c>LoadFrom</c>). Read
/// models usually maintain independent state, but this one's shape IS the aggregate's
/// state — the editor edits pages — and duplicating the tree-fold logic would be a
/// divergence bug waiting to happen. The instances are never used to decide or raise
/// events here; they are folded state, nothing more.
/// </summary>
public sealed class PageDrafts : ReadModel
{
    private readonly Dictionary<PageId, Page> _pages = [];

    public Page? Get(PageId id) => _pages.GetValueOrDefault(id);

    public IReadOnlyCollection<Page> All => _pages.Values;

    public override void Apply(StoredEvent @event)
    {
        if (StreamIds.IdOf(@event.StreamId, "page-") is not { } guid)
        {
            return;
        }

        var id = PageId.From(guid);
        if (@event.Event is PageCreated)
        {
            var page = new Page();
            page.LoadFrom([@event.Event]);
            _pages[id] = page;
        }
        else if (_pages.TryGetValue(id, out var page))
        {
            page.LoadFrom([@event.Event]);
            if (page.IsDeleted)
            {
                _pages.Remove(id);
            }
        }
        else
        {
            throw new InvalidOperationException(
                $"Page event {@event.StableId} for unknown page {id} — corrupt sequence?");
        }

        NotifyChanged();
    }

    public override void Reset() => _pages.Clear();
}
