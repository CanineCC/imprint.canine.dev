using Imprint.Authoring.Domain;
using Imprint.Authoring.Features.Pages.ChangePageMeta;
using Imprint.Authoring.Features.Pages.ChangePageTitle;
using Imprint.Authoring.Features.Pages.EditText;
using Imprint.Authoring.Features.Pages.PublishPage;
using Imprint.Authoring.Features.Sites.AddLocale;
using Imprint.Authoring.Projections;
using Imprint.EventSourcing;

namespace ContentSeeder;

/// <summary>
/// Authors a non-default locale (e.g. "da") over an already-English-authored site, WITHOUT
/// touching the English content. Content is "born multilingual": a translation is just another
/// per-locale entry on each field, so this registers the locale (once) then, for every
/// translatable field the <see cref="TranslationCoverage"/> worklist reports, emits the matching
/// per-field command with the target value — <c>EditText</c> for node fields (heading/rich-text/
/// button), <c>ChangePageTitle</c>/<c>ChangePageMeta</c> for the page-level title + SEO. The
/// publisher's per-locale render loop then emits a real <c>/da/</c> subtree instead of the
/// English fallback. Fields with no target are collected as the fill-in worklist.
/// </summary>
public sealed class TranslationSeeder(
    ICommandDispatcher dispatcher,
    TranslationCoverage coverage,
    PageList pages,
    SiteOverview sites,
    ProjectionEngine projections)
{
    public async Task<SeedResult> Seed(SiteDef site, string locale, Translations translations)
    {
        var loc = new Locale(locale);

        // Register the locale if the site doesn't already carry it (the editor may have added it
        // earlier). AddLocale throws if it's already present, so guard on the read model, and
        // catch up projections so the EditText locale-membership check sees it.
        if (sites.Get(site.SiteId) is { } current && !current.Locales.Contains(loc))
        {
            await Ok(new AddLocale(site.SiteId, locale), $"AddLocale {site.Key}/{locale}");
            await projections.CatchUp();
        }

        var translated = 0;
        var total = 0;
        var rejected = new List<string>();
        foreach (var page in pages.All(site.SiteId))
        {
            string? title = null, metaTitle = null, metaDescription = null;

            foreach (var field in coverage.FieldsOf(page.Id))
            {
                total++;
                if (!translations.TryGet(field.SourceText, out var value))
                {
                    continue;
                }

                if (field.IsPageField)
                {
                    switch (field.Field)
                    {
                        case "text": title = value; break;
                        case "meta-title": metaTitle = value; break;
                        case "meta-description": metaDescription = value; break;
                    }

                    translated++;
                }
                // A rich-text 'html' value must be canonical HTML or the aggregate rejects it.
                // Collect a rejection (bad translation) rather than abort the whole locale run —
                // the field keeps its English fallback and the string surfaces for a fix.
                else if (await TryDispatch(
                             new EditText(page.Id, field.NodeId, field.Field, locale, value)) is { } error)
                {
                    rejected.Add($"[{site.Key}/{page.Slug.Value}/{field.Label}] {error}: {Trim(field.SourceText)}");
                }
                else
                {
                    translated++;
                }
            }

            if (title is not null)
            {
                await Ok(new ChangePageTitle(page.Id, locale, title), $"Title {site.Key}/{page.Slug.Value}");
            }

            if (metaTitle is not null || metaDescription is not null)
            {
                await Ok(new ChangePageMeta(page.Id, locale, metaTitle, metaDescription), $"Meta {site.Key}/{page.Slug.Value}");
            }

            // Re-publish: MigrateSite already snapshotted the English-only draft, but the publisher
            // renders the PUBLISHED version — so without a fresh PublishPage the new locale's fields
            // stay in the draft and /da/ falls back to English. Re-snapshot the now-translated draft.
            await Ok(new PublishPage(page.Id), $"PublishPage {site.Key}/{page.Slug.Value}");
        }

        await projections.CatchUp();
        return new SeedResult(translated, total, [.. translations.Missing.Distinct()], rejected);
    }

    /// <summary>Dispatch and return null on success, or the error message on domain failure.</summary>
    private async Task<string?> TryDispatch(ICommand command)
    {
        var result = await dispatcher.Dispatch(command);
        return result.Succeeded ? null : result.ErrorMessage;
    }

    private async Task Ok(ICommand command, string what)
    {
        if (await TryDispatch(command) is { } error)
        {
            throw new InvalidOperationException($"{what} FAILED: {error}");
        }
    }

    private static string Trim(string s) => s.Length <= 80 ? s : s[..80] + "…";

    public sealed record SeedResult(
        int Translated, int Total, IReadOnlyList<string> Missing, IReadOnlyList<string> Rejected);
}
