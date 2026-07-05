using System.Text.Json.Nodes;
using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Features.Assets.ProcessAssetDarkVariant;
using Imprint.Authoring.Features.Assets.ProcessUploadedAsset;
using Imprint.Authoring.Features.Assets.UploadAsset;
using Imprint.Authoring.Features.Assets.UploadAssetDarkVariant;
using Imprint.Authoring.Features.Pages.AddNode;
using Imprint.Authoring.Features.Pages.CreatePage;
using Imprint.Authoring.Features.Pages.PublishPage;
using Imprint.Authoring.Features.Sites.ChangeNavigation;
using Imprint.Authoring.Features.Sites.ChangeThemeToken;
using Imprint.Authoring.Features.Sites.ChangeTypography;
using Imprint.Authoring.Features.Sites.SetCopyLine;
using Imprint.Authoring.Features.Sites.SetFooter;
using Imprint.Authoring.Features.Sites.SetHeaderActions;
using Imprint.EventSourcing;

namespace ContentSeeder;

/// <summary>
/// The migration engine: for each target site, authors every CMS surface into the Imprint
/// event store through <see cref="ICommandDispatcher"/> — CreatePage (or the pre-seeded
/// home page for home.json), AddNode for each mapped Section in order, ChangeNavigation
/// for the header, then PublishPage. Deterministic and idempotent given a fresh store.
/// </summary>
public sealed class Migrator(ICommandDispatcher dispatcher, string? apiBase = null)
{
    public sealed record SiteResult(
        string Key,
        int PagesAuthored,
        int DocsAuthored,
        int Published,
        IReadOnlyList<string> Slugs,
        IReadOnlyList<string> Flags);

    private readonly List<string> _flags = [];
    private Translations? _chrome;

    /// <summary>
    /// Supply a translation map so the site CHROME (nav labels + descriptions, footer headings +
    /// links, header action labels, copy line) is authored bilingually — English plus every match
    /// in the map, keyed by the English label. Page content is NOT translated here (that is the
    /// TranslationSeeder's EditText pass); this only covers the site-level furniture the publisher
    /// renders on every localized page. Pass null (default) for English-only chrome.
    /// </summary>
    public void UseChromeTranslations(Translations? translations) => _chrome = translations;

    /// <summary>A chrome label as bilingual LocalizedText: English, plus Danish when the map has it.</summary>
    private LocalizedText Chrome(string english) =>
        _chrome is { } t && t.TryGet(english, out var danish)
            ? Nodes.Text(english).With(Nodes.Da, danish)
            : Nodes.Text(english);

