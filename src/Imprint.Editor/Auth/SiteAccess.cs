using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Posts;
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

    /// <summary>
    /// What the signed-in user may do with one post. Site access answers this for the people who
    /// own the site; it cannot answer it for the REVIEWER, who is mailed a link to a single post
    /// and is, by design, not a collaborator (<see cref="Site.SetReviewer"/> grants no access on
    /// purpose). Without a pass of their own the reviewer was redirected off the page they were
    /// asked to act on — so the one job the mail describes was impossible to do.
    ///
    /// <para>The pass covers a post that has actually been HANDED to them — anything that has been
    /// through review (<paramref name="review"/> is not <see cref="PostReview.None"/>). It does not
    /// open the site's drafts: a reviewer sees the documents they were sent and nothing else, and
    /// a post whose approval lapsed returns to the author until it is submitted again.</para>
    /// </summary>
    public async ValueTask<PostPass> PassForAsync(SiteId siteId, PostReview review)
    {
        if (!Enforced)
        {
            return PostPass.Edit;
        }

        var me = await actor.IdentityAsync() ?? string.Empty;
        if (sites.CanAccess(siteId, me))
        {
            return PostPass.Edit;
        }

        return review is not PostReview.None
               && sites.Get(siteId) is { ReviewerEmail: { Length: > 0 } reviewer }
               && string.Equals(reviewer, me, StringComparison.OrdinalIgnoreCase)
            ? PostPass.Review
            : PostPass.None;
    }
}
