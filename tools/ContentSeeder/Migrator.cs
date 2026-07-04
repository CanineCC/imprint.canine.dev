using System.Text.Json.Nodes;
using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Features.Pages.AddNode;
using Imprint.Authoring.Features.Pages.CreatePage;
using Imprint.Authoring.Features.Pages.PublishPage;
using Imprint.Authoring.Features.Sites.ChangeNavigation;
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
            var desc = string.IsNullOrWhiteSpace(link.Desc) ? null : Nodes.Text(link.Desc!);
            return new NavigationChild(Nodes.Text(link.Label), target, desc);
        }

        // ── 2. header navigation: home page first (so it renders at "/"), then the
        //       CMS header entries — direct links and dropdown groups, preserved. ──
        var navItems = new List<NavigationItem> { NavigationItem.Page(site.HomePageId, Nodes.Text("Home")) };
        foreach (var entry in site.HeaderNav)
        {
            if (entry.IsGroup)
            {
                var children = entry.Children!.Select(ToChild).OfType<NavigationChild>().ToList();
                if (children.Count > 0)
                {
                    navItems.Add(NavigationItem.Group(Nodes.Text(entry.Label), children));
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
            navItems.Add(new NavigationItem { Label = Nodes.Text(entry.Label), Link = link });
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
            return new FooterLink(Nodes.Text(link.Label), target);
        }

        var footerGroups = site.FooterGroups
            .Select(col => new FooterLinkGroup(
                Nodes.Text(col.Heading),
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

            return new HeaderAction(Nodes.Text(act.Label), target);
        }

        await Ok(new SetHeaderActions(site.SiteId, ToAction(site.HeaderCta), ToAction(site.HeaderQuiet)),
            $"SetHeaderActions {site.Key}");
        await Ok(new SetCopyLine(site.SiteId, new CopyLine(Nodes.Text(site.CopyLine))), $"SetCopyLine {site.Key}");

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