    public async Task<SiteResult> MigrateSite(SiteDef site)
    {
        var surfaces = CmsReader.Read(site.CmsDir);
        var assets = await IngestSvgFigures(site, surfaces);
        var mapper = new BlockMapper(site.Origin, apiBase, assets);
        var relToPageId = new Dictionary<string, PageId>(StringComparer.Ordinal);
        var slugs = new List<string>();
        var authoredPages = 0;
        var authoredDocs = 0;

        // ── 1. create every page (home is pre-seeded), then author its node subtrees ──
        foreach (var surface in surfaces)
        {
            var isHome = surface.Rel == "home";
            var slug = isHome ? "home" : SlugFor(surface.Rel, site.Key);
            PageId pageId;

            if (isHome)
            {
                pageId = site.HomePageId; // already exists (empty home page)
            }
            else
            {
                pageId = PageId.New();
                await Ok(new CreatePage(pageId, site.SiteId, surface.Title, slug, "en"),
                    $"CreatePage {site.Key}/{slug}");
            }

            relToPageId[surface.Rel] = pageId;
            if (!isHome)
            {
                slugs.Add(slug);
            }

            // Imprint has no nested pages: a nested CMS route (reports/tender) becomes a
            // single-segment slug (reports-tender), so the published URL SHAPE differs
            // (/reports/tender/ → /reports-tender/). The copy is identical. Flag it.
            if (!isHome && surface.Rel.Contains('/'))
            {
                _flags.Add($"[{site.Key}] nested CMS route '/{surface.Rel}/' has no nested-page equivalent in Imprint; " +
                           $"authored as flat slug '{slug}' (URL becomes '/{slug}/'). Copy is identical.");
            }

            var roots = surface.IsDoc
                ? DocRoots(surface.Doc!, site.Origin)
                : PageRoots(surface.Blocks, mapper, surface.Rel);

            var index = 0;
            foreach (var section in roots)
            {
                await Ok(new AddNode(pageId, NodeId.Root, index, section),
                    $"AddNode {site.Key}/{slug} #{index}");
                index++;
            }

            if (surface.IsDoc)
            {
                authoredDocs++;
            }
            else
            {
                authoredPages++;
            }
        }

        _flags.AddRange(mapper.Flags.Select(f => $"[{site.Key}/{f.Rel}] {f.Note}"));

        // A CMS href → an Imprint Link. A same-site relative path that names a migrated
        // page becomes a PageLink (so it tracks that page's slug/title); every other href
        // (cross-site absolute, or a same-site path with no migrated page) becomes an
        // ExternalLink to the real deployed destination. Nothing invented.
        Link? ToLink(string href)
        {
            var rel = href.TrimStart('/');
            if (!href.Contains("://") && relToPageId.TryGetValue(rel, out var target))
            {
                return new PageLink(target);
            }

            var resolved = Inline.ResolveHref(href, site.Origin);
            return resolved is null ? null : new ExternalLink(resolved);
        }

        NavigationChild? ToChild(NavLink link)
        {
            var target = ToLink(link.Href);
            if (target is null)
            {
                _flags.Add($"[{site.Key}] nav child '{link.Label}' → '{link.Href}' resolved to no destination (dropped).");
                return null;
            }

            // Keep the CMS label + optional description verbatim (the dropdown card copy
            // is authored, not the page title).
            var desc = string.IsNullOrWhiteSpace(link.Desc) ? null : Chrome(link.Desc!);
            return new NavigationChild(Chrome(link.Label), target, desc);
        }

        // ── 2. header navigation: home page first (so it renders at "/"), then the
        //       CMS header entries — direct links and dropdown groups, preserved. ──
        var navItems = new List<NavigationItem> { NavigationItem.Page(site.HomePageId, Chrome("Home")) };
        foreach (var entry in site.HeaderNav)
        {
            if (entry.IsGroup)
            {
                var children = entry.Children!.Select(ToChild).OfType<NavigationChild>().ToList();
                if (children.Count > 0)
                {
                    navItems.Add(NavigationItem.Group(Chrome(entry.Label), children));
                }

                continue;
            }

            var link = ToLink(entry.Href!);
            if (link is null)
            {
                _flags.Add($"[{site.Key}] header entry '{entry.Label}' → '{entry.Href}' resolved to no destination (dropped).");
                continue;
            }

            // A direct same-site page link may collide with the home page already added;
            // a page appears at most once as a top-level link, so skip the duplicate.
            if (link is PageLink page && navItems.Any(i => i.PageId == page.PageId))
            {
                continue;
            }

            // The CMS header carries its own label verbatim (it need not equal the page
            // title), so keep it for both link kinds.
            navItems.Add(new NavigationItem { Label = Chrome(entry.Label), Link = link });
        }

        await Ok(new ChangeNavigation(site.SiteId, navItems), $"ChangeNavigation {site.Key}");

        // ── footer columns, header actions, copy line — all first-class in Imprint now. ──
        FooterLink? ToFooterLink(NavLink link)
        {
            var target = ToLink(link.Href);
            if (target is null)
            {
                _flags.Add($"[{site.Key}] footer link '{link.Label}' → '{link.Href}' resolved to no destination (dropped).");
                return null;
            }

            // Footer labels are authored copy, kept verbatim for both link kinds.
            return new FooterLink(Chrome(link.Label), target);
        }

        var footerGroups = site.FooterGroups
            .Select(col => new FooterLinkGroup(
                Chrome(col.Heading),
                col.Links.Select(ToFooterLink).OfType<FooterLink>().ToList()))
            .Where(col => col.Links.Count > 0)
            .ToList();
        await Ok(new SetFooter(site.SiteId, footerGroups), $"SetFooter {site.Key}");

        HeaderAction? ToAction(HeaderAct? act)
        {
            if (act is null || ToLink(act.Href) is not { } target)
            {
                return null;
            }

            return new HeaderAction(Chrome(act.Label), target);
        }

        await Ok(new SetHeaderActions(site.SiteId, ToAction(site.HeaderCta), ToAction(site.HeaderQuiet)),
            $"SetHeaderActions {site.Key}");
        await Ok(new SetCopyLine(site.SiteId, new CopyLine(Chrome(site.CopyLine))), $"SetCopyLine {site.Key}");

        // ── design theme: this brand's neutral family + accent ramp, then its typography
        //    (watchdog/cai wear the shared canine look; assay wears "Dal" — warm paper,
        //    copper, editorial serif). Each token flows through the aggregate's own colour
        //    + range validation. ──
        foreach (var (token, light, dark) in Themes.TokensFor(site.Neutrals, site.Accent))
        {
            await Ok(new ChangeThemeToken(site.SiteId, token, light, dark), $"ChangeThemeToken {site.Key}/{token}");
        }

        await Ok(new ChangeTypography(site.SiteId, site.Typography), $"ChangeTypography {site.Key}");

        // ── 3. publish every page ──
        var published = 0;
        foreach (var (_, pageId) in relToPageId)
        {
            await Ok(new PublishPage(pageId), $"PublishPage {site.Key}/{pageId.Compact}");
            published++;
        }

        return new SiteResult(site.Key, authoredPages, authoredDocs, published, slugs, [.. _flags.Where(f => f.Contains($"[{site.Key}"))]);
    }

