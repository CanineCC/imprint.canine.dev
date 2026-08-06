using System.Text.Json;
using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Sites;
using Imprint.Editor.Api;

namespace Imprint.Editor.Tests;

/// <summary>
/// Navigation and footer travel as a whole value, but a caller can only speak for one
/// locale at a time. Without a merge, translating the English header DELETES the Danish
/// one — silently, because the labels then resolve through the default locale and the
/// header still looks right in English.
/// </summary>
public sealed class AuthoringLocaleMergeTests
{
    private static readonly Locale En = new("en");
    private static readonly Locale Da = new("da");

    private static LocalizedText Both(string en, string da) =>
        LocalizedText.Of(En, en).With(Da, da);

    // ------------------------------------------------------------------- navigation

    [Fact]
    public void Retranslating_one_locale_keeps_the_other_locales_labels()
    {
        var existing = new[] { NavigationItem.External(Both("Independence", "Uvildighed"), "#independence") };
        var incoming = new List<NavigationItem> { NavigationItem.External(LocalizedText.Of(En, "Impartiality"), "#independence") };

        var merged = AuthoringApi.CarryOtherLocales(incoming, existing, En);

        Assert.Equal("Impartiality", merged[0].Label!.Get(En));
        Assert.Equal("Uvildighed", merged[0].Label!.Get(Da));
    }

    [Fact]
    public void A_reordered_item_keeps_its_translations()
    {
        var existing = new[]
        {
            NavigationItem.External(Both("Home", "Forside"), "#home"),
            NavigationItem.External(Both("The team", "Holdet"), "#team"),
        };
        var incoming = new List<NavigationItem>
        {
            NavigationItem.External(LocalizedText.Of(En, "The team"), "#team"),
            NavigationItem.External(LocalizedText.Of(En, "Home"), "#home"),
        };

        var merged = AuthoringApi.CarryOtherLocales(incoming, existing, En);

        Assert.Equal("Holdet", merged[0].Label!.Get(Da));
        Assert.Equal("Forside", merged[1].Label!.Get(Da));
    }

    [Fact]
    public void A_new_item_carries_only_the_locale_it_arrived_in()
    {
        var incoming = new List<NavigationItem> { NavigationItem.External(LocalizedText.Of(En, "Pricing"), "#pricing") };

        var merged = AuthoringApi.CarryOtherLocales(incoming, [], En);

        Assert.Equal("Pricing", merged[0].Label!.Get(En));
        Assert.Null(merged[0].Label!.Get(Da));
    }

    [Fact]
    public void A_dropdown_heading_keeps_its_other_languages()
    {
        // A group is the one entry with no link to match on, so it matches by position — the
        // same rule a footer column's heading already uses.
        var child = new NavigationChild(Both("Read me first", "Læs mig først"), new PageLink(PageId.New()));
        var existing = new[] { NavigationItem.Group(Both("Onboarding", "Introduktion"), [child]) };
        var incoming = new List<NavigationItem> { NavigationItem.Group(LocalizedText.Of(En, "Onboarding"), [child]) };

        var merged = AuthoringApi.CarryOtherLocales(incoming, existing, En);

        Assert.Equal("Introduktion", merged[0].Label!.Get(Da));
    }

    [Fact]
    public void A_dropdown_does_not_inherit_the_heading_of_a_plain_link_in_its_slot()
    {
        // Position only identifies a group against another group. Borrowing a page entry's
        // label would put a page's name on a menu that no longer points at it.
        var existing = new[] { NavigationItem.External(Both("Status", "Status"), "https://status.example.com/") };
        var incoming = new List<NavigationItem>
        {
            NavigationItem.Group(LocalizedText.Of(En, "Onboarding"), [new NavigationChild(null, new PageLink(PageId.New()))]),
        };

        var merged = AuthoringApi.CarryOtherLocales(incoming, existing, En);

        Assert.Null(merged[0].Label!.Get(Da));
    }

    [Fact]
    public void A_navigation_item_can_name_a_section_of_the_page_it_points_at()
    {
        var pageId = PageId.New();
        using var json = JsonDocument.Parse(
            $$"""{"label":"Independence","pageId":"{{pageId.Compact}}","fragment":"Independence"}""");

        var item = AuthoringApi.ParseNavigationItem(json.RootElement, En);

        Assert.Equal(new PageLink(pageId, "independence"), item.Link);
    }

