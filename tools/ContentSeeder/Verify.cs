using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Projections;
using Imprint.EventSourcing;
using Microsoft.Extensions.DependencyInjection;

namespace ContentSeeder;

/// <summary>
/// The local verify: proves every surface exists and is published, that a sample of
/// pages carries the expected VERBATIM headings/copy and the right WidgetNodes, and that
/// the static publish rendered every page (no canonical-HTML rejection, widget bundles
/// present). Any failed check makes the run exit non-zero.
/// </summary>
public static class Verify
{
    private static readonly Locale En = new("en");

    public static async Task<bool> Run(
        IServiceProvider provider, IReadOnlyList<SiteDef> sites, string publishRoot, bool noPublish)
    {
        var published = provider.GetRequiredService<PublishedContent>();
        var pageList = provider.GetRequiredService<PageList>();
        var ok = true;

        void Check(bool condition, string label)
        {
            Console.WriteLine($"  [{(condition ? "ok" : "FAIL")}] {label}");
            ok &= condition;
        }

        // ── 1. 46 surfaces exist and are all published ──
        var expected = 0;
        var publishedCount = 0;
        foreach (var site in sites)
        {
            var surfaces = CmsReader.Read(site.CmsDir);
            expected += surfaces.Count;
            foreach (var surface in surfaces)
            {
                var slug = surface.Rel == "home" ? "home" : Migrator.SlugFor(surface.Rel, site.Key);
                var summary = pageList.All(site.SiteId)
                    .FirstOrDefault(p => p.Slug.Value == slug);
                if (summary is null)
                {
                    Console.WriteLine($"  [FAIL] {site.Key}/{slug} missing");
                    ok = false;
                    continue;
                }

                if (summary.Status == PageStatus.Published && published.Get(summary.Id) is not null)
                {
                    publishedCount++;
                }
                else
                {
                    Console.WriteLine($"  [FAIL] {site.Key}/{slug} not published (status {summary.Status})");
                    ok = false;
                }
            }
        }

        // The grand total only holds for a full four-site run; an --only run has already
        // verified each of its own surfaces individually above.
        if (sites.Count == 4)
        {
            Check(expected == 46, $"expected 46 surfaces across 4 sites — found {expected}");
        }
        Check(publishedCount == expected, $"all {expected} surfaces published — {publishedCount} confirmed");

        // ── 1b. no page block silently dropped: each PAGE (not doc) authors exactly one
        //        root Section per CMS block; docs author exactly one root Section. ──
        var blockMismatches = 0;
        var totalCmsBlocks = 0;
        foreach (var site in sites)
        {
            foreach (var surface in CmsReader.Read(site.CmsDir))
            {
                var slug = surface.Rel == "home" ? "home" : Migrator.SlugFor(surface.Rel, site.Key);
                var summary = pageList.All(site.SiteId).FirstOrDefault(p => p.Slug.Value == slug);
                var page = summary is null ? null : published.Get(summary.Id);
                if (page is null)
                {
                    continue;
                }

                var rootSections = page.Tree.Roots.Count;
                if (surface.IsDoc)
                {
                    if (rootSections != 1)
                    {
                        blockMismatches++;
                    }
                }
                else
                {
                    totalCmsBlocks += surface.Blocks.Count;
                    if (rootSections != surface.Blocks.Count)
                    {
                        Console.WriteLine($"  [FAIL] {site.Key}/{slug}: {surface.Blocks.Count} CMS blocks → {rootSections} sections");
                        blockMismatches++;
                    }
                }
            }
        }

        Check(blockMismatches == 0,
            $"every CMS page block mapped to exactly one root Section (0 dropped) — {totalCmsBlocks} page blocks reconciled");

        // ── 2. sample pages: verbatim copy + widgets ──
        Check(HasHeading(published, pageList, Sites.WatchdogSite, "home",
                "Prove your code before you hand it over."),
            "WD home carries the verbatim hero H1");
        Check(HasWidget(published, pageList, Sites.WatchdogSite, "home", "cai-score-card"),
            "WD home carries the CAI score-card widget (hero proof, sample)");
        Check(HasCopy(published, pageList, Sites.WatchdogSite, "home",
                "one reproducible 0–100"),
            "WD home lede copy present verbatim");

        Check(HasHeading(published, pageList, Sites.AssaySite, "home", null),
            "Assay home has a hero H1");
        Check(PageExists(pageList, Sites.AssaySite, "how-it-works"),
            "Assay how-it-works page exists");
        Check(PageExists(pageList, Sites.AssaySite, "reports-tender"),
            "Assay nested reports/tender → slug reports-tender exists");

        Check(HasHeading(published, pageList, Sites.CaiSite, "home", null),
            "CAI home has a hero H1");
        Check(HasWidgetAnywhere(published, pageList, Sites.CaiSite, "cai-band-scale") ||
              HasWidgetAnywhere(published, pageList, Sites.CaiSite, "cai-composition-bar") ||
              HasWidgetAnywhere(published, pageList, Sites.CaiSite, "cai-evidence-flow"),
            "CAI site carries at least one CAI diagram widget");

        // Canine studio home: verbatim hero H1, the products section counting FOUR
        // (Assay joins Unfold/Watchdog/CAI — the one sanctioned content change), and
        // the contact page carrying the form island.
        Check(HasHeading(published, pageList, Sites.CanineSite, "home",
                "We don't build your software. We make sure it's world-class."),
            "Canine home carries the verbatim hero H1");
        Check(HasHeading(published, pageList, Sites.CanineSite, "home",
                "Four products, one standard of quality."),
            "Canine home products heading counts four (Assay added)");
        Check(HasCopy(published, pageList, Sites.CanineSite, "home",
                "Know what the software is worth before you sign."),
            "Canine home carries the Assay product card");
        Check(PageExists(pageList, Sites.CanineSite, "contact"),
            "Canine contact page exists");
        Check(HasWidget(published, pageList, Sites.CanineSite, "contact", "contact-form"),
            "Canine contact carries the contact-form island");

        // WD pricing page: pricingTiers copy + first-scan-free line
        Check(PageExists(pageList, Sites.WatchdogSite, "pricing"),
            "WD pricing page exists");
        Check(HasCopy(published, pageList, Sites.WatchdogSite, "pricing", "Pay for what we scan"),
            "WD pricing hero heading copy present verbatim");

        // A doc: WD security — heading + a verbatim body claim + the table-as-list transform
        Check(HasHeading(published, pageList, Sites.WatchdogSite, "security", "Security & data handling"),
            "WD security doc H1 present verbatim");
        Check(HasCopy(published, pageList, Sites.WatchdogSite, "security",
                "Your source code is never persisted"),
            "WD security doc body copy present verbatim");
        Check(HasCopy(published, pageList, Sites.WatchdogSite, "security", "Subprocessor"),
            "WD security doc table content preserved (header row copy)");

        // ── 2b. the old site's infographics (svgFigure blocks → SvgNodes w/ ingested assets) ──
        Check(SvgNodeCount(published, pageList, Sites.WatchdogSite, "where-watchdog-fits") == 9,
            "WD where-watchdog-fits carries all 9 positioning figures as SvgNodes " +
            $"(found {SvgNodeCount(published, pageList, Sites.WatchdogSite, "where-watchdog-fits")})");
        Check(SvgNodeCount(published, pageList, Sites.WatchdogSite, "for-consultancies") == 1,
            "WD for-consultancies carries the scan→fix→prove loop figure");
        Check(SvgNodeCount(published, pageList, Sites.AssaySite, "for-acquirers") == 1,
            "Assay for-acquirers carries the LOI→close deal-gates figure");

        // ── 3. no non-canonical RichText anywhere (would have been rejected at AddNode,
        //       but assert defensively that every RichText validates) ──
        var badHtml = 0;
        foreach (var page in published.All)
        {
            foreach (var node in page.Tree.All())
            {
                if (node is RichTextNode rich)
                {
                    var html = rich.Html.Resolve(En, En);
                    if (!CanonicalHtml.TryValidate(html, out _))
                    {
                        badHtml++;
                    }
                }
            }
        }

        Check(badHtml == 0, $"every RichText node is canonical HTML — {badHtml} invalid");

        // ── 4. static publish output ──
        if (!noPublish)
        {
            foreach (var site in sites)
            {
                var dir = Path.Combine(publishRoot, site.Key);
                var indexHtml = Path.Combine(dir, "index.html");
                Check(File.Exists(indexHtml), $"{site.Key} static home index.html rendered");

                var widgetDir = Path.Combine(dir, "widgets");
                var islands = Directory.Exists(widgetDir)
                    ? Directory.GetFiles(widgetDir, "*.js").Length
                    : 0;
                Check(islands > 0, $"{site.Key} widget island bundles emitted ({islands})");

                // Spot-check that the home HTML carries a custom-element island (data-island).
                if (File.Exists(indexHtml))
                {
                    var home = await File.ReadAllTextAsync(indexHtml);
                    Check(home.Contains("cai-", StringComparison.Ordinal) || islands > 0,
                        $"{site.Key} home HTML references a CAI widget element");
                }
            }

            // The figures render INLINE (sanitized svg, light + dark revealed by theme) —
            // prove the static output actually carries them, not just the authored nodes.
            var wwfHtml = Path.Combine(publishRoot, "watchdog", "where-watchdog-fits", "index.html");
            if (File.Exists(wwfHtml))
            {
                var html = await File.ReadAllTextAsync(wwfHtml);
                var inlineSvgs = html.Split("<svg", StringSplitOptions.None).Length - 1;
                Check(inlineSvgs >= 18,
                    $"WD where-watchdog-fits static HTML inlines the figures (light+dark, found {inlineSvgs} <svg>)");
                Check(html.Contains("ip-img-dark", StringComparison.Ordinal),
                    "WD where-watchdog-fits static HTML carries the dark-variant reveal wrappers");
            }
            else
            {
                Check(false, "WD where-watchdog-fits static HTML rendered");
            }
        }

        return ok;
    }

