using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Features.Pages;

namespace Imprint.Publishing.Tests.Pipeline;

/// <summary>
/// The marketing chrome the shell reproduces: grouped dropdown navigation, header CTA +
/// quiet link, footer link columns (same-site and cross-site), and the copy line — all
/// resolved per locale, with same-site page links tracking slug/title and external links
/// passed through verbatim.
/// </summary>
public sealed class MarketingChromeTests
{
    private static async Task<(PublishingTestHost Host, SiteId SiteId, PageId HomeId, PageId AboutId, PageId MethodId)>
        BuildChromeSite(PublishingTestHost host)
    {
        var siteId = await host.CreateSite("Watchdog", "en");
        var homeId = await host.CreatePage(siteId, "home", "Home");
        await host.AddSection(homeId, SectionPresets.Find("hero")!.Build(PublishingTestHost.En));
        var aboutId = await host.CreatePage(siteId, "about", "About");
        await host.AddSection(aboutId, SectionPresets.Find("text")!.Build(PublishingTestHost.En));
        var methodId = await host.CreatePage(siteId, "methodology", "Methodology");
        await host.AddSection(methodId, SectionPresets.Find("text")!.Build(PublishingTestHost.En));

        await host.Publish(homeId);
        await host.Publish(aboutId);
        await host.Publish(methodId);
        return (host, siteId, homeId, aboutId, methodId);
    }

    [Fact]
    public async Task Grouped_nav_header_actions_footer_and_copy_line_render_in_the_shell()
    {
        await using var host = new PublishingTestHost();
        var (_, siteId, homeId, aboutId, methodId) = await BuildChromeSite(host);

        var en = PublishingTestHost.En;
        await host.SetNavigation(siteId,
        [
            // Home is a direct page link first (so it becomes the site root).
            NavigationItem.Page(homeId),
            // A direct external link.
            NavigationItem.External(LocalizedText.Of(en, "Status"), "https://status.example.com/"),
            // A dropdown group with a same-site page child (with a description) and an
            // external child.
            NavigationItem.Group(LocalizedText.Of(en, "Why Watchdog"),
            [
                new NavigationChild(
                    LocalizedText.Of(en, "What we measure"),
                    new PageLink(methodId),
                    LocalizedText.Of(en, "The lenses behind the index")),
                new NavigationChild(
                    LocalizedText.Of(en, "The standard"),
                    new ExternalLink("https://cai.example.com/spec")),
            ]),
        ]);

        await host.SetHeaderActions(siteId,
            new HeaderAction(LocalizedText.Of(en, "Survey a repo"), new ExternalLink("https://app.example.com/")),
            new HeaderAction(LocalizedText.Of(en, "Sign in"), new ExternalLink("https://app.example.com/in")));

        await host.SetFooter(siteId,
        [
            new FooterLinkGroup(LocalizedText.Of(en, "Product"),
            [
                // A same-site page link (label falls back to page title) and one with an override.
                new FooterLink(null, new PageLink(aboutId)),
                new FooterLink(LocalizedText.Of(en, "What we measure"), new PageLink(methodId)),
            ]),
            new FooterLinkGroup(LocalizedText.Of(en, "The CAI standard"),
            [
                new FooterLink(LocalizedText.Of(en, "cai.example.com"), new ExternalLink("https://cai.example.com")),
            ]),
        ]);

        await host.SetCopyLine(siteId,
            new CopyLine(LocalizedText.Of(en, "Copyright 2025-2026 - The independent surveyor.")));

        await host.Publisher.Synchronize();
        var html = host.ReadText("index.html");

        // Direct external nav link, verbatim.
        Assert.Contains("<a href=\"https://status.example.com/\">Status</a>", html);

        // The dropdown group: a trigger button + a panel of cards (label + description).
        Assert.Contains("<li class=\"ip-nav-group\">", html);
        Assert.Contains("<button type=\"button\" class=\"ip-nav-trigger\" aria-haspopup=\"true\">Why Watchdog</button>", html);
        Assert.Contains("<div class=\"ip-nav-panel\">", html);
        Assert.Contains("<a class=\"ip-nav-card\" href=\"/methodology/\"", html);
        Assert.Contains("<strong>What we measure</strong>", html);
        Assert.Contains("<span>The lenses behind the index</span>", html);
        // The external child in the dropdown, verbatim, with no description span.
        Assert.Contains("<a class=\"ip-nav-card\" href=\"https://cai.example.com/spec\"", html);
        Assert.Contains("<strong>The standard</strong>", html);

        // Header actions: quiet link then CTA.
        Assert.Contains("<a class=\"ip-header-quiet\" href=\"https://app.example.com/in\">Sign in</a>", html);
        Assert.Contains("<a class=\"ip-header-cta\" href=\"https://app.example.com/\">Survey a repo</a>", html);

        // Footer columns: heading, a page link resolving to its title, an override label,
        // and a cross-site external link.
        Assert.Contains("<div class=\"ip-footer-cols\">", html);
        Assert.Contains("<nav class=\"ip-footer-group\" aria-label=\"Product\">", html);
        Assert.Contains("<span class=\"ip-footer-h\">Product</span>", html);
        Assert.Contains("<a href=\"/about/\">About</a>", html);
        Assert.Contains("<a href=\"/methodology/\">What we measure</a>", html);
        Assert.Contains("<a href=\"https://cai.example.com\">cai.example.com</a>", html);

        // Copy line + the fixed Canine byline.
        Assert.Contains("<p class=\"ip-footer-copy\">Copyright 2025-2026 - The independent surveyor.</p>", html);
        Assert.Contains("by <a href=\"https://canine.dev\">Canine Development</a>", html);
    }

