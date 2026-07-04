using Imprint.Authoring.Domain.Pages;

namespace Imprint.Authoring.Domain.Sites;

/// <summary>
/// One footer link: a label and a target. The target is imprint's own
/// <see cref="PageLink"/> (same-site page, label falls back to the page title when
/// <see cref="Label"/> is empty) or <see cref="ExternalLink"/> (cross-site absolute URL,
/// used verbatim) — so the marketing footer's mix of in-site and cross-property links
/// (watchdog ↔ assay ↔ cai) both round-trip cleanly.
/// </summary>
public sealed record FooterLink(LocalizedText? Label, Link Link)
{
    /// <summary>The target page when this link points at a same-site page; null for an external link.</summary>
    public PageId? PageId => Link is PageLink page ? page.PageId : null;
}

/// <summary>A named column of footer links (the marketing footer's grouped columns).</summary>
public sealed record FooterLinkGroup(LocalizedText Heading, IReadOnlyList<FooterLink> Links)
{
    // Sequence value-equality on the links, so footer events compare by value on round trip.
    public bool Equals(FooterLinkGroup? other) =>
        other is not null && Equals(Heading, other.Heading) && Links.SequenceEqual(other.Links);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Heading);
        foreach (var link in Links)
        {
            hash.Add(link);
        }

        return hash.ToHashCode();
    }
}

/// <summary>
/// A header call-to-action: a label and a target — used for both the primary CTA button
/// and the quiet link beside it. The target is a <see cref="PageLink"/> or
/// <see cref="ExternalLink"/> (the marketing header points at the app, so it is usually
/// external).
/// </summary>
public sealed record HeaderAction(LocalizedText Label, Link Link)
{
    /// <summary>The target page when this action points at a same-site page; null for an external link.</summary>
    public PageId? PageId => Link is PageLink page ? page.PageId : null;
}

/// <summary>
/// The footer's fine-print copy line (e.g. "© 2025–2026 · The independent surveyor …").
/// A thin localized wrapper so the aggregate treats "no copy line" (null) distinctly from
/// "an empty one", and events carry it by value.
/// </summary>
public sealed record CopyLine(LocalizedText Text);
