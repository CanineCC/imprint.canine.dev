using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Features.Pages.AddPreset;

namespace Imprint.Authoring.Features.Sites.CreateSiteFromTemplate;

/// <summary>
/// Starter templates: the seed-stream pattern. A template is nothing special — just a
/// recipe of ordinary commands (create site, create pages, add sections, set nav), so
/// a templated site's history reads exactly like a hand-built one.
/// </summary>
public sealed record SiteTemplate(
    string Key,
    string Name,
    string Description,
    IReadOnlyList<TemplatePage> Pages);

/// <summary>Slug/title plus a factory building fresh-id sections in the site's default locale.</summary>
public sealed record TemplatePage(
    string Slug,
    string Title,
    bool InNavigation,
    Func<Locale, IReadOnlyList<SectionNode>> BuildSections);

public static class SiteTemplates
{
    public static readonly IReadOnlyList<SiteTemplate> All =
    [
        new SiteTemplate(
            "blank", "Blank", "An empty site — one page, no content.",
            [new TemplatePage("home", "Home", InNavigation: true, _ => [])]),

        new SiteTemplate(
            "launch", "Launch", "A three-page starter: landing page, about, contact.",
            [
                new TemplatePage("home", "Home", InNavigation: true, locale =>
                [
                    Preset("hero", locale),
                    Preset("feature-grid", locale),
                    Preset("split", locale),
                    Preset("cta", locale),
                    Preset("footer", locale),
                ]),
                new TemplatePage("about", "About", InNavigation: true, locale =>
                [
                    Preset("text", locale),
                    Preset("split", locale),
                    Preset("footer", locale),
                ]),
                new TemplatePage("contact", "Contact", InNavigation: true, locale =>
                [
                    Preset("text", locale),
                    Preset("cta", locale),
                    Preset("footer", locale),
                ]),
            ]),
    ];

    public static SiteTemplate? Find(string key) => All.FirstOrDefault(template => template.Key == key);

    private static SectionNode Preset(string key, Locale locale) =>
        SectionPresets.Find(key)!.Build(locale);
}
