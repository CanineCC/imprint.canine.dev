using System.ComponentModel;
using System.Text.Json;
using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Assets;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Projections;
using Imprint.Editor.Api;
using Imprint.Editor.Auth;
using Imprint.EventSourcing;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Server;
using AddNodeCmd = Imprint.Authoring.Features.Pages.AddNode.AddNode;
using ChangeNavigationCmd = Imprint.Authoring.Features.Sites.ChangeNavigation.ChangeNavigation;
using ChangeNodePropsCmd = Imprint.Authoring.Features.Pages.ChangeNodeProps.ChangeNodeProps;
using ChangePageMetaCmd = Imprint.Authoring.Features.Pages.ChangePageMeta.ChangePageMeta;
using ChangePageTitleCmd = Imprint.Authoring.Features.Pages.ChangePageTitle.ChangePageTitle;
using CreatePageCmd = Imprint.Authoring.Features.Pages.CreatePage.CreatePage;
using CreateSiteCmd = Imprint.Authoring.Features.Sites.CreateSite.CreateSite;
using DuplicateNodeCmd = Imprint.Authoring.Features.Pages.DuplicateNode.DuplicateNode;
using EditTextCmd = Imprint.Authoring.Features.Pages.EditText.EditText;
using MoveNodeCmd = Imprint.Authoring.Features.Pages.MoveNode.MoveNode;
using PublishAllStaleCmd = Imprint.Authoring.Features.Pages.PublishAllStale.PublishAllStale;
using PublishPageCmd = Imprint.Authoring.Features.Pages.PublishPage.PublishPage;
using RemoveNodeCmd = Imprint.Authoring.Features.Pages.RemoveNode.RemoveNode;
using SetCopyLineCmd = Imprint.Authoring.Features.Sites.SetCopyLine.SetCopyLine;
using SetFaviconCmd = Imprint.Authoring.Features.Sites.SetFavicon.SetFavicon;
using SetHeaderLogoCmd = Imprint.Authoring.Features.Sites.SetHeaderLogo.SetHeaderLogo;
using UploadAssetCmd = Imprint.Authoring.Features.Assets.UploadAsset.UploadAsset;

namespace Imprint.Editor.Mcp;

/// <summary>
/// The headless authoring MCP: every capability of the Blazor editor exposed as an MCP tool so an
/// AI agent can drive the CMS — list/create sites and pages, edit the node tree, upload assets, set
/// the favicon and header logo, and publish. A thin forward over the SAME <see cref="ICommandDispatcher"/>
/// the editor and the authoring API use, so every guard, validator and the automatic
/// publish-on-catch-up apply unchanged. Every write is stamped with the service actor
/// (<c>Imprint:Authoring:Actor</c>) via <see cref="EditorActor.BeginScope"/>.
/// </summary>
/// <remarks>
/// SECURITY: mounted ONLY at <c>/mcp</c> behind a wholesale bearer-token branch that enforces the same
/// <c>Imprint:Authoring:Token</c> as the authoring API (even listing tools needs the token), and mapped
/// only when that token is configured — fail-closed, exactly like the authoring API. Serve over TLS.
/// </remarks>
[McpServerToolType]
public sealed class ImprintAuthoringMcpTools
{
    // ── reads ────────────────────────────────────────────────────────────────────────────────────

    [McpServerTool(Name = "list_sites")]
    [Description("List every site: its id (compact GUID), name and default locale. Start here to find the site id the other tools take.")]
    public static IReadOnlyList<SiteInfo> ListSites(SiteOverview sites) =>
        [.. sites.All.Select(s => new SiteInfo(s.Id.Compact, s.Name, s.DefaultLocale.Value))];

    [McpServerTool(Name = "list_pages")]
    [Description("List a site's pages: id, slug, title, publish status, whether it is the home page / in navigation, and its draft vs published version.")]
    public static object ListPages(
        [Description("The site id (compact or dashed GUID).")] string siteId,
        SiteOverview sites, PageList pages)
    {
        if (!TrySiteId(siteId, out var sid)) return Fail("invalid siteId");
        var site = sites.Get(sid);
        if (site is null) return Fail("unknown site");
        var loc = site.DefaultLocale;
        return new
        {
            ok = true,
            pages = pages.All(sid).Select(p => new
            {
                id = p.Id.Compact,
                slug = p.Slug.Value,
                title = p.Title.Resolve(loc, loc),
                status = p.Status.ToString(),
                isHome = p.IsHome,
                inNavigation = p.IsInNavigation,
                version = p.Version,
                publishedVersion = p.PublishedVersion,
            }).ToList(),
        };
    }

