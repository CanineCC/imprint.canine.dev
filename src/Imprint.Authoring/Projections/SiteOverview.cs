using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Domain.Sites.Events;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Projections;

/// <summary>
/// Site settings for the editor and the publisher, folded through the Site aggregate
/// (same pattern and rationale as <see cref="PageDrafts"/>). The editor manages one
/// site; the domain supports many — <see cref="Current"/> is the first created.
/// <c>Site.Version</c> doubles as the chrome version for publish staleness.
/// </summary>
public sealed class SiteOverview : ReadModel
{
    private readonly Dictionary<SiteId, Site> _sites = [];
    private SiteId? _first;

    public Site? Current => _first is { } id ? _sites.GetValueOrDefault(id) : null;

    public Site? Get(SiteId id) => _sites.GetValueOrDefault(id);

    public override void Apply(StoredEvent @event)
    {
        if (StreamIds.IdOf(@event.StreamId, "site-") is not { } guid)
        {
            return;
        }

        var id = SiteId.From(guid);
        if (@event.Event is SiteCreated)
        {
            var site = new Site();
            site.LoadFrom([@event.Event]);
            _sites[id] = site;
            _first ??= id;
        }
        else if (_sites.TryGetValue(id, out var site))
        {
            site.LoadFrom([@event.Event]);
        }
        else
        {
            throw new InvalidOperationException(
                $"Site event {@event.StableId} for unknown site {id} — corrupt sequence?");
        }

        NotifyChanged();
    }

    public override void Reset()
    {
        _sites.Clear();
        _first = null;
    }
}
