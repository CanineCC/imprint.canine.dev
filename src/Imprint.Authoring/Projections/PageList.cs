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
        Slug Slug, LocalizedText Title, long Version, long? PublishedVersion, DateTimeOffset UpdatedAt);

    private readonly Dictionary<PageId, Entry> _pages = [];
    private List<PageId> _navigation = [];

    /// <summary>Navigation pages first (in nav order), then the rest by slug.</summary>
    public IReadOnlyList<PageSummary> All()
    {
        var summaries = _pages.Select(pair => Summarize(pair.Key, pair.Value)).ToList();
        return
        [
            .. summaries
                .OrderBy(page => page.NavigationOrder ?? int.MaxValue)
                .ThenBy(page => page.Slug.Value, StringComparer.Ordinal),
        ];
    }

    public PageSummary? Get(PageId id) =>
        _pages.TryGetValue(id, out var entry) ? Summarize(id, entry) : null;

    public bool SlugTaken(Slug slug, PageId? except = null) =>
        _pages.Any(pair => pair.Value.Slug == slug && pair.Key != except);

    public PageSummary? Home => All().FirstOrDefault(page => page.IsHome);

    public override void Apply(StoredEvent @event)
    {
        switch (@event.Event)
        {
            case PageCreated created:
                Slug.TryCreate(created.Slug, out var createdSlug, out _);
                _pages[created.PageId] = new Entry(
                    createdSlug,
                    LocalizedText.Of(created.InitialLocale, created.Title),
                    Version: 1,
                    PublishedVersion: null,
                    @event.Metadata.TimestampUtc);
                break;

            case SiteNavigationChanged navigation:
                _navigation = [.. navigation.Items.Select(item => item.PageId)];
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
        _navigation = [];
    }

    private PageSummary Summarize(PageId id, Entry entry)
    {
        var order = _navigation.IndexOf(id);
        return new PageSummary(
            id, entry.Slug, entry.Title, entry.Version, entry.PublishedVersion,
            order < 0 ? null : order, entry.UpdatedAt);
    }
}
