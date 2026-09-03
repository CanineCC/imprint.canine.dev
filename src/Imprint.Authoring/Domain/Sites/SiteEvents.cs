using Imprint.EventSourcing;

namespace Imprint.Authoring.Domain.Sites.Events;

// The Site aggregate's events — a closed union, so they share this file (the union is
// the concept). Stable names follow docs/domain-model.md §1; the store persists those,
// never the CLR names.

// Kind is additive on the stored payload, the same contract BaseUrl has on
// site.environments-changed: every site.created written before blogs existed carries no
// Kind and folds to SiteKind.Site, so existing sites keep their identity with no
// migration and no upcaster.
[EventType("site.created", 1)]
public sealed record SiteCreated(
    SiteId SiteId,
    string Name,
    Locale DefaultLocale,
    SiteKind Kind = SiteKind.Site);

[EventType("site.renamed", 1)]
public sealed record SiteRenamed(string Name);

[EventType("site.locale-added", 1)]
public sealed record SiteLocaleAdded(Locale Locale);

[EventType("site.locale-removed", 1)]
public sealed record SiteLocaleRemoved(Locale Locale);

[EventType("site.default-locale-changed", 1)]
public sealed record SiteDefaultLocaleChanged(Locale Locale);

[EventType("site.theme-token-changed", 1)]
public sealed record SiteThemeTokenChanged(string Token, string Light, string Dark);

[EventType("site.typography-changed", 1)]
public sealed record SiteTypographyChanged(Typography Typography);

[EventType("site.navigation-changed", 1)]
public sealed record SiteNavigationChanged(IReadOnlyList<NavigationItem> Items)
{
    // A list-typed positional member silently defeats synthesized record equality
    // (reference compare), and events must compare by value for Given/When/Then and
    // round-trip tests — so equality is by sequence, the ColumnsNode precedent.
    public bool Equals(SiteNavigationChanged? other) =>
        other is not null && Items.SequenceEqual(other.Items);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var item in Items)
        {
            hash.Add(item);
        }

        return hash.ToHashCode();
    }
}

[EventType("site.environments-changed", 1)]
public sealed record SiteEnvironmentsChanged(IReadOnlyList<DeployEnvironment> Environments)
{
    // Same list-equality reasoning as SiteNavigationChanged: the ordered set of deploy
    // targets is the value, so equality is by sequence for Given/When/Then round-trips.
    public bool Equals(SiteEnvironmentsChanged? other) =>
        other is not null && Environments.SequenceEqual(other.Environments);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var environment in Environments)
        {
            hash.Add(environment);
        }

        return hash.ToHashCode();
    }
}

// ── Marketing chrome around the page content: footer columns, header actions, copy ──

[EventType("site.footer-changed", 1)]
public sealed record SiteFooterChanged(IReadOnlyList<FooterLinkGroup> Groups)
{
    // Ordered columns of ordered links are the value — sequence equality, like navigation.
    public bool Equals(SiteFooterChanged? other) =>
        other is not null && Groups.SequenceEqual(other.Groups);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var group in Groups)
        {
            hash.Add(group);
        }

        return hash.ToHashCode();
    }
}

// The two header actions travel together: they occupy the same header slot and the
// editor sets them as a pair, so one event carries both (either may be null).
[EventType("site.header-actions-changed", 1)]
public sealed record SiteHeaderActionsChanged(HeaderAction? Cta, HeaderAction? Quiet);

[EventType("site.copy-line-changed", 1)]
public sealed record SiteCopyLineChanged(CopyLine? CopyLine);

// Which page is served at the site root. Explicit BECAUSE it used to be implicit: the home
// page was whatever sat first in the menu (PageSummary.IsHome was NavigationOrder == 0), so
// dropping "Home" from the navigation silently repointed "/" at the next top-level link and
// took that page's own URL away with it. A site root is not a menu position. Null means
// "fall back to the nav-first page", which is what every site created before this event did.
[EventType("site.home-page-changed", 1)]
public sealed record SiteHomePageChanged(PageId? HomePageId);

// Brand imagery. Each carries the chosen asset id (or null to clear); the asset's bytes
// and variants live in the Asset stream, so the event stays a single reference.
[EventType("site.favicon-changed", 1)]
public sealed record SiteFaviconChanged(AssetId? FaviconAssetId);

[EventType("site.header-logo-changed", 1)]
public sealed record SiteHeaderLogoChanged(AssetId? HeaderLogoAssetId);

// The share card image (og:image) — what a chat app, a social platform or a model shows
// when it is handed the URL instead of the page. Separate from the logo on purpose: this
// one is wide (1200x630-ish) and the header logo is not, and a platform rejects the wrong
// shape rather than cropping it.
[EventType("site.social-image-changed", 1)]
public sealed record SiteSocialImageChanged(AssetId? SocialImageAssetId);

// The llms.txt preamble: what a site says about ITSELF to a machine reading the whole
// file, above the generated page index. Not localized — llms.txt is emitted once per
// site at the root, not once per locale.
[EventType("site.llms-preamble-changed", 1)]
public sealed record SiteLlmsPreambleChanged(string? Preamble);

// Path prefixes the LLM files skip. Stored normalized (lowercase, no leading or trailing
// slash) so the publisher compares them against slug paths without re-parsing on every
// page, on every pass. An empty list is the normal case: no policy.
[EventType("site.llms-excluded-paths-changed", 1)]
public sealed record SiteLlmsExcludedPathsChanged(IReadOnlyList<string> Paths)
{
    // Same list-equality reasoning as SiteNavigationChanged: the set of prefixes is the
    // value, and a positional list member would otherwise compare by reference.
    public bool Equals(SiteLlmsExcludedPathsChanged? other) =>
        other is not null && Paths.SequenceEqual(other.Paths, StringComparer.Ordinal);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var path in Paths)
        {
            hash.Add(path, StringComparer.Ordinal);
        }

        return hash.ToHashCode();
    }
}

// ── Access: who may open and edit the site besides its owner ──
// The email is the same identity the auth layer stamps as the envelope actor, so a
// collaborator's access check and their attribution in history use one value.

// The claimant is the envelope actor (like site.created's owner), so the payload is
// empty: whoever raised the event owns the site from that point on.
[EventType("site.ownership-claimed", 1)]
public sealed record SiteOwnershipClaimed;

// The person who has to clear a post before it reaches the public — "Public Relations Review".
// Per site, because one blog's reviewer is not another's. Null clears the role, and with no
// reviewer a site publishes exactly as it always did.
[EventType("site.reviewer-set", 1)]
public sealed record SiteReviewerSet(string? Name, string? Email);

[EventType("site.collaborator-added", 1)]
public sealed record SiteCollaboratorAdded(string Email);

[EventType("site.collaborator-removed", 1)]
public sealed record SiteCollaboratorRemoved(string Email);