    [Fact]
    public async Task Same_site_chrome_links_track_their_page_slug_and_title()
    {
        await using var host = new PublishingTestHost();
        var (_, siteId, homeId, _, methodId) = await BuildChromeSite(host);

        var en = PublishingTestHost.En;
        await host.SetNavigation(siteId, homeId, methodId);
        await host.SetFooter(siteId,
        [
            new FooterLinkGroup(LocalizedText.Of(en, "Product"),
                [new FooterLink(null, new PageLink(methodId))]),
        ]);
        await host.Publisher.Synchronize();

        // Baseline: the footer link resolves to the page title and current slug.
        var before = host.ReadText("index.html");
        Assert.Contains("<a href=\"/methodology/\">Methodology</a>", before);

        // Rename the page's slug and title; the chrome links must follow on republish.
        await host.ChangeSlug(methodId, "what-we-measure");
        await host.SetTitle(methodId, "en", "What we measure");
        await host.Publish(methodId);
        await host.Publisher.Synchronize();

        var after = host.ReadText("index.html");
        Assert.Contains("<a href=\"/what-we-measure/\">What we measure</a>", after);
        Assert.DoesNotContain("/methodology/", after);
    }

    [Fact]
    public async Task External_footer_links_survive_a_locale_variant()
    {
        await using var host = new PublishingTestHost();
        var siteId = await host.CreateSite("Watchdog", "en");
        await host.AddLocale(siteId, "da");
        var homeId = await host.CreatePage(siteId, "home", "Home");
        await host.AddSection(homeId, SectionPresets.Find("hero")!.Build(PublishingTestHost.En));
        await host.SetTitle(homeId, "da", "Hjem");
        await host.Publish(homeId);

        var en = PublishingTestHost.En;
        await host.SetNavigation(siteId, homeId);
        await host.SetFooter(siteId,
        [
            new FooterLinkGroup(LocalizedText.Of(en, "Trust"),
                [new FooterLink(LocalizedText.Of(en, "Terms"), new ExternalLink("https://example.com/tos"))]),
        ]);
        await host.SetCopyLine(siteId, new CopyLine(LocalizedText.Of(en, "Copyright Watchdog")));
        await host.Publisher.Synchronize();

        // The Danish variant keeps the external footer link verbatim and the copy line
        // (falling back to the default locale, since only 'en' was set).
        var da = host.ReadText("da/index.html");
        Assert.StartsWith("<!doctype html>\n<html lang=\"da\">", da);
        Assert.Contains("<a href=\"https://example.com/tos\">Terms</a>", da);
        Assert.Contains("<p class=\"ip-footer-copy\">Copyright Watchdog</p>", da);
    }
}
