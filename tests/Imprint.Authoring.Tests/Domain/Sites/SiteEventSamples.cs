using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Domain.Sites.Events;

namespace Imprint.Authoring.Tests.Domain.Sites;

public sealed class SiteEventSamples : IEventSampleProvider
{
    public IEnumerable<object> Samples
    {
        get
        {
            var en = new Locale("en");
            var daDk = new Locale("da-DK");

            yield return new SiteCreated(SiteId.New(), "Marketing site", en);
            yield return new SiteRenamed("Rebranded site");
            yield return new SiteLocaleAdded(daDk);
            yield return new SiteLocaleRemoved(daDk);
            yield return new SiteDefaultLocaleChanged(daDk);
            yield return new SiteThemeTokenChanged("primary", "#3b5bdb", "oklch(70% 0.15 265)");
            yield return new SiteTypographyChanged(new Typography(
                Heading: FontStack.Serif,
                Body: FontStack.Humanist,
                BaseSizePx: 18,
                ScaleRatio: 1.333,
                RadiusPx: 12,
                Spacing: SpacingScale.Spacious));

            // A page link with a multi-locale label override, a page link relying on the
            // title fallback, an external direct link, and a group with a page child +
            // an external child carrying a description — so the round trip exercises every
            // shape of the hierarchical payload (both Link kinds, groups, descriptions).
            yield return new SiteNavigationChanged([
                NavigationItem.Page(PageId.New(), LocalizedText.Of(en, "Home").With(daDk, "Hjem")),
                NavigationItem.Page(PageId.New()),
                NavigationItem.External(LocalizedText.Of(en, "Sign in"), "https://app.example.com/"),
                NavigationItem.Group(LocalizedText.Of(en, "Why us"),
                [
                    new NavigationChild(LocalizedText.Of(en, "Methodology"), new PageLink(PageId.New())),
                    new NavigationChild(
                        LocalizedText.Of(en, "The standard"),
                        new ExternalLink("https://cai.example.com/spec"),
                        LocalizedText.Of(en, "One reproducible index")),
                ]),
            ]);

            // Footer columns mixing same-site page links and cross-site external links,
            // plus the header actions and the copy line — every new chrome event.
            yield return new SiteFooterChanged([
                new FooterLinkGroup(LocalizedText.Of(en, "Product"),
                [
                    new FooterLink(LocalizedText.Of(en, "Methodology"), new PageLink(PageId.New())),
                    new FooterLink(null, new PageLink(PageId.New())),
                ]),
                new FooterLinkGroup(LocalizedText.Of(en, "The standard"),
                [
                    new FooterLink(LocalizedText.Of(en, "cai.example.com"), new ExternalLink("https://cai.example.com")),
                ]),
            ]);

            yield return new SiteHeaderActionsChanged(
                new HeaderAction(LocalizedText.Of(en, "Survey a repo"), new ExternalLink("https://app.example.com/")),
                new HeaderAction(LocalizedText.Of(en, "Sign in"), new ExternalLink("https://app.example.com/in")));

            yield return new SiteCopyLineChanged(
                new CopyLine(LocalizedText.Of(en, "© 2025–2026 · The independent surveyor.")));

            yield return new SiteOwnershipClaimed();
            yield return new SiteCollaboratorAdded("colleague@example.com");
            yield return new SiteCollaboratorRemoved("colleague@example.com");

            // A promotion pipeline: several ordered deploy targets, exercising the list
            // payload's sequence value-equality on round trip.
            yield return new SiteEnvironmentsChanged([
                new DeployEnvironment("Test", "/srv/www/acme/test"),
                new DeployEnvironment("Staging", "staging"),
                new DeployEnvironment("Production", "/srv/www/acme"),
            ]);
        }
    }
}
