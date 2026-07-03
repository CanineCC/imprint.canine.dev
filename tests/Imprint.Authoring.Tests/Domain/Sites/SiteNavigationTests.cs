using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Domain.Sites.Events;
using Imprint.TestKit;

namespace Imprint.Authoring.Tests.Domain.Sites;

public sealed class SiteNavigationTests
{
    private static readonly SiteId Id = SiteId.New();
    private static readonly Locale En = new("en");
    private static SiteCreated Created => new(Id, "Site", En);

    private static NavigationItem[] ItemCount(int count) =>
        [.. Enumerable.Range(0, count).Select(_ => new NavigationItem(PageId.New(), null))];

    [Fact]
    public void SetNavigation_valid_items_raises_navigation_changed()
    {
        var items = new[]
        {
            new NavigationItem(PageId.New(), null),
            new NavigationItem(PageId.New(), LocalizedText.Of(En, "About us")),
        };

        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetNavigation(items))
            .ThenRaised(new SiteNavigationChanged(items));
    }

    [Fact]
    public void SetNavigation_with_duplicate_pages_is_rejected()
    {
        var pageId = PageId.New();
        var items = new[]
        {
            new NavigationItem(pageId, null),
            new NavigationItem(pageId, LocalizedText.Of(En, "Same page, other label")),
        };

        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetNavigation(items))
            .ThenFails("same page twice");
    }

    [Fact]
    public void SetNavigation_with_twenty_items_is_accepted()
    {
        var items = ItemCount(20);

        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetNavigation(items))
            .ThenRaised(new SiteNavigationChanged(items));
    }

    [Fact]
    public void SetNavigation_with_more_than_twenty_items_is_rejected() =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetNavigation(ItemCount(21)))
            .ThenFails("at most 20");

    [Fact]
    public void SetNavigation_with_unchanged_items_raises_nothing()
    {
        // Fresh but value-equal instances: the no-op check is sequence *value* equality,
        // not reference identity — the editor always sends a newly built list.
        var pageId = PageId.New();

        AggregateSpec.For<Site>()
            .Given(Created, new SiteNavigationChanged([new NavigationItem(pageId, LocalizedText.Of(En, "Home"))]))
            .When(s => s.SetNavigation([new NavigationItem(pageId, LocalizedText.Of(En, "Home"))]))
            .ThenNothing();
    }

    [Fact]
    public void SetNavigation_with_reordered_items_raises_navigation_changed()
    {
        var first = new NavigationItem(PageId.New(), null);
        var second = new NavigationItem(PageId.New(), null);

        AggregateSpec.For<Site>()
            .Given(Created, new SiteNavigationChanged([first, second]))
            .When(s => s.SetNavigation([second, first]))
            .ThenRaised(new SiteNavigationChanged([second, first]));
    }

    [Fact]
    public void SetNavigation_clearing_existing_navigation_raises_navigation_changed() =>
        AggregateSpec.For<Site>()
            .Given(Created, new SiteNavigationChanged([new NavigationItem(PageId.New(), null)]))
            .When(s => s.SetNavigation([]))
            .ThenRaised(new SiteNavigationChanged([]));

    [Fact]
    public void SetNavigation_empty_when_already_empty_raises_nothing() =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetNavigation([]))
            .ThenNothing();

    [Fact]
    public void SetNavigation_updates_the_navigation_state()
    {
        var items = new[]
        {
            new NavigationItem(PageId.New(), LocalizedText.Of(En, "Home")),
            new NavigationItem(PageId.New(), null),
        };

        var outcome = AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetNavigation(items));

        Assert.Equal(items, outcome.Aggregate.Navigation);
    }
}