    /// <summary>
    /// Re-applies ONLY a site's brand layer — its neutral+accent theme tokens and its
    /// typography — to an already-seeded aggregate, creating no pages. This is the safe
    /// path to change the look of a LIVE store (e.g. rolling out Assay's Dal) without a
    /// destructive fresh reseed: the page/content/locale history (incl. editor-authored
    /// translations) is untouched, and every command is idempotent — an unchanged token
    /// or typography no-ops, so re-running is safe and only the changed values raise events.
    /// </summary>
    public async Task RebrandSite(SiteDef site)
    {
        foreach (var (token, light, dark) in Themes.TokensFor(site.Neutrals, site.Accent))
        {
            await Ok(new ChangeThemeToken(site.SiteId, token, light, dark), $"ChangeThemeToken {site.Key}/{token}");
        }

        await Ok(new ChangeTypography(site.SiteId, site.Typography), $"ChangeTypography {site.Key}");
    }

    public IReadOnlyList<string> AllFlags => _flags;

    /// <summary>
    /// Uploads every SVG file referenced by an <c>svgFigure</c> block through the real
    /// asset pipeline BEFORE the pages are authored: <c>UploadAsset</c> (base) +
    /// <c>UploadAssetDarkVariant</c>, then the processing commands the editor's
    /// background worker would dispatch (<c>ProcessUploadedAsset</c> /
    /// <c>ProcessAssetDarkVariant</c>) run inline so every asset is Ready — sanitized
    /// and inlinable — by publish time. Site-relative paths (<c>/figures/x.svg</c>)
    /// resolve against the CMS site's <c>public/</c> dir (exactly what Next served).
    /// Returns the src → AssetId map the block mapper embeds into SvgNodes.
    /// </summary>
    private async Task<IReadOnlyDictionary<string, AssetId>> IngestSvgFigures(
        SiteDef site, IReadOnlyList<CmsSurface> surfaces)
    {
        var map = new Dictionary<string, AssetId>(StringComparer.Ordinal);
        var publicRoot = Path.GetFullPath(Path.Combine(site.CmsDir, "..", "public"));

        foreach (var surface in surfaces)
        {
            foreach (var block in surface.Blocks)
            {
                if (block is null || block.Template() != "svgFigure")
                {
                    continue;
                }

                var src = block.Str("src");
                if (string.IsNullOrWhiteSpace(src) || map.ContainsKey(src!))
                {
                    continue;
                }

                var lightPath = Path.Combine(publicRoot, src!.TrimStart('/'));
                if (!File.Exists(lightPath))
                {
                    _flags.Add($"[{site.Key}/{surface.Rel}] svgFigure src '{src}' not found under {publicRoot} — block will be skipped.");
                    continue;
                }

                var assetId = AssetId.New();
                await using (var light = File.OpenRead(lightPath))
                {
                    await Ok(new UploadAsset(
                            assetId, Path.GetFileName(lightPath), "image/svg+xml", light.Length, light),
                        $"UploadAsset {site.Key}{src}");
                }

                await Ok(new ProcessUploadedAsset(assetId), $"ProcessUploadedAsset {site.Key}{src}");

                var darkSrc = block.Str("darkSrc");
                if (!string.IsNullOrWhiteSpace(darkSrc))
                {
                    var darkPath = Path.Combine(publicRoot, darkSrc!.TrimStart('/'));
                    if (!File.Exists(darkPath))
                    {
                        _flags.Add($"[{site.Key}/{surface.Rel}] svgFigure darkSrc '{darkSrc}' not found — figure ships light-only.");
                    }
                    else
                    {
                        await using (var dark = File.OpenRead(darkPath))
                        {
                            await Ok(new UploadAssetDarkVariant(
                                    assetId, Path.GetFileName(darkPath), "image/svg+xml", dark.Length, dark),
                                $"UploadAssetDarkVariant {site.Key}{darkSrc}");
                        }

                        await Ok(new ProcessAssetDarkVariant(assetId), $"ProcessAssetDarkVariant {site.Key}{darkSrc}");
                    }
                }

                map[src!] = assetId;
            }
        }

        return map;
    }

