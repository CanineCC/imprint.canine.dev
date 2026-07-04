using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Domain.Sites.Events;
using Imprint.TestKit;

namespace Imprint.Authoring.Tests.Domain.Sites;

/// <summary>
/// The marketing-chrome setters on the Site aggregate: hierarchical navigation (direct
/// links, external links, groups), footer link columns, header actions and the copy line
/// — each idempotent on an unchanged value, each with its shape invariants.
/// </summary>
public sealed class SiteChromeTests
{
    private static readonly SiteId Id = SiteId.New();
    private static readonly Locale En = new("en");
    private static SiteCreated Created => new(Id, "Site", En);

    private static LocalizedText T(string value) => LocalizedText.Of(En, value);

    // ------------------------------------------------------------ hierarchical nav

    [Fact]
    public void SetNavigation_accepts_a_direct_external_link()
    {
        var items = new[] { NavigationItem.External(T("Sign in"), "https://app.example.com/") };

        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetNavigation(items))
            .ThenRaised(new SiteNavigationChanged(items));
    }

    [Fact]
    public void SetNavigation_accepts_a_group_with_page_and_external_children()
    {
        var items = new[]
        {
            NavigationItem.Group(T("Why us"),
            [
                new NavigationChild(T("What we measure"), new PageLink(PageId.New()), T("The lenses")),
                new NavigationChild(T("The standard"), new ExternalLink("https://cai.example.com/spec")),
            ]),
        };

        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetNavigation(items))
            .ThenRaised(new SiteNavigationChanged(items));
    }

    [Fact]
    public void SetNavigation_group_without_a_label_is_rejected() =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetNavigation([NavigationItem.Group(LocalizedText.Empty,
                [new NavigationChild(T("Child"), new ExternalLink("https://example.com"))])]))
            .ThenFails("group must have a label");

    [Fact]
    public void SetNavigation_external_link_without_a_label_is_rejected() =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetNavigation([new NavigationItem { Link = new ExternalLink("https://example.com") }]))
            .ThenFails("external navigation link must have a label");

    [Fact]
    public void SetNavigation_the_same_page_twice_at_top_level_is_rejected()
    {
        var pageId = PageId.New();

        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetNavigation([NavigationItem.Page(pageId), NavigationItem.Page(pageId)]))
            .ThenFails("same page twice");
    }

    [Fact]
    public void SetNavigation_the_same_page_as_a_group_child_and_a_top_level_link_is_allowed()
    {
        // A page may be both a direct link and a dropdown child — only DIRECT top-level
        // page links must be unique (they decide navigation order / the home page).
        var pageId = PageId.New();
        var items = new[]
        {
            NavigationItem.Page(pageId),
            NavigationItem.Group(T("More"), [new NavigationChild(T("Same page"), new PageLink(pageId))]),
        };

        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetNavigation(items))
            .ThenRaised(new SiteNavigationChanged(items));
    }

    [Fact]
    public void SetNavigation_with_more_than_the_child_cap_is_rejected()
    {
        var children = Enumerable.Range(0, Site.MaxNavigationChildren + 1)
            .Select(i => new NavigationChild(T($"Child {i}"), new ExternalLink($"https://example.com/{i}")))
            .ToArray();

        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetNavigation([NavigationItem.Group(T("Big"), children)]))
            .ThenFails($"at most {Site.MaxNavigationChildren}");
    }

    // ------------------------------------------------------------------- footer

    [Fact]
    public void SetFooter_valid_groups_raise_footer_changed()
    {
        var groups = new[]
        {
            new FooterLinkGroup(T("Product"),
            [
                new FooterLink(null, new PageLink(PageId.New())),
                new FooterLink(T("cai"), new ExternalLink("https://cai.example.com")),
            ]),
        };

        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetFooter(groups))
            .ThenRaised(new SiteFooterChanged(groups));
    }

    [Fact]
    public void SetFooter_updates_the_footer_state()
    {
        var groups = new[] { new FooterLinkGroup(T("Trust"), [new FooterLink(T("Terms"), new ExternalLink("https://x/tos"))]) };

        var outcome = AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetFooter(groups));

        Assert.Equal(groups, outcome.Aggregate.FooterGroups);
    }

    [Fact]
    public void SetFooter_group_without_a_heading_is_rejected() =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetFooter([new FooterLinkGroup(LocalizedText.Empty, [])]))
            .ThenFails("must have a heading");

    [Fact]
    public void SetFooter_external_link_without_a_label_is_rejected() =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetFooter([new FooterLinkGroup(T("Trust"),
                [new FooterLink(null, new ExternalLink("https://x/tos"))])]))
            .ThenFails("external footer link must have a label");

    [Fact]
    public void SetFooter_with_unchanged_groups_raises_nothing()
    {
        var pageId = PageId.New();
        FooterLinkGroup[] Groups() => [new FooterLinkGroup(T("Product"), [new FooterLink(T("About"), new PageLink(pageId))])];

        AggregateSpec.For<Site>()
            .Given(Created, new SiteFooterChanged(Groups()))
            .When(s => s.SetFooter(Groups()))
            .ThenNothing();
    }

    // --------------------------------------------------------------- header actions

    [Fact]
    public void SetHeaderActions_raises_actions_changed()
    {
        var cta = new HeaderAction(T("Survey a repo"), new ExternalLink("https://app.example.com/"));
        var quiet = new HeaderAction(T("Sign in"), new ExternalLink("https://app.example.com/in"));

        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetHeaderActions(cta, quiet))
            .ThenRaised(new SiteHeaderActionsChanged(cta, quiet));
    }

    [Fact]
    public void SetHeaderActions_updates_state_and_allows_clearing()
    {
        var cta = new HeaderAction(T("Go"), new ExternalLink("https://app.example.com/"));

        var outcome = AggregateSpec.For<Site>()
            .Given(Created, new SiteHeaderActionsChanged(cta, null))
            .When(s => s.SetHeaderActions(null, null));

        Assert.Null(outcome.Aggregate.HeaderCta);
        Assert.Null(outcome.Aggregate.HeaderQuiet);
    }

    [Fact]
    public void SetHeaderActions_with_unchanged_pair_raises_nothing()
    {
        HeaderAction Cta() => new(T("Go"), new ExternalLink("https://app.example.com/"));

        AggregateSpec.For<Site>()
            .Given(Created, new SiteHeaderActionsChanged(Cta(), null))
            .When(s => s.SetHeaderActions(Cta(), null))
            .ThenNothing();
    }

    // ------------------------------------------------------------------ copy line

    [Fact]
    public void SetCopyLine_raises_copy_line_changed()
    {
        var copy = new CopyLine(T("Copyright 2026"));

        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetCopyLine(copy))
            .ThenRaised(new SiteCopyLineChanged(copy));
    }

    [Fact]
    public void SetCopyLine_can_clear_an_existing_line() =>
        AggregateSpec.For<Site>()
            .Given(Created, new SiteCopyLineChanged(new CopyLine(T("old"))))
            .When(s => s.SetCopyLine(null))
            .ThenRaised(new SiteCopyLineChanged(null));

    [Fact]
    public void SetCopyLine_with_unchanged_value_raises_nothing() =>
        AggregateSpec.For<Site>()
            .Given(Created, new SiteCopyLineChanged(new CopyLine(T("same"))))
            .When(s => s.SetCopyLine(new CopyLine(T("same"))))
            .ThenNothing();

    [Fact]
    public void SetCopyLine_clearing_when_already_clear_raises_nothing() =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetCopyLine(null))
            .ThenNothing();
}
