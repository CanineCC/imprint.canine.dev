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
