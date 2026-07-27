using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Domain.Sites.Events;
using Imprint.TestKit;

namespace Imprint.Authoring.Tests.Domain.Sites;

/// <summary>
/// Seeding the chrome is the more visible half of adding a locale: a header and footer
/// left in the wrong language show on every page, while an unseeded page merely still
/// reads in the language it was written in.
/// </summary>
public sealed class SiteSeedLocaleTests
{
    private static readonly SiteId Id = SiteId.New();
    private static readonly Locale En = new("en");
    private static readonly Locale Da = new("da");
    private static SiteCreated Created => new(Id, "Site", En);

    private static LocalizedText T(string value) => LocalizedText.Of(En, value);

    private static object[] SiteWithChrome(params object[] extra) =>
    [
        Created,
        new SiteLocaleAdded(Da),
        new SiteNavigationChanged([NavigationItem.External(T("Sign in"), "https://app.example.com/")]),
        new SiteFooterChanged([new FooterLinkGroup(T("Product"), [new FooterLink(T("Pricing"), new ExternalLink("https://x/p"))])]),
        new SiteCopyLineChanged(new CopyLine(T("(c) Example"))),
        .. extra,
    ];

    [Fact]
    public void SeedChromeLocale_copies_navigation_footer_and_copy_line()
    {
        var outcome = AggregateSpec.For<Site>()
            .Given(SiteWithChrome())
            .When(s => s.SeedChromeLocale(Da, En));

        Assert.Equal("Sign in", outcome.Aggregate.Navigation[0].Label!.Get(Da));
        Assert.Equal("Product", outcome.Aggregate.FooterGroups[0].Heading.Get(Da));
        Assert.Equal("Pricing", outcome.Aggregate.FooterGroups[0].Links[0].Label!.Get(Da));
        Assert.Equal("(c) Example", outcome.Aggregate.CopyLine!.Text.Get(Da));
    }

    [Fact]
    public void SeedChromeLocale_keeps_the_source_locale_intact()
    {
        var outcome = AggregateSpec.For<Site>()
            .Given(SiteWithChrome())
            .When(s => s.SeedChromeLocale(Da, En));

        Assert.Equal("Sign in", outcome.Aggregate.Navigation[0].Label!.Get(En));
        Assert.Equal("(c) Example", outcome.Aggregate.CopyLine!.Text.Get(En));
    }

    [Fact]
    public void SeedChromeLocale_leaves_a_label_already_translated()
    {
        var translated = new[]
        {
            NavigationItem.External(T("Sign in").With(Da, "Log ind"), "https://app.example.com/"),
        };

        var outcome = AggregateSpec.For<Site>()
            .Given(SiteWithChrome(new SiteNavigationChanged(translated)))
            .When(s => s.SeedChromeLocale(Da, En));

        Assert.Equal("Log ind", outcome.Aggregate.Navigation[0].Label!.Get(Da));
    }

    [Fact]
    public void SeedChromeLocale_run_twice_changes_nothing_the_second_time()
    {
        var once = AggregateSpec.For<Site>()
            .Given(SiteWithChrome())
            .When(s => s.SeedChromeLocale(Da, En));

        var twice = AggregateSpec.For<Site>()
            .Given([.. SiteWithChrome(), .. once.Raised])
            .When(s => s.SeedChromeLocale(Da, En));

        Assert.Empty(twice.Raised);
    }

    [Fact]
    public void SeedChromeLocale_from_itself_is_rejected() =>
        AggregateSpec.For<Site>()
            .Given(SiteWithChrome())
            .When(s => s.SeedChromeLocale(Da, Da))
            .ThenFails("cannot be seeded from itself");
}