    [McpServerTool(Name = "get_page_tree")]
    [Description("The page's node tree, flattened depth-first: each node's id, type, parent, widget tag, whether it is a section, child count, depth — and its content/props (text fields as locale → value). Read this before editing: it is how you see the copy that is on the page now, and find the node id to change. Also returns the page's slug, title and SEO meta.")]
    public static object GetPageTree(
        [Description("The page id (compact or dashed GUID).")] string pageId,
        [Description("Include each node's props/text (default true). Pass false for a structure-only outline.")] bool? content,
        PageDrafts drafts)
    {
        if (!TryPageId(pageId, out var pid)) return Fail("invalid pageId");
        var page = drafts.Get(pid);
        if (page is null) return Fail("unknown page");

        var withContent = content ?? true;
        var flat = new List<object>();
        void Walk(NodeList nodes, NodeId parent, int depth)
        {
            foreach (var n in nodes)
            {
                flat.Add(new
                {
                    id = n.Id.Compact,
                    type = n.GetType().Name,
                    tag = n is WidgetNode w ? w.Tag : null,
                    isSection = n is SectionNode,
                    childCount = n is IContainerNode c ? c.Children.Count : 0,
                    depth,
                    parentId = parent.IsRoot ? null : parent.Compact,
                    props = withContent ? AuthoringNodeJson.Describe(n) : null,
                });
                if (n is IContainerNode container) Walk(container.Children, n.Id, depth + 1);
            }
        }

        Walk(page.Tree.Roots, NodeId.Root, 0);
        return new
        {
            ok = true,
            pageId = pid.Compact,
            slug = page.Slug.Value,
            title = Localized(page.Title),
            metaTitle = Localized(page.MetaTitle),
            metaDescription = Localized(page.MetaDescription),
            rootCount = page.Tree.Roots.Count,
            nodes = flat,
        };
    }

    [McpServerTool(Name = "get_site")]
    [Description("One site's chrome: locales, navigation (with groups and children), footer link groups and the fine-print copy line. Read this before set_navigation or set_copy_line — both carry the whole value, so you edit what you read back.")]
    public static object GetSite(
        [Description("The site id.")] string siteId,
        SiteOverview sites, PageList pages)
    {
        if (!TrySiteId(siteId, out var sid)) return Fail("invalid siteId");
        var site = sites.Get(sid);
        if (site is null) return Fail("unknown site");
        var slugs = pages.All(sid).ToDictionary(p => p.Id, p => p.Slug.Value);
        return new
        {
            ok = true,
            id = sid.Compact,
            name = site.Name,
            defaultLocale = site.DefaultLocale.Value,
            locales = site.Locales.Select(l => l.Value).ToList(),
            copyLine = site.CopyLine is null ? null : Localized(site.CopyLine.Text),
            navigation = site.Navigation.Select(item => (object)new
            {
                label = item.Label is null ? null : Localized(item.Label),
                link = LinkView(item.Link, slugs),
                children = item.Children.Select(child => (object)new
                {
                    label = child.Label is null ? null : Localized(child.Label),
                    description = child.Description is null ? null : Localized(child.Description),
                    link = LinkView(child.Link, slugs),
                }).ToList(),
            }).ToList(),
            footer = site.FooterGroups.Select(group => (object)new
            {
                heading = Localized(group.Heading),
                links = group.Links.Select(link => (object)new
                {
                    label = link.Label is null ? null : Localized(link.Label),
                    link = LinkView(link.Link, slugs),
                }).ToList(),
            }).ToList(),
        };
    }

    [McpServerTool(Name = "list_assets")]
    [Description("List the media library (a single shared shelf, not per-site): each asset's id, name, kind, processing status, content type and resolved /media variant URLs. Find image ids here to use as a favicon or header logo. Optionally pass a siteId to validate it exists.")]
    public static object ListAssets(
        AssetLibrary assets,
        [Description("Optional site id to validate; the library is shared, so all assets are returned regardless.")] string? siteId,
        SiteOverview sites)
    {
        if (!string.IsNullOrWhiteSpace(siteId))
        {
            if (!TrySiteId(siteId, out var sid)) return Fail("invalid siteId");
            if (sites.Get(sid) is null) return Fail("unknown site");
        }

        return new { ok = true, assets = assets.All().Select(AuthoringApi.AssetView).ToList() };
    }

