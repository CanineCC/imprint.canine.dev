using Imprint.EventSourcing;

namespace Imprint.Authoring.Domain.Sites.Events;

// The Site aggregate's events — a closed union, so they share this file (the union is
// the concept). Stable names follow docs/domain-model.md §1; the store persists those,
// never the CLR names.

[EventType("site.created", 1)]
public sealed record SiteCreated(SiteId SiteId, string Name, Locale DefaultLocale);

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
