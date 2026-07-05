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
        [.. _order.Where(id => IsOwner(id, actor)).Select(id => _sites[id])];

    /// <summary>
    /// Whether <paramref name="actor"/> is the site's owner. A legacy site with no
    /// recorded owner (empty string) counts as everyone's — same rationale as
    /// <see cref="OwnedBy"/>.
    /// </summary>
    public bool IsOwner(SiteId id, string actor) =>
        _owners.GetValueOrDefault(id, string.Empty) is var owner
        && (owner.Length == 0 || string.Equals(owner, actor, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Whether <paramref name="actor"/> may open and edit the site: its owner, one of
    /// its collaborators (<see cref="Site.Collaborators"/>), or anyone when the site
    /// predates ownership.
    /// </summary>
    public bool CanAccess(SiteId id, string actor) =>
        _sites.TryGetValue(id, out var site)
        && (IsOwner(id, actor) || site.Collaborators.Contains(actor, StringComparer.OrdinalIgnoreCase));

    /// <summary>The sites <paramref name="actor"/> may edit, in creation order — the dashboard list.</summary>
    public IReadOnlyList<Site> AccessibleTo(string actor) =>
        [.. _order.Where(id => CanAccess(id, actor)).Select(id => _sites[id])];

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
