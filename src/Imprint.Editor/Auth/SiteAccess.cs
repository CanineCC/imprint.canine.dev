using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Projections;

namespace Imprint.Editor.Auth;

/// <summary>
/// The per-circuit answer to "which sites may this user touch". Enforcement exists only
/// when auth is enabled: an open install (dev, tests, trusted LAN) keeps its historical
/// behaviour — every site visible and editable, the OS user as the actor. When enforced,
/// a user sees the sites they own plus the ones they were added to as a collaborator
/// (and legacy sites with no recorded owner, so nothing is ever orphaned).
///
/// This gates the UI entry points (dashboard list, opening a page, the settings page).
/// In Blazor Server those entry points ARE the attack surface for site access — every
/// command is dispatched from a component that first had to get past one of them.
/// </summary>
public sealed class SiteAccess(KeycloakOptions auth, EditorActor actor, SiteOverview sites)
{
    /// <summary>Whether per-site access control is active (i.e. auth is enabled).</summary>
    public bool Enforced => auth.Enabled;

    /// <summary>The signed-in user's email, or null when auth is off.</summary>
    public ValueTask<string?> UserAsync() => Enforced ? actor.IdentityAsync() : ValueTask.FromResult<string?>(null);

    public async ValueTask<IReadOnlyList<Site>> SitesAsync() =>
        Enforced ? sites.AccessibleTo(await actor.IdentityAsync() ?? string.Empty) : sites.All;

    public async ValueTask<bool> CanAccessAsync(SiteId id) =>
        !Enforced || sites.CanAccess(id, await actor.IdentityAsync() ?? string.Empty);

    /// <summary>Owner-only surfaces (managing who has access). Everyone owns everything when auth is off.</summary>
    public async ValueTask<bool> IsOwnerAsync(SiteId id) =>
        !Enforced || sites.IsOwner(id, await actor.IdentityAsync() ?? string.Empty);
}
