using Imprint.Authoring.Domain.Pages;

namespace Imprint.Authoring.Domain.Sites;

/// <summary>
/// A child link inside a navigation group (one level deep, mirroring the marketing
/// header's dropdown cards). <see cref="Link"/> is imprint's own <see cref="PageLink"/>
/// (a same-site page, whose label falls back to the page title when
/// <see cref="Label"/> is empty) or <see cref="ExternalLink"/> (a cross-site absolute
/// URL — the label is then required and used verbatim). <see cref="Description"/> is the
/// optional supporting line shown under the label in the dropdown card.
/// </summary>
public sealed record NavigationChild(LocalizedText? Label, Link Link, LocalizedText? Description = null)
{
    /// <summary>The target page when this child points at a same-site page; null for an external link.</summary>
    public PageId? PageId => Link is PageLink page ? page.PageId : null;
}

/// <summary>
/// One top-level navigation entry. It is EITHER a direct link (<see cref="Link"/> set,
/// <see cref="Children"/> empty) OR a group (<see cref="Children"/> non-empty,
/// <see cref="Link"/> null and only <see cref="Label"/> shown as the group heading) —
/// exactly the marketing header's shape (<c>NavItem = { label, href } | { label, menu[] }</c>).
///
/// The label override stays optional on a direct <em>page</em> link so renaming a page
/// keeps the navigation in sync for free; on an external link or a group the label is the
/// only text there is, so it must be present (an aggregate invariant, not enforced here).
/// </summary>
public sealed record NavigationItem
{
    public LocalizedText? Label { get; init; }

    /// <summary>The direct target of this entry, or null when the entry is a group.</summary>
    public Link? Link { get; init; }

    /// <summary>The group's children; empty for a direct link.</summary>
    public IReadOnlyList<NavigationChild> Children { get; init; } = [];

    /// <summary>True when this entry is a group (has children) rather than a direct link.</summary>
    public bool IsGroup => Children.Count > 0;

    /// <summary>The target page when this entry is a direct same-site page link; null otherwise.</summary>
    public PageId? PageId => Link is PageLink page ? page.PageId : null;

    // List-typed member defeats synthesized record equality (reference compare), yet
    // events and the aggregate's no-op check compare navigation by value — so children
    // are compared by sequence, the ColumnsNode/SiteNavigationChanged precedent.
    public bool Equals(NavigationItem? other) =>
        other is not null &&
        Equals(Label, other.Label) &&
        Equals(Link, other.Link) &&
        Children.SequenceEqual(other.Children);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Label);
        hash.Add(Link);
        foreach (var child in Children)
        {
            hash.Add(child);
        }

        return hash.ToHashCode();
    }

    /// <summary>A direct same-site page link — the classic entry the editor and templates build.</summary>
    public static NavigationItem Page(PageId pageId, LocalizedText? labelOverride = null) =>
        new() { Label = labelOverride, Link = new PageLink(pageId) };

    /// <summary>A direct external (cross-site) link; the label is used verbatim.</summary>
    public static NavigationItem External(LocalizedText label, string url) =>
        new() { Label = label, Link = new ExternalLink(url) };

    /// <summary>A group heading with its dropdown children.</summary>
    public static NavigationItem Group(LocalizedText label, IReadOnlyList<NavigationChild> children) =>
        new() { Label = label, Children = [.. children] };
}