    private IReadOnlyList<Node> PageRoots(JsonArray blocks, BlockMapper mapper, string rel)
    {
        var roots = new List<Node>();
        foreach (var block in blocks)
        {
            if (block is null)
            {
                continue;
            }

            var section = mapper.Map(block, rel);
            if (section is not null)
            {
                roots.Add(section);
            }
        }

        return roots;
    }

    private static IReadOnlyList<Node> DocRoots(DocContent doc, string origin)
    {
        // A doc is one section: kicker (bold), H1 heading, doc-meta line, then the body
        // converted from markdown to canonical Imprint nodes.
        var stack = new List<Node>();
        if (!string.IsNullOrWhiteSpace(doc.Kicker))
        {
            stack.Add(Nodes.RichHtml($"<p><strong>{Inline.Escape(doc.Kicker!)}</strong></p>"));
        }

        stack.Add(Nodes.Heading(1, doc.Heading));
        if (!string.IsNullOrWhiteSpace(doc.DocMeta))
        {
            stack.Add(Nodes.Paragraph(doc.DocMeta, origin));
        }

        var bodyFlags = new List<Markdown.Flag>();
        stack.AddRange(Markdown.ToNodes(doc.MarkdownBody, origin, "(doc body)", bodyFlags));
        // The whole markdown page is one Doc-appearance section (canine's .mk-doc reading
        // column) — a measure-width, centered prose track.
        return [Nodes.Section(SectionBackground.None, SectionAppearance.Doc, Nodes.Stack([.. stack]))];
    }

    /// <summary>
    /// CMS rel → Imprint single-segment slug. Imprint has no nested pages, so a nested rel
    /// (e.g. <c>reports/tender</c>) is flattened with hyphens (<c>reports-tender</c>). The
    /// published URL therefore differs in shape from the CMS (<c>/reports/tender/</c> →
    /// <c>/reports-tender/</c>); the copy is identical. Flagged in the report.
    /// </summary>
    public static string SlugFor(string rel, string siteKey)
    {
        var flat = rel.Replace('/', '-').ToLowerInvariant();
        // Suggest normalizes to the Slug grammar (kebab, 1–80, no reserved/locale shadows).
        return Slug.TryCreate(flat, out var slug, out _) ? slug.Value : Slug.Suggest(flat);
    }

    private async Task Ok(ICommand command, string what)
    {
        var result = await dispatcher.Dispatch(command);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"{what} FAILED: {result.ErrorMessage}");
        }
    }
}