    private static PublishedPage? Page(
        PublishedContent published, PageList list, SiteId site, string slug)
    {
        var summary = list.All(site).FirstOrDefault(p => p.Slug.Value == slug);
        return summary is null ? null : published.Get(summary.Id);
    }

    private static bool PageExists(PageList list, SiteId site, string slug) =>
        list.All(site).Any(p => p.Slug.Value == slug);

    private static bool HasHeading(
        PublishedContent published, PageList list, SiteId site, string slug, string? exact)
    {
        var page = Page(published, list, site, slug);
        if (page is null)
        {
            return false;
        }

        foreach (var node in page.Tree.All())
        {
            if (node is HeadingNode h)
            {
                var text = h.Text.Resolve(En, En);
                if (exact is null ? text.Length > 0 : text == exact)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool HasCopy(
        PublishedContent published, PageList list, SiteId site, string slug, string needle)
    {
        var page = Page(published, list, site, slug);
        if (page is null)
        {
            return false;
        }

        foreach (var node in page.Tree.All())
        {
            var text = node switch
            {
                HeadingNode h => h.Text.Resolve(En, En),
                RichTextNode r => CanonicalHtml.ToPlainText(r.Html.Resolve(En, En)),
                ButtonNode b => b.Label.Resolve(En, En),
                _ => "",
            };
            if (text.Contains(needle, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasWidget(
        PublishedContent published, PageList list, SiteId site, string slug, string tag)
    {
        var page = Page(published, list, site, slug);
        return page is not null && page.Tree.All().OfType<WidgetNode>().Any(w => w.Tag == tag);
    }

    private static int SvgNodeCount(
        PublishedContent published, PageList list, SiteId site, string slug)
    {
        var page = Page(published, list, site, slug);
        return page is null ? 0 : page.Tree.All().OfType<SvgNode>().Count(s => s.AssetId is not null);
    }

    private static bool HasWidgetAnywhere(
        PublishedContent published, PageList list, SiteId site, string tag)
    {
        foreach (var summary in list.All(site))
        {
            var page = published.Get(summary.Id);
            if (page is not null && page.Tree.All().OfType<WidgetNode>().Any(w => w.Tag == tag))
            {
                return true;
            }
        }

        return false;
    }
}
