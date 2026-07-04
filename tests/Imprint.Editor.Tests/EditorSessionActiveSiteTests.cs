using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Projections;
using Imprint.Editor.Services;
using Imprint.EventSourcing;

namespace Imprint.Editor.Tests;

/// <summary>
/// The multi-site editing invariant: the editor edits the site that OWNS the open page,
/// not whichever site happens to be first. Everything chrome-related (default locale,
/// theme, page list) hangs off <see cref="EditorSession.ActiveSite"/>, so this is the
/// property that makes a dashboard card open the right site.
/// </summary>
public sealed class EditorSessionActiveSiteTests
{
    private static readonly EventMetadata Meta = new("alice", DateTimeOffset.UnixEpoch, Guid.Empty, Guid.Empty);

    // Folds an aggregate's uncommitted events into the given read models, exactly as the
    // projection engine would — no test shortcut into the read model's internals.
    private static void Fold(AggregateRoot aggregate, ref long position, params ReadModel[] readModels)
    {
        long version = 0;
        foreach (var @event in aggregate.UncommittedEvents)
        {
            var stored = new StoredEvent(
                ++position, aggregate.StreamId, ++version, @event.GetType().Name, @event, Meta);
            foreach (var readModel in readModels)
            {
                readModel.Apply(stored);
            }
        }
    }

    private static Slug MakeSlug(string value)
    {
        Assert.True(Slug.TryCreate(value, out var slug, out var error), error);
        return slug;
    }

    [Fact]
    public void ActiveSite_follows_the_open_pages_site_not_the_first_site()
    {
        var sites = new SiteOverview();
        var drafts = new PageDrafts();
        long position = 0;

        var firstId = SiteId.New();
        Fold(Site.Create(firstId, "First", new Locale("en")), ref position, sites);
        var secondId = SiteId.New();
        Fold(Site.Create(secondId, "Second", new Locale("da")), ref position, sites);

        var pageInSecond = Page.Create(PageId.New(), secondId, MakeSlug("about"), new Locale("da"), "About");
        Fold(pageInSecond, ref position, drafts);

        var session = new EditorSession(drafts, sites);

        // Nothing open yet → the first site (the single-site fallback).
        Assert.Equal(firstId, session.ActiveSiteId);

        // Open a page owned by the SECOND site → the editor now edits the second site,
        // and the default edit locale follows it.
        session.OpenPage(pageInSecond.Id);
        Assert.Equal(secondId, session.ActiveSiteId);
        Assert.Equal(new Locale("da"), session.EditLocale);
    }

    [Fact]
    public void ActiveSite_is_the_first_site_when_no_page_is_open()
    {
        var sites = new SiteOverview();
        long position = 0;
        var onlyId = SiteId.New();
        Fold(Site.Create(onlyId, "Only", new Locale("en")), ref position, sites);

        var session = new EditorSession(new PageDrafts(), sites);

        Assert.Equal(onlyId, session.ActiveSiteId);
    }

    [Fact]
    public void ActiveSite_is_null_for_an_unknown_open_page_not_the_first_site()
    {
        // A stale/shared link or a page deleted in another tab: the page id is set but
        // resolves to nothing. ActiveSite must NOT silently fall back to the first site
        // (which would let the editor write to the wrong site) — it must be null so the
        // chrome shows an empty state.
        var sites = new SiteOverview();
        long position = 0;
        Fold(Site.Create(SiteId.New(), "First", new Locale("en")), ref position, sites);

        var session = new EditorSession(new PageDrafts(), sites);
        session.OpenPage(PageId.New()); // never created

        Assert.Null(session.ActiveSite);
        Assert.Null(session.ActiveSiteId);
    }
}
