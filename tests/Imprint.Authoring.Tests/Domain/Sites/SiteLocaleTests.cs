using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Domain.Sites.Events;
using Imprint.TestKit;

namespace Imprint.Authoring.Tests.Domain.Sites;

public sealed class SiteLocaleTests
{
    private static readonly SiteId Id = SiteId.New();
    private static readonly Locale En = new("en");
    private static readonly Locale DaDk = new("da-DK");
    private static SiteCreated Created => new(Id, "Site", En);

    private static object[] HistoryWithLocaleCount(int count)
    {
        var tags = new[] { "da", "de", "fr", "es", "it", "nl", "pt", "sv", "fi" };
        var history = new List<object> { Created };
        history.AddRange(tags.Take(count - 1).Select(tag => new SiteLocaleAdded(new Locale(tag))));
        return [.. history];
    }

    // ------------------------------------------------------------------ AddLocale

    [Fact]
    public void AddLocale_new_locale_raises_locale_added() =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.AddLocale(DaDk))
            .ThenRaised(new SiteLocaleAdded(DaDk));

    [Fact]
    public void AddLocale_keeps_insertion_order()
    {
        var outcome = AggregateSpec.For<Site>()
            .Given(Created, new SiteLocaleAdded(new Locale("de")), new SiteLocaleAdded(DaDk))
            .When(s => s.AddLocale(new Locale("sv")));

        Assert.Equal(new[] { En, new Locale("de"), DaDk, new Locale("sv") }, outcome.Aggregate.Locales);
    }

    [Fact]
    public void AddLocale_already_present_is_rejected() =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.AddLocale(En))
            .ThenFails("already on this site");

    [Fact]
    public void AddLocale_same_locale_with_different_casing_is_rejected() =>
        // Locale normalizes on construction, so 'DA-dk' *is* 'da-DK' — the invariant
        // cannot be dodged by retyping the tag in another case.
        AggregateSpec.For<Site>()
            .Given(Created, new SiteLocaleAdded(new Locale("da-DK")))
            .When(s => s.AddLocale(new Locale("DA-dk")))
            .ThenFails("already on this site");

    [Fact]
    public void AddLocale_tenth_locale_is_accepted() =>
        AggregateSpec.For<Site>()
            .Given(HistoryWithLocaleCount(9))
            .When(s => s.AddLocale(new Locale("pl")))
            .ThenRaised(new SiteLocaleAdded(new Locale("pl")));

    [Fact]
    public void AddLocale_beyond_ten_locales_is_rejected() =>
        AggregateSpec.For<Site>()
            .Given(HistoryWithLocaleCount(10))
            .When(s => s.AddLocale(new Locale("pl")))
            .ThenFails("at most 10");

    // --------------------------------------------------------------- RemoveLocale

    [Fact]
    public void RemoveLocale_non_default_locale_raises_locale_removed() =>
        AggregateSpec.For<Site>()
            .Given(Created, new SiteLocaleAdded(DaDk))
            .When(s => s.RemoveLocale(DaDk))
            .ThenRaised(new SiteLocaleRemoved(DaDk));

    [Fact]
    public void RemoveLocale_with_differently_cased_tag_removes_the_same_locale() =>
        AggregateSpec.For<Site>()
            .Given(Created, new SiteLocaleAdded(DaDk))
            .When(s => s.RemoveLocale(new Locale("DA-dk")))
            .ThenRaised(new SiteLocaleRemoved(DaDk));

    [Fact]
    public void RemoveLocale_absent_locale_is_rejected() =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.RemoveLocale(DaDk))
            .ThenFails("not on this site");

    [Fact]
    public void RemoveLocale_default_locale_is_rejected() =>
        AggregateSpec.For<Site>()
            .Given(Created, new SiteLocaleAdded(DaDk))
            .When(s => s.RemoveLocale(En))
            .ThenFails("default locale");

    [Fact]
    public void RemoveLocale_referenced_by_a_navigation_label_override_is_allowed()
    {
        // Removing a locale that navigation overrides still mention is fine: labels
        // resolve with fallback, so the stale values simply stop being used.
        var item = NavigationItem.Page(PageId.New(), LocalizedText.Of(En, "Home").With(DaDk, "Hjem"));

        AggregateSpec.For<Site>()
            .Given(Created, new SiteLocaleAdded(DaDk), new SiteNavigationChanged([item]))
            .When(s => s.RemoveLocale(DaDk))
            .ThenRaised(new SiteLocaleRemoved(DaDk));
    }

    [Fact]
    public void RemoveLocale_updates_the_locale_list()
    {
        var outcome = AggregateSpec.For<Site>()
            .Given(Created, new SiteLocaleAdded(DaDk), new SiteLocaleAdded(new Locale("de")))
            .When(s => s.RemoveLocale(DaDk));

        Assert.Equal(new[] { En, new Locale("de") }, outcome.Aggregate.Locales);
    }

    // -------------------------------------------------------- ChangeDefaultLocale

    [Fact]
    public void ChangeDefaultLocale_to_a_site_locale_raises_default_changed() =>
        AggregateSpec.For<Site>()
            .Given(Created, new SiteLocaleAdded(DaDk))
            .When(s => s.ChangeDefaultLocale(DaDk))
            .ThenRaised(new SiteDefaultLocaleChanged(DaDk));

    [Fact]
    public void ChangeDefaultLocale_to_an_unknown_locale_is_rejected() =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.ChangeDefaultLocale(DaDk))
            .ThenFails("not one of this site's locales");

    [Fact]
    public void RemoveLocale_previous_default_after_default_changed_is_allowed() =>
        AggregateSpec.For<Site>()
            .Given(Created, new SiteLocaleAdded(DaDk), new SiteDefaultLocaleChanged(DaDk))
            .When(s => s.RemoveLocale(En))
            .ThenRaised(new SiteLocaleRemoved(En));

    [Fact]
    public void RemoveLocale_new_default_after_default_changed_is_rejected() =>
        AggregateSpec.For<Site>()
            .Given(Created, new SiteLocaleAdded(DaDk), new SiteDefaultLocaleChanged(DaDk))
            .When(s => s.RemoveLocale(DaDk))
            .ThenFails("default locale");
}
