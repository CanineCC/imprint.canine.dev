using System.Text.Json.Nodes;
using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Features.Pages.AddNode;
using Imprint.Authoring.Features.Pages.CreatePage;
using Imprint.Authoring.Features.Pages.PublishPage;
using Imprint.Authoring.Features.Sites.ChangeNavigation;
using Imprint.EventSourcing;

namespace ContentSeeder;

/// <summary>
/// The migration engine: for each target site, authors every CMS surface into the Imprint
/// event store through <see cref="ICommandDispatcher"/> — CreatePage (or the pre-seeded
/// home page for home.json), AddNode for each mapped Section in order, ChangeNavigation
/// for the header, then PublishPage. Deterministic and idempotent given a fresh store.
/// </summary>
public sealed class Migrator(ICommandDispatcher dispatcher)
{
    public sealed record SiteResult(
        string Key,
        int PagesAuthored,
        int DocsAuthored,
        int Published,
        IReadOnlyList<string> Slugs,
        IReadOnlyList<string> Flags);

    private readonly List<string> _flags = [];

    public async Task<SiteResult> MigrateSite(SiteDef site)
    {
        var surfaces = CmsReader.Read(site.CmsDir);
        var mapper = new BlockMapper(site.Origin);
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

        // ── 2. navigation: home first (so it renders at "/"), then header links ──
        var navItems = new List<NavigationItem>();
        var navSeen = new HashSet<PageId>();
        void AddNav(PageId id, string? label)
        {
            if (navSeen.Add(id))
            {
                navItems.Add(NavigationItem.Page(id, label is null ? null : Nodes.Text(label)));
            }
        }

        AddNav(site.HomePageId, "Home");
        foreach (var entry in site.HeaderNav)
        {
            // Header nav entries are same-site relative hrefs → map to the page whose rel
            // matches the href path. Off-site or unmatched entries are recorded, not invented.
            var rel = entry.Href.TrimStart('/');
            if (relToPageId.TryGetValue(rel, out var target))
            {
                AddNav(target, entry.Label);
            }
            else
            {
                _flags.Add($"[{site.Key}] header nav entry '{entry.Label}' → '{entry.Href}' has no matching page (skipped in Imprint navigation).");
            }
        }

        // Imprint navigation holds at most 20 items; the header lists more, so keep home +
        // the first entries up to the cap and flag the overflow (never silently drop copy).
        if (navItems.Count > Site.MaxNavigationItems)
        {
            var dropped = navItems.Skip(Site.MaxNavigationItems).Select(n => n.Label?.Resolve(Nodes.En, Nodes.En)).ToList();
            navItems = navItems.Take(Site.MaxNavigationItems).ToList();
            _flags.Add($"[{site.Key}] header navigation has {navItems.Count + dropped.Count} entries; Imprint caps navigation at {Site.MaxNavigationItems} — kept the first {Site.MaxNavigationItems}, overflow: {string.Join(", ", dropped)}.");
        }

        await Ok(new ChangeNavigation(site.SiteId, navItems), $"ChangeNavigation {site.Key}");

        // Imprint's static chrome (StaticPageChrome) models only: site name → home, a
        // flat header nav (set above), and a 404. It has NO footer-group model, no
        // cross-site external footer links, no theme toggle, no per-site copyLine, and
        // no dropdown-menu grouping. Those parts of sites/*/lib/site.ts are therefore
        // not authorable through the command surface and are not migrated (never invented).
        _flags.Add($"[{site.Key}] site.ts chrome NOT representable in Imprint's static chrome/command model: " +
                   "footer groups + cross-site footer links, header dropdown grouping/desc lines, header CTA buttons, " +
                   "the theme toggle, and the copyLine. Header nav was flattened to a same-site page list.");

        // ── 3. publish every page ──
        var published = 0;
        foreach (var (_, pageId) in relToPageId)
        {
            await Ok(new PublishPage(pageId), $"PublishPage {site.Key}/{pageId.Compact}");
            published++;
        }

        return new SiteResult(site.Key, authoredPages, authoredDocs, published, slugs, [.. _flags.Where(f => f.Contains($"[{site.Key}"))]);
    }

    public IReadOnlyList<string> AllFlags => _flags;

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
        return [Nodes.Section(Nodes.Stack([.. stack]))];
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
