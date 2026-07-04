using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Domain.Sites.Events;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Projections;

/// <summary>
/// Site settings for the editor and the publisher, folded through the Site aggregate
/// (same pattern and rationale as <see cref="PageDrafts"/>). The domain has always been
/// multi-site (one <c>site-{id}</c> stream each); this read model exposes them all, in
/// creation order, and records each site's <em>owner</em> — the actor who raised its
/// <c>site.created</c> event (from the envelope metadata, so no event-schema change).
/// <see cref="Current"/> is retained as "the first created" for the single-site call
/// sites still being migrated. <c>Site.Version</c> doubles as the chrome version for
/// publish staleness.
/// </summary>
public sealed class SiteOverview : ReadModel
{
    private readonly Dictionary<SiteId, Site> _sites = [];
    private readonly Dictionary<SiteId, string> _owners = [];

    // Creation order so the editor's site list is stable (a dictionary is not ordered).
    private readonly List<SiteId> _order = [];

    public Site? Current => _order.Count > 0 ? _sites.GetValueOrDefault(_order[0]) : null;

    public Site? Get(SiteId id) => _sites.GetValueOrDefault(id);

    /// <summary>Every site, in creation order.</summary>
    public IReadOnlyList<Site> All => [.. _order.Select(id => _sites[id])];

    /// <summary>The actor who created the site (empty string when unknown / legacy).</summary>
    public string OwnerOf(SiteId id) => _owners.GetValueOrDefault(id, string.Empty);

    /// <summary>
    /// Sites owned by <paramref name="actor"/>, in creation order. A legacy site created
    /// before ownership was recorded (empty owner) is visible to everyone, so a
    /// single-tenant install keeps working and no site is ever orphaned.
    /// </summary>
    public IReadOnlyList<Site> OwnedBy(string actor) =>
    [
        .. _order
            .Where(id => _owners.GetValueOrDefault(id, string.Empty) is var owner
                && (owner.Length == 0 || string.Equals(owner, actor, StringComparison.OrdinalIgnoreCase)))
            .Select(id => _sites[id]),
    ];

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
            if (!_sites.ContainsKey(id))
            {
                _order.Add(id);
            }

            _sites[id] = site;
            // Ownership rides the creation event's envelope actor — the SaaS "who owns
            // this site" answer without changing the site.created payload.
            _owners[id] = @event.Metadata.Actor ?? string.Empty;
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
        _owners.Clear();
        _order.Clear();
    }
}
