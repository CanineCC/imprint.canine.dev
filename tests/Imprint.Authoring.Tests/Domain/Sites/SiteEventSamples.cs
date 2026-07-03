using Imprint.Authoring.Domain;
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

            // One item with a multi-locale label override and one relying on the page
            // title fallback, so the round trip exercises both shapes of the payload.
            yield return new SiteNavigationChanged([
                new NavigationItem(PageId.New(), LocalizedText.Of(en, "Home").With(daDk, "Hjem")),
                new NavigationItem(PageId.New(), null),
            ]);
        }
    }
}