    [McpServerTool(Name = "get_asset")]
    [Description("One asset's processing status and resolved /media variant URLs. Poll this after upload_asset until status is Ready before using the asset as a favicon or logo.")]
    public static object GetAsset(
        [Description("The asset id (compact or dashed GUID).")] string assetId,
        AssetLibrary assets)
    {
        if (!AuthoringApi.TryAssetId(assetId, out var aid)) return Fail("invalid assetId");
        var asset = assets.Get(aid);
        return asset is null ? Fail("unknown asset") : new { ok = true, asset = AuthoringApi.AssetView(asset) };
    }

    // ── writes ───────────────────────────────────────────────────────────────────────────────────

    [McpServerTool(Name = "create_site")]
    [Description("Create a new site and return its id. The default locale (e.g. 'en', 'de-AT') is the language pages fall back to; defaults to 'en'.")]
    public static Task<object> CreateSite(
        [Description("The site name (1–100 chars).")] string name,
        [Description("Default locale tag, e.g. 'en' or 'de-AT'. Defaults to 'en'.")] string? defaultLocale,
        ICommandDispatcher dispatcher, IConfiguration config, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name)) return Task.FromResult(Fail("name is required"));
        var siteId = SiteId.New();
        return Dispatch(dispatcher, config, new CreateSiteCmd(siteId, name, string.IsNullOrWhiteSpace(defaultLocale) ? "en" : defaultLocale), ct,
            () => new { ok = true, siteId = siteId.Compact });
    }

    [McpServerTool(Name = "create_page")]
    [Description("Create a page on a site and return its id. Slug defaults to a slugified title; locale defaults to 'en'.")]
    public static Task<object> CreatePage(
        [Description("The site id.")] string siteId,
        [Description("The page title.")] string title,
        [Description("Optional URL slug (empty = derived from the title).")] string? slug,
        [Description("Optional content locale (default 'en').")] string? locale,
        ICommandDispatcher dispatcher, IConfiguration config, CancellationToken ct = default)
    {
        if (!TrySiteId(siteId, out var sid)) return Task.FromResult(Fail("invalid siteId"));
        if (string.IsNullOrWhiteSpace(title)) return Task.FromResult(Fail("title is required"));
        var pageId = PageId.New();
        return Dispatch(dispatcher, config,
            new CreatePageCmd(pageId, sid, title, slug ?? string.Empty, string.IsNullOrWhiteSpace(locale) ? "en" : locale), ct,
            () => new { ok = true, pageId = pageId.Compact });
    }

    [McpServerTool(Name = "insert_widget")]
    [Description("Insert a widget onto a page. If sectionId is given the widget goes into that section; otherwise a new top-level section is created to hold it (widgets cannot live at the page root). Props is an optional JSON object of string key/values.")]
    public static async Task<object> InsertWidget(
        [Description("The page id.")] string pageId,
        [Description("The widget tag (custom-element name, e.g. 'cai-verifier').")] string tag,
        [Description("Optional JSON object of string props, e.g. {\"title\":\"Hi\"}.")] string? propsJson,
        [Description("Optional section id to insert into; omit to create a new section.")] string? sectionId,
        [Description("Optional insert index within the section (or among top-level sections for a new one).")] int? index,
        ICommandDispatcher dispatcher, IConfiguration config, PageDrafts drafts, CancellationToken ct = default)
    {
        if (!TryPageId(pageId, out var pid)) return Fail("invalid pageId");
        if (string.IsNullOrWhiteSpace(tag)) return Fail("tag is required");
        if (!TryProps(propsJson, out var props, out var propsError)) return Fail(propsError);
        var page = drafts.Get(pid);
        if (page is null) return Fail("unknown page");

        var actor = ActorOf(config);
        using var _ = EditorActor.BeginScope(actor);

        NodeId section;
        int childIndex;
        if (!string.IsNullOrWhiteSpace(sectionId))
        {
            if (!NodeId.TryParse(sectionId, out section)) return Fail("invalid sectionId");
            if (page.Tree.Find(section) is not SectionNode existing) return Fail("sectionId is not a section on this page");
            childIndex = index ?? existing.Children.Count;
        }
        else
        {
            section = NodeId.New();
            var rootIndex = index ?? page.Tree.Roots.Count;
            var sectionResult = await dispatcher.Dispatch(new AddNodeCmd(pid, NodeId.Root, rootIndex, new SectionNode { Id = section }), ct);
            if (!sectionResult.Succeeded) return FailResult("could not create section", sectionResult);
            childIndex = 0;
        }

        var widgetId = NodeId.New();
        var widget = new WidgetNode { Id = widgetId, Tag = tag, Props = props };
        var result = await dispatcher.Dispatch(new AddNodeCmd(pid, section, childIndex, widget), ct);
        return result.Succeeded
            ? new { ok = true, widgetId = widgetId.Compact, sectionId = section.Compact }
            : FailResult("insert failed", result);
    }

    [McpServerTool(Name = "add_node")]
    [Description("Add a node — and its whole subtree in one call — to a page. The node spec is a JSON object: {\"type\":\"section|stack|columns|grid|heading|richtext|button|image|video|svg|divider|spacer|widget\", ...props, \"children\":[...]}. Text props (text/html/label/alt) take a plain string (default locale) or a {\"en\":\"…\"} object; rich-text html must be the canonical inline subset (<p>, <ul>/<ol>/<li>, <strong>, <em>, <a href>, <br>). parentId omitted ⇒ the page root, which accepts sections only. Ids are minted server-side.")]
    public static async Task<object> AddNode(
        [Description("The page id.")] string pageId,
        [Description("The node spec as JSON, e.g. {\"type\":\"heading\",\"level\":2,\"text\":\"The loop\"}.")] string nodeJson,
        [Description("Optional parent node id; omit for the page root (sections only).")] string? parentId,
        [Description("Optional insert index among the parent's children; omit to append.")] int? index,
        [Description("Optional locale for the spec's text (default: the site's default locale).")] string? locale,
        ICommandDispatcher dispatcher, IConfiguration config, PageDrafts drafts, SiteOverview sites, CancellationToken ct = default)
    {
        if (!TryPageId(pageId, out var pid)) return Fail("invalid pageId");
        var page = drafts.Get(pid);
        if (page is null) return Fail("unknown page");

        var parent = NodeId.Root;
        if (!string.IsNullOrWhiteSpace(parentId) && !NodeId.TryParse(parentId, out parent)) return Fail("invalid parentId");
        if (!TryLocale(sites, page, locale, out var contentLocale, out var localeError)) return Fail(localeError);

        JsonElement spec;
        try
        {
            using var document = JsonDocument.Parse(nodeJson ?? string.Empty);
            spec = document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return Fail("nodeJson must be a JSON object");
        }

        if (!AuthoringNodeJson.TryParse(spec, contentLocale, out var node, out var specError)) return Fail(specError);

        var siblings = parent.IsRoot
            ? page.Tree.Roots.Count
            : page.Tree.Find(parent) is IContainerNode container ? container.Children.Count : 0;
        return await Dispatch(dispatcher, config, new AddNodeCmd(pid, parent, index ?? siblings, node), ct,
            () => new { ok = true, nodeId = node.Id.Compact, parentId = parent.IsRoot ? null : parent.Compact });
    }

    [McpServerTool(Name = "edit_text")]
    [Description("Rewrite one text field on one node — the copy-editing tool. field is 'text' (heading), 'html' (rich text, canonical inline subset), 'label' (button) or 'alt' (image/graphic). Locale defaults to the site's default. Read get_page_tree first to see the current value.")]
    public static async Task<object> EditText(
        [Description("The page id.")] string pageId,
        [Description("The node id to edit.")] string nodeId,
        [Description("The field: text | html | label | alt.")] string field,
        [Description("The new value. For 'html' it must be canonical inline HTML, e.g. <p>Hello <strong>world</strong>.</p>.")] string value,
        [Description("Optional locale (default: the site's default locale).")] string? locale,
        ICommandDispatcher dispatcher, IConfiguration config, PageDrafts drafts, SiteOverview sites, CancellationToken ct = default)
    {
        if (!TryPageId(pageId, out var pid)) return Fail("invalid pageId");
        if (!NodeId.TryParse(nodeId, out var nid)) return Fail("invalid nodeId");
        if (string.IsNullOrWhiteSpace(field)) return Fail("field is required (text, html, label or alt)");
        var page = drafts.Get(pid);
        if (page is null) return Fail("unknown page");
        if (!TryLocale(sites, page, locale, out var contentLocale, out var localeError)) return Fail(localeError);

        return await Dispatch(dispatcher, config, new EditTextCmd(pid, nid, field, contentLocale.Value, value ?? string.Empty), ct,
            () => new { ok = true, nodeId = nid.Compact, field, locale = contentLocale.Value });
    }

    [McpServerTool(Name = "set_node_props")]
    [Description("Change a node's props — any node type. The patch is a JSON object of only the props you want changed (e.g. {\"appearance\":\"Hero\"} on a section, {\"level\":2} on a heading); everything else is left as it is. A widget is the exception: its props are the whole bag, so an empty/omitted object clears them.")]
    public static async Task<object> SetNodeProps(
        [Description("The page id.")] string pageId,
        [Description("The node id.")] string nodeId,
        [Description("JSON object of props to change. For a widget this is the complete prop bag.")] string? propsJson,
        [Description("Optional locale for any text props in the patch (default: the site's default locale).")] string? locale,
        ICommandDispatcher dispatcher, IConfiguration config, PageDrafts drafts, SiteOverview sites, CancellationToken ct = default)
    {
        if (!TryPageId(pageId, out var pid)) return Fail("invalid pageId");
        if (!NodeId.TryParse(nodeId, out var nid)) return Fail("invalid nodeId");
        var page = drafts.Get(pid);
        if (page is null) return Fail("unknown page");
        if (page.Tree.Find(nid) is not { } current) return Fail("unknown nodeId on this page");
        if (!TryLocale(sites, page, locale, out var contentLocale, out var localeError)) return Fail(localeError);

        JsonElement patch;
        try
        {
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(propsJson) ? "{}" : propsJson);
            patch = document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return Fail("props must be a JSON object");
        }

        if (!AuthoringNodeJson.TryApply(current, patch, contentLocale, out var replacement, out var applyError)) return Fail(applyError);
        return await Dispatch(dispatcher, config, new ChangeNodePropsCmd(pid, replacement), ct,
            () => new { ok = true, nodeId = nid.Compact });
    }

    [McpServerTool(Name = "move_node")]
    [Description("Move a node to a new parent and/or position — how a page is reordered. parentId omitted ⇒ the page root (sections only). Index is the slot among the target parent's children.")]
    public static async Task<object> MoveNode(
        [Description("The page id.")] string pageId,
        [Description("The node id to move.")] string nodeId,
        [Description("The target index among the new parent's children.")] int index,
        [Description("Optional new parent node id; omit for the page root.")] string? parentId,
        ICommandDispatcher dispatcher, IConfiguration config, CancellationToken ct = default)
    {
        if (!TryPageId(pageId, out var pid)) return Fail("invalid pageId");
        if (!NodeId.TryParse(nodeId, out var nid)) return Fail("invalid nodeId");
        var parent = NodeId.Root;
        if (!string.IsNullOrWhiteSpace(parentId) && !NodeId.TryParse(parentId, out parent)) return Fail("invalid parentId");
        return await Dispatch(dispatcher, config, new MoveNodeCmd(pid, nid, parent, index), ct,
            () => new { ok = true, nodeId = nid.Compact });
    }

    [McpServerTool(Name = "duplicate_node")]
    [Description("Copy a node and its whole subtree in beside the original — how a card grid grows another card without restating its structure. Returns the copy's node id.")]
    public static async Task<object> DuplicateNode(
        [Description("The page id.")] string pageId,
        [Description("The node id to copy.")] string nodeId,
        ICommandDispatcher dispatcher, IConfiguration config, CancellationToken ct = default)
    {
        if (!TryPageId(pageId, out var pid)) return Fail("invalid pageId");
        if (!NodeId.TryParse(nodeId, out var nid)) return Fail("invalid nodeId");
        var copyId = NodeId.New();
        return await Dispatch(dispatcher, config, new DuplicateNodeCmd(pid, nid, copyId), ct,
            () => new { ok = true, nodeId = copyId.Compact, copyOf = nid.Compact });
    }

    [McpServerTool(Name = "set_page_title")]
    [Description("Change a page's title (the name in listings and navigation fallbacks).")]
    public static async Task<object> SetPageTitle(
        [Description("The page id.")] string pageId,
        [Description("The new title.")] string title,
        [Description("Optional locale (default: the site's default locale).")] string? locale,
        ICommandDispatcher dispatcher, IConfiguration config, PageDrafts drafts, SiteOverview sites, CancellationToken ct = default)
    {
        if (!TryPageId(pageId, out var pid)) return Fail("invalid pageId");
        var page = drafts.Get(pid);
        if (page is null) return Fail("unknown page");
        if (!TryLocale(sites, page, locale, out var contentLocale, out var localeError)) return Fail(localeError);
        return await Dispatch(dispatcher, config, new ChangePageTitleCmd(pid, contentLocale.Value, title ?? string.Empty), ct,
            () => new { ok = true, pageId = pid.Compact });
    }

    [McpServerTool(Name = "set_page_meta")]
    [Description("Set a page's SEO meta title and/or meta description (the <title> and the search-result snippet). Pass null for a field to leave it unchanged.")]
    public static async Task<object> SetPageMeta(
        [Description("The page id.")] string pageId,
        [Description("The meta title, or null to leave it as it is.")] string? metaTitle,
        [Description("The meta description, or null to leave it as it is.")] string? metaDescription,
        [Description("Optional locale (default: the site's default locale).")] string? locale,
        ICommandDispatcher dispatcher, IConfiguration config, PageDrafts drafts, SiteOverview sites, CancellationToken ct = default)
    {
        if (!TryPageId(pageId, out var pid)) return Fail("invalid pageId");
        var page = drafts.Get(pid);
        if (page is null) return Fail("unknown page");
        if (!TryLocale(sites, page, locale, out var contentLocale, out var localeError)) return Fail(localeError);
        return await Dispatch(dispatcher, config, new ChangePageMetaCmd(pid, contentLocale.Value, metaTitle, metaDescription), ct,
            () => new { ok = true, pageId = pid.Compact });
    }

    [McpServerTool(Name = "set_navigation")]
    [Description("Replace the site's whole navigation — call get_site first and PUT back the order you want. itemsJson is a JSON array: {\"label\":\"Pricing\",\"pageId\":\"…\"} for a page link, {\"label\":\"Docs\",\"url\":\"https://…\"} for an external one, or {\"label\":\"Who it's for\",\"children\":[{\"label\":\"Teams\",\"pageId\":\"…\",\"description\":\"…\"}]} for a dropdown group.")]
    public static async Task<object> SetNavigation(
        [Description("The site id.")] string siteId,
        [Description("The navigation items as a JSON array.")] string itemsJson,
        [Description("Optional locale for the labels (default: the site's default locale).")] string? locale,
        ICommandDispatcher dispatcher, IConfiguration config, SiteOverview sites, CancellationToken ct = default)
    {
        if (!TrySiteId(siteId, out var sid)) return Fail("invalid siteId");
        var site = sites.Get(sid);
        if (site is null) return Fail("unknown site");
        var labelLocale = site.DefaultLocale;
        if (!string.IsNullOrWhiteSpace(locale) && !Locale.TryCreate(locale, out labelLocale)) return Fail($"'{locale}' is not a valid locale tag");

        List<NavigationItem> items;
        try
        {
            using var document = JsonDocument.Parse(itemsJson ?? string.Empty);
            if (document.RootElement.ValueKind != JsonValueKind.Array) return Fail("itemsJson must be a JSON array");
            items = [.. document.RootElement.EnumerateArray().Select(item => AuthoringApi.ParseNavigationItem(item, labelLocale))];
        }
        catch (JsonException)
        {
            return Fail("itemsJson must be a JSON array");
        }
        catch (ArgumentException ex)
        {
            return Fail(ex.Message);
        }

        return await Dispatch(dispatcher, config, new ChangeNavigationCmd(sid, items), ct,
            () => new { ok = true, siteId = sid.Compact, items = items.Count });
    }

    [McpServerTool(Name = "set_copy_line")]
    [Description("Set the footer's fine-print copy line, shown on every page of the site (e.g. '© 2025–2026 · …'). An empty text clears it. Other locales' values are preserved.")]
    public static async Task<object> SetCopyLine(
        [Description("The site id.")] string siteId,
        [Description("The copy line text, or empty to clear it.")] string? text,
        [Description("Optional locale (default: the site's default locale).")] string? locale,
        ICommandDispatcher dispatcher, IConfiguration config, SiteOverview sites, CancellationToken ct = default)
    {
        if (!TrySiteId(siteId, out var sid)) return Fail("invalid siteId");
        var site = sites.Get(sid);
        if (site is null) return Fail("unknown site");
        var lineLocale = site.DefaultLocale;
        if (!string.IsNullOrWhiteSpace(locale) && !Locale.TryCreate(locale, out lineLocale)) return Fail($"'{locale}' is not a valid locale tag");

        var updated = (site.CopyLine?.Text ?? LocalizedText.Empty).With(lineLocale, text ?? string.Empty);
        return await Dispatch(dispatcher, config, new SetCopyLineCmd(sid, updated.IsEmpty ? null : new CopyLine(updated)), ct,
            () => new { ok = true, siteId = sid.Compact, copyLine = Localized(updated) });
    }

    [McpServerTool(Name = "delete_node")]
    [Description("Delete a node (and its subtree) from a page.")]
    public static async Task<object> DeleteNode(
        [Description("The page id.")] string pageId,
        [Description("The node id to remove.")] string nodeId,
        ICommandDispatcher dispatcher, IConfiguration config, CancellationToken ct = default)
    {
        if (!TryPageId(pageId, out var pid)) return Fail("invalid pageId");
        if (!NodeId.TryParse(nodeId, out var nid)) return Fail("invalid nodeId");
        return await Dispatch(dispatcher, config, new RemoveNodeCmd(pid, nid), ct, () => new { ok = true, nodeId = nid.Compact });
    }

    [McpServerTool(Name = "upload_asset")]
    [Description("Upload a file from base64-encoded bytes and return the new asset id. Processing (image variants / SVG sanitize / video transcode) runs asynchronously — poll get_asset until status is Ready before using it. Then set_favicon / set_header_logo.")]
    public static async Task<object> UploadAsset(
        [Description("The file's bytes, base64-encoded.")] string base64,
        [Description("The file name, e.g. 'logo.png' (its extension matters).")] string fileName,
        [Description("The media type, e.g. 'image/png' or 'image/svg+xml'.")] string contentType,
        ICommandDispatcher dispatcher, IConfiguration config, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return Fail("fileName is required");
        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(base64 ?? string.Empty);
        }
        catch (FormatException)
        {
            return Fail("base64 is not valid base64");
        }

        if (bytes.Length == 0) return Fail("the file is empty");
        var type = string.IsNullOrWhiteSpace(contentType) || !contentType.Contains('/') ? "application/octet-stream" : contentType;
        var assetId = AssetId.New();
        await using var stream = new MemoryStream(bytes);
        return await Dispatch(dispatcher, config, new UploadAssetCmd(assetId, fileName, type, bytes.Length, stream), ct,
            () => new { ok = true, assetId = assetId.Compact, status = "Pending" });
    }

    [McpServerTool(Name = "set_favicon")]
    [Description("Set (or clear) the site's favicon — the browser tab / bookmark icon. Pass the asset id of an uploaded image, or null/empty to clear. The asset must already exist.")]
    public static async Task<object> SetFavicon(
        [Description("The site id.")] string siteId,
        [Description("The asset id to use, or null/empty to clear.")] string? assetId,
        ICommandDispatcher dispatcher, IConfiguration config, CancellationToken ct = default)
    {
        if (!TrySiteId(siteId, out var sid)) return Fail("invalid siteId");
        if (!TryOptionalAssetId(assetId, out var aid)) return Fail("invalid assetId");
        return await Dispatch(dispatcher, config, new SetFaviconCmd(sid, aid), ct,
            () => new { ok = true, siteId = sid.Compact, faviconAssetId = aid?.Compact });
    }

    [McpServerTool(Name = "set_header_logo")]
    [Description("Set (or clear) the site's header logo — shown in place of the brand dot in the published header and footer. Pass an uploaded image's asset id, or null/empty to clear. The asset must already exist.")]
    public static async Task<object> SetHeaderLogo(
        [Description("The site id.")] string siteId,
        [Description("The asset id to use, or null/empty to clear.")] string? assetId,
        ICommandDispatcher dispatcher, IConfiguration config, CancellationToken ct = default)
    {
        if (!TrySiteId(siteId, out var sid)) return Fail("invalid siteId");
        if (!TryOptionalAssetId(assetId, out var aid)) return Fail("invalid assetId");
        return await Dispatch(dispatcher, config, new SetHeaderLogoCmd(sid, aid), ct,
            () => new { ok = true, siteId = sid.Compact, headerLogoAssetId = aid?.Compact });
    }

    [McpServerTool(Name = "publish_page")]
    [Description("Publish ONE page to the site's output. Prefer this on a live site: publish_site also ships every other page that happens to be sitting stale.")]
    public static async Task<object> PublishPage(
        [Description("The page id.")] string pageId,
        ICommandDispatcher dispatcher, IConfiguration config, CancellationToken ct = default)
    {
        if (!TryPageId(pageId, out var pid)) return Fail("invalid pageId");
        return await Dispatch(dispatcher, config, new PublishPageCmd(pid), ct, () => new { ok = true, pageId = pid.Compact, published = true });
    }

    [McpServerTool(Name = "publish_site")]
    [Description("Publish every stale page of a site to its output. The static files (and any favicon/logo change) follow automatically on the projection catch-up.")]
    public static async Task<object> PublishSite(
        [Description("The site id.")] string siteId,
        ICommandDispatcher dispatcher, IConfiguration config, CancellationToken ct = default)
    {
        if (!TrySiteId(siteId, out var sid)) return Fail("invalid siteId");
        return await Dispatch(dispatcher, config, new PublishAllStaleCmd(sid), ct, () => new { ok = true, siteId = sid.Compact, published = true });
    }

    // ── internals ──────────────────────────────────────────────────────────────────────────────

    private static async Task<object> Dispatch(
        ICommandDispatcher dispatcher, IConfiguration config, ICommand command, CancellationToken ct, Func<object> onSuccess)
    {
        using var _ = EditorActor.BeginScope(ActorOf(config));
        var result = await dispatcher.Dispatch(command, ct);
        return result.Succeeded ? onSuccess() : FailResult($"{command.GetType().Name} failed", result);
    }

    private static string ActorOf(IConfiguration config) =>
        config[AuthoringApi.ActorKey] is { Length: > 0 } actor ? actor : "service:authoring-mcp";

    private static object Fail(string error) => new { ok = false, error };

    private static object FailResult(string error, Result result) => new { ok = false, error, details = result.Errors };

    private static bool TryProps(string? json, out PropBag bag, out string error)
    {
        error = string.Empty;
        bag = PropBag.Empty;
        if (string.IsNullOrWhiteSpace(json)) return true;
        try
        {
            var map = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (map is { Count: > 0 })
            {
                bag = PropBag.Of(map.Select(kv => new KeyValuePair<string, string>(kv.Key, kv.Value ?? string.Empty)));
            }

            return true;
        }
        catch (JsonException)
        {
            error = "props must be a JSON object of string values";
            return false;
        }
    }

    private static bool TrySiteId(string? s, out SiteId id)
    {
        if (Guid.TryParseExact(s, "N", out var g) || Guid.TryParse(s, out g)) { id = SiteId.From(g); return true; }
        id = default;
        return false;
    }

    private static bool TryPageId(string? s, out PageId id)
    {
        if (Guid.TryParseExact(s, "N", out var g) || Guid.TryParse(s, out g)) { id = PageId.From(g); return true; }
        id = default;
        return false;
    }

    private static bool TryOptionalAssetId(string? s, out AssetId? id)
    {
        if (string.IsNullOrWhiteSpace(s)) { id = null; return true; }
        if (AuthoringApi.TryAssetId(s, out var parsed)) { id = parsed; return true; }
        id = null;
        return false;
    }

    /// <summary>The locale a page write lands in: the caller's if given, else the owning site's default.</summary>
    private static bool TryLocale(SiteOverview sites, Page page, string? requested, out Locale locale, out string error)
    {
        error = string.Empty;
        if (!string.IsNullOrWhiteSpace(requested))
        {
            if (!Locale.TryCreate(requested, out locale))
            {
                error = $"'{requested}' is not a valid locale tag (expected e.g. 'en' or 'de-AT').";
                return false;
            }

            return true;
        }

        var site = sites.Get(page.SiteId);
        if (site is null)
        {
            locale = default;
            error = "the page's site is unknown";
            return false;
        }

        locale = site.DefaultLocale;
        return true;
    }

    private static Dictionary<string, string> Localized(LocalizedText text) =>
        text.Values.ToDictionary(kv => kv.Key.Value, kv => kv.Value, StringComparer.Ordinal);

    private static object? LinkView(Link? link, IReadOnlyDictionary<PageId, string> slugs) => link switch
    {
        PageLink page => new { kind = "page", pageId = page.PageId.Compact, slug = slugs.GetValueOrDefault(page.PageId) },
        ExternalLink external => new { kind = "external", url = external.Url },
        _ => null,
    };

    /// <summary>A site row for <c>list_sites</c>.</summary>
    public sealed record SiteInfo(string Id, string Name, string DefaultLocale);
}