    [Fact]
    public void A_section_of_a_page_is_one_entry_in_every_language()
    {
        // The reason the section link had to become a PAGE link. Written as an absolute URL it
        // needs the locale in the address — /#independence and /da/#independence — which makes
        // the two languages two different entries, and translating one then deletes the other's
        // label. As a page link the destination is identical in both, so the merge finds it.
        var link = new PageLink(PageId.New(), "independence");
        var existing = new[] { new NavigationItem { Label = Both("Independence", "Uvildighed"), Link = link } };
        var incoming = new List<NavigationItem> { new() { Label = LocalizedText.Of(En, "Independence"), Link = link } };

        var merged = AuthoringApi.CarryOtherLocales(incoming, existing, En);

        Assert.Equal("Uvildighed", merged[0].Label!.Get(Da));
    }

    [Fact]
    public void Repointing_a_link_drops_the_old_translations()
    {
        // A different destination is a different entry; a label translated for the old one
        // would be wrong, and keeping it is worse than asking for a fresh translation.
        var existing = new[] { NavigationItem.External(Both("Docs", "Dokumentation"), "#docs") };
        var incoming = new List<NavigationItem> { NavigationItem.External(LocalizedText.Of(En, "Docs"), "#guides") };

        var merged = AuthoringApi.CarryOtherLocales(incoming, existing, En);

        Assert.Null(merged[0].Label!.Get(Da));
    }

    [Fact]
    public void Group_children_keep_their_labels_and_descriptions()
    {
        var child = new NavigationChild(Both("Teams", "Hold"), new ExternalLink("#teams"), Both("For builders", "Til dem der bygger"));
        var existing = new[] { NavigationItem.Group(Both("Who it is for", "Hvem det er til"), [child]) };
        var incoming = new List<NavigationItem>
        {
            NavigationItem.Group(
                LocalizedText.Of(En, "Who it is for"),
                [new NavigationChild(LocalizedText.Of(En, "Teams"), new ExternalLink("#teams"), LocalizedText.Of(En, "For builders"))]),
        };

        var merged = AuthoringApi.CarryOtherLocales(incoming, existing, En);

        Assert.Equal("Hold", merged[0].Children[0].Label!.Get(Da));
        Assert.Equal("Til dem der bygger", merged[0].Children[0].Description!.Get(Da));
    }

    // ----------------------------------------------------------------------- footer

    [Fact]
    public void Footer_headings_and_links_keep_their_translations()
    {
        var existing = new[]
        {
            new FooterLinkGroup(Both("Products", "Produkter"),
                [new FooterLink(Both("Watchdog", "Watchdog"), new ExternalLink("https://watchdog.canine.dev"))]),
        };
        var incoming = new List<FooterLinkGroup>
        {
            new(LocalizedText.Of(En, "Products"),
                [new FooterLink(LocalizedText.Of(En, "Watchdog"), new ExternalLink("https://watchdog.canine.dev"))]),
        };

        var merged = AuthoringApi.CarryOtherLocales(incoming, existing, En);

        Assert.Equal("Produkter", merged[0].Heading.Get(Da));
        Assert.Equal("Watchdog", merged[0].Links[0].Label!.Get(Da));
    }

    [Fact]
    public void A_footer_link_moved_to_another_column_keeps_its_translations()
    {
        var existing = new[]
        {
            new FooterLinkGroup(Both("Products", "Produkter"), []),
            new FooterLinkGroup(Both("The studio", "Studiet"),
                [new FooterLink(Both("Independence", "Uvildighed"), new ExternalLink("#independence"))]),
        };
        var incoming = new List<FooterLinkGroup>
        {
            new(LocalizedText.Of(En, "Products"),
                [new FooterLink(LocalizedText.Of(En, "Independence"), new ExternalLink("#independence"))]),
        };

        var merged = AuthoringApi.CarryOtherLocales(incoming, existing, En);

        Assert.Equal("Uvildighed", merged[0].Links[0].Label!.Get(Da));
    }
}
