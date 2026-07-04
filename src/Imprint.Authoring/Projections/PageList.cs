using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Pages.Events;
using Imprint.Authoring.Domain.Sites.Events;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Projections;

public enum PageStatus
{
    /// <summary>Never published.</summary>
    Draft,

    /// <summary>Published and unchanged since.</summary>
    Published,

    /// <summary>Published, but the draft has newer changes.</summary>
    Modified,
}

public sealed record PageSummary(
    PageId Id,
    Slug Slug,
    LocalizedText Title,
    long Version,
    long? PublishedVersion,
    int? NavigationOrder,
    DateTimeOffset UpdatedAt)
{
    public PageStatus Status => PublishedVersion switch
    {
        null => PageStatus.Draft,
        // PagePublished.Version is the stream position of the publish event itself,
        // so "any event after it" means the draft moved on.
        var published when Version > published => PageStatus.Modified,
        _ => PageStatus.Published,
    };

    public bool IsInNavigation => NavigationOrder is not null;

    /// <summary>The nav-first page is the home page, rendered at the site root.</summary>
    public bool IsHome => NavigationOrder == 0;
}

/// <summary>
/// The dashboard/list read model: slugs, titles, publish status, navigation
/// membership. Also the slug-uniqueness oracle used by slices (an accepted
/// eventual-consistency race — docs/architecture.md §Consistency).
/// </summary>
public sealed class PageList : ReadModel
{
    private sealed record Entry(
        SiteId SiteId, Slug Slug, LocalizedText Title, long Version, long? PublishedVersion, DateTimeOffset UpdatedAt);

    private readonly Dictionary<PageId, Entry> _pages = [];

    // Navigation is per-site (each site owns its own menu), keyed by the site stream the
    // SiteNavigationChanged event came from. A single shared list would let one site's
    // menu decide another's home page and nav order.
    private readonly Dictionary<SiteId, List<PageId>> _navigation = [];

    /// <summary>Every page across all sites — navigation pages first, then the rest by slug.</summary>
    public IReadOnlyList<PageSummary> All() => Ordered(_pages.Select(pair => Summarize(pair.Key, pair.Value)));

    /// <summary>The pages of one site — the multi-site dashboard/nav view.</summary>
    public IReadOnlyList<PageSummary> All(SiteId site) =>
        Ordered(_pages.Where(pair => pair.Value.SiteId == site).Select(pair => Summarize(pair.Key, pair.Value)));

    private static IReadOnlyList<PageSummary> Ordered(IEnumerable<PageSummary> summaries) =>
    [
        .. summaries
            .OrderBy(page => page.NavigationOrder ?? int.MaxValue)
            .ThenBy(page => page.Slug.Value, StringComparer.Ordinal),
    ];

    public PageSummary? Get(PageId id) =>
        _pages.TryGetValue(id, out var entry) ? Summarize(id, entry) : null;

    /// <summary>
    /// Whether a slug is already used <em>within the given site</em>. Slugs are unique per
    /// site, not globally — two different sites may each have a 'home' or 'about' page.
    /// </summary>
    public bool SlugTaken(SiteId site, Slug slug, PageId? except = null) =>
        _pages.Any(pair => pair.Value.SiteId == site && pair.Value.Slug == slug && pair.Key != except);

    /// <summary>The home page of one site (nav-first), for multi-site publishing/routing.</summary>
    public PageSummary? HomeOf(SiteId site) => All(site).FirstOrDefault(page => page.IsHome);

    public PageSummary? Home => All().FirstOrDefault(page => page.IsHome);

    public override void Apply(StoredEvent @event)
    {
        switch (@event.Event)
        {
            case PageCreated created:
                Slug.TryCreate(created.Slug, out var createdSlug, out _);
                _pages[created.PageId] = new Entry(
                    created.SiteId,
                    createdSlug,
                    LocalizedText.Of(created.InitialLocale, created.Title),
                    Version: 1,
                    PublishedVersion: null,
                    @event.Metadata.TimestampUtc);
                break;

            case SiteNavigationChanged navigation when StreamIds.IdOf(@event.StreamId, "site-") is { } siteGuid:
                // Only top-level direct page links carry a PageId and therefore a
                // navigation order / home candidacy; group headings and external links
                // are chrome, not pages, so they never decide a site's home page.
                _navigation[SiteId.From(siteGuid)] =
                    [.. navigation.Items.Select(item => item.PageId).OfType<PageId>()];
                break;

            default:
                if (StreamIds.IdOf(@event.StreamId, "page-") is not { } guid ||
                    !_pages.TryGetValue(PageId.From(guid), out var entry))
                {
                    return;
                }

                var id = PageId.From(guid);
                entry = entry with { Version = @event.StreamVersion, UpdatedAt = @event.Metadata.TimestampUtc };
                entry = @event.Event switch
                {
                    SlugChanged slugChanged when Slug.TryCreate(slugChanged.Slug, out var changed, out _) =>
                        entry with { Slug = changed },
                    TitleChanged titleChanged => entry with
                    {
                        Title = entry.Title.With(titleChanged.Locale, titleChanged.Title),
                    },
                    PagePublished published => entry with { PublishedVersion = published.Version },
                    PageUnpublished => entry with { PublishedVersion = null },
                    _ => entry,
                };

                if (@event.Event is PageDeleted)
                {
                    _pages.Remove(id);
                }
                else
                {
                    _pages[id] = entry;
                }

                break;
        }

        NotifyChanged();
    }

    public override void Reset()
    {
        _pages.Clear();
        _navigation.Clear();
    }

    private PageSummary Summarize(PageId id, Entry entry)
    {
        // Navigation order is looked up in the page's OWN site's menu, so a page is only
        // "home" (order 0) relative to its site.
        var order = _navigation.GetValueOrDefault(entry.SiteId)?.IndexOf(id) ?? -1;
        return new PageSummary(
            id, entry.Slug, entry.Title, entry.Version, entry.PublishedVersion,
            order < 0 ? null : order, entry.UpdatedAt);
    }
}
