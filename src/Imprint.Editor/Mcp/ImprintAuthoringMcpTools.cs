using System.ComponentModel;
using System.Text.Json;
using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Assets;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Posts;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Projections;
using Imprint.Authoring.Syndication;
using Imprint.Editor.Api;
using Imprint.Editor.Auth;
using Imprint.EventSourcing;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Server;
using AddNodeCmd = Imprint.Authoring.Features.Pages.AddNode.AddNode;
using ChangeNavigationCmd = Imprint.Authoring.Features.Sites.ChangeNavigation.ChangeNavigation;
using SetFooterCmd = Imprint.Authoring.Features.Sites.SetFooter.SetFooter;
using AddLocaleCmd = Imprint.Authoring.Features.Sites.AddLocale.AddLocale;
using SetHeaderActionsCmd = Imprint.Authoring.Features.Sites.SetHeaderActions.SetHeaderActions;
using ChangeNodePropsCmd = Imprint.Authoring.Features.Pages.ChangeNodeProps.ChangeNodeProps;
using ChangePageMetaCmd = Imprint.Authoring.Features.Pages.ChangePageMeta.ChangePageMeta;
using SetPageArticleCmd = Imprint.Authoring.Features.Pages.SetPageArticle.SetPageArticle;
using ChangePageTitleCmd = Imprint.Authoring.Features.Pages.ChangePageTitle.ChangePageTitle;
using CreatePageCmd = Imprint.Authoring.Features.Pages.CreatePage.CreatePage;
using DeletePageCmd = Imprint.Authoring.Features.Pages.DeletePage.DeletePage;
using RestorePageToRevisionCmd = Imprint.Authoring.Features.Pages.RestorePageToRevision.RestorePageToRevision;
using CreateSiteCmd = Imprint.Authoring.Features.Sites.CreateSite.CreateSite;
using DuplicateNodeCmd = Imprint.Authoring.Features.Pages.DuplicateNode.DuplicateNode;
using EditTextCmd = Imprint.Authoring.Features.Pages.EditText.EditText;
using MoveNodeCmd = Imprint.Authoring.Features.Pages.MoveNode.MoveNode;
using PublishAllStaleCmd = Imprint.Authoring.Features.Pages.PublishAllStale.PublishAllStale;
using PublishPageCmd = Imprint.Authoring.Features.Pages.PublishPage.PublishPage;
using UnpublishPageCmd = Imprint.Authoring.Features.Pages.UnpublishPage.UnpublishPage;
using RemoveNodeCmd = Imprint.Authoring.Features.Pages.RemoveNode.RemoveNode;
using RemoveLocaleCmd = Imprint.Authoring.Features.Sites.RemoveLocale.RemoveLocale;
using SeedLocaleCmd = Imprint.Authoring.Features.Sites.SeedLocale.SeedLocale;
using SetCopyLineCmd = Imprint.Authoring.Features.Sites.SetCopyLine.SetCopyLine;
using SetFaviconCmd = Imprint.Authoring.Features.Sites.SetFavicon.SetFavicon;
using SetSocialImageCmd = Imprint.Authoring.Features.Sites.SetSocialImage.SetSocialImage;
using SetLlmsExcludedPathsCmd = Imprint.Authoring.Features.Sites.SetLlmsExcludedPaths.SetLlmsExcludedPaths;
using SetLlmsPreambleCmd = Imprint.Authoring.Features.Sites.SetLlmsPreamble.SetLlmsPreamble;
using SetHeaderLogoCmd = Imprint.Authoring.Features.Sites.SetHeaderLogo.SetHeaderLogo;
using ChangePostBodyCmd = Imprint.Authoring.Features.Posts.ChangePostBody.ChangePostBody;
using ChangePostMetaCmd = Imprint.Authoring.Features.Posts.ChangePostMeta.ChangePostMeta;
using CreatePostCmd = Imprint.Authoring.Features.Posts.CreatePost.CreatePost;
using SetSiteReviewerCmd = Imprint.Authoring.Features.Sites.SetSiteReviewer.SetSiteReviewer;
using SubmitPostForReviewCmd = Imprint.Authoring.Features.Posts.SubmitPostForReview.SubmitPostForReview;
using TagAssetCmd = Imprint.Authoring.Features.Assets.TagAsset.TagAsset;
using UntagAssetCmd = Imprint.Authoring.Features.Assets.UntagAsset.UntagAsset;
using UploadAssetCmd = Imprint.Authoring.Features.Assets.UploadAsset.UploadAsset;
using UploadAssetDarkVariantCmd = Imprint.Authoring.Features.Assets.UploadAssetDarkVariant.UploadAssetDarkVariant;

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
    [Description("Add a node — and its whole subtree in one call — to a page. The node spec is a JSON object: {\"type\":\"section|stack|columns|grid|heading|richtext|code|table|button|image|video|svg|divider|spacer|widget\", ...props, \"children\":[...]}. Text props (text/html/label/alt) take a plain string (default locale) or a {\"en\":\"…\"} object; rich-text html must be the canonical inline subset (<p>, <ul>/<ol>/<li>, <strong>, <em>, <a href>, <br>). A table takes {\"head\":[\"…\"],\"rows\":[[\"…\"]]} (cells: string or locale object); code takes {\"text\":…,\"language\":…}. parentId omitted ⇒ the page root, which accepts sections only. Ids are minted server-side.")]
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

    [McpServerTool(Name = "set_page_article")]
    [Description("Declare (or clear) a page as an ARTICLE for structured data: a named human author and an ISO publication date (yyyy-MM-dd). The published page then carries a schema.org TechArticle node with author, datePublished and publisher — the facts that make a document citable by search engines and models rather than merely crawlable. Pass both to declare, neither to clear; half a declaration is refused. Use it for whitepapers, research pages and anything a person signed.")]
    public static async Task<object> SetPageArticle(
        [Description("The page id.")] string pageId,
        [Description("The named human author, or null to clear the declaration.")] string? author,
        [Description("The publication date (yyyy-MM-dd), or null to clear.")] string? published,
        ICommandDispatcher dispatcher, IConfiguration config, PageDrafts drafts, CancellationToken ct = default)
    {
        if (!TryPageId(pageId, out var pid)) return Fail("invalid pageId");
        if (drafts.Get(pid) is null) return Fail("unknown page");
        return await Dispatch(dispatcher, config, new SetPageArticleCmd(pid, author, published), ct,
            () => new { ok = true, pageId = pid.Compact });
    }

    [McpServerTool(Name = "set_navigation")]
    [Description("Replace the site's whole navigation — call get_site first and PUT back the order you want. itemsJson is a JSON array: {\"label\":\"Pricing\",\"pageId\":\"…\"} for a page link (add \"fragment\":\"pricing\" to land on one section of that page — unlike an absolute URL it stays in the reader's locale), {\"label\":\"Docs\",\"url\":\"https://…\"} for an external one, or {\"label\":\"Who it's for\",\"children\":[{\"label\":\"Teams\",\"pageId\":\"…\",\"description\":\"…\"}]} for a dropdown group.")]
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

        items = AuthoringApi.CarryOtherLocales(items, site.Navigation, labelLocale);

        return await Dispatch(dispatcher, config, new ChangeNavigationCmd(sid, items), ct,
            () => new { ok = true, siteId = sid.Compact, items = items.Count });
    }

    [McpServerTool(Name = "set_footer")]
    [Description("Replace the site's whole footer — call get_site first and PUT back the columns you want. groupsJson is a JSON array of columns: {\"heading\":\"Product\",\"links\":[{\"label\":\"Pricing\",\"pageId\":\"…\",\"fragment\":\"pricing\"},{\"label\":\"Docs\",\"url\":\"https://…\"}]}. Use this to fix a broken footer link — the footer is otherwise only editable in the interactive editor.")]
    public static async Task<object> SetFooter(
        [Description("The site id.")] string siteId,
        [Description("The footer columns as a JSON array.")] string groupsJson,
        [Description("Optional locale for the headings and labels (default: the site's default locale).")] string? locale,
        ICommandDispatcher dispatcher, IConfiguration config, SiteOverview sites, CancellationToken ct = default)
    {
        if (!TrySiteId(siteId, out var sid)) return Fail("invalid siteId");
        var site = sites.Get(sid);
        if (site is null) return Fail("unknown site");
        var labelLocale = site.DefaultLocale;
        if (!string.IsNullOrWhiteSpace(locale) && !Locale.TryCreate(locale, out labelLocale)) return Fail($"'{locale}' is not a valid locale tag");

        List<FooterLinkGroup> groups;
        try
        {
            using var document = JsonDocument.Parse(groupsJson ?? string.Empty);
            if (document.RootElement.ValueKind != JsonValueKind.Array) return Fail("groupsJson must be a JSON array");
            groups = [.. document.RootElement.EnumerateArray().Select(group => AuthoringApi.ParseFooterGroup(group, labelLocale))];
        }
        catch (JsonException)
        {
            return Fail("groupsJson must be a JSON array");
        }
        catch (ArgumentException ex)
        {
            return Fail(ex.Message);
        }

        groups = AuthoringApi.CarryOtherLocales(groups, site.FooterGroups, labelLocale);

        return await Dispatch(dispatcher, config, new SetFooterCmd(sid, groups), ct,
            () => new { ok = true, siteId = sid.Compact, groups = groups.Count, links = groups.Sum(g => g.Links.Count) });
    }

    [McpServerTool(Name = "add_locale")]
    [Description("Add a language to the site, e.g. 'da'. Every text field is stored per locale, so this adds a slot rather than changing anything: existing content stays on the locale it was written in, and edit_text/set_page_meta then write the translation by passing the new locale. Follow it with seed_locale — text falls back to the default locale, so an unseeded language renders the default one and looks finished while being empty. Call get_site to see which locales exist.")]
    public static async Task<object> AddLocale(
        [Description("The site id.")] string siteId,
        [Description("The locale tag to add, e.g. 'da' or 'de-AT'.")] string locale,
        ICommandDispatcher dispatcher, IConfiguration config, SiteOverview sites, CancellationToken ct = default)
    {
        if (!TrySiteId(siteId, out var sid)) return Fail("invalid siteId");
        if (sites.Get(sid) is null) return Fail("unknown site");
        if (string.IsNullOrWhiteSpace(locale)) return Fail("a locale is required, e.g. 'da'");

        return await Dispatch(dispatcher, config, new AddLocaleCmd(sid, locale), ct,
            () => new { ok = true, siteId = sid.Compact, locale });
    }

    [McpServerTool(Name = "set_header_actions")]
    [Description("Set the header's primary CTA and quiet link. They share a slot and are set TOGETHER, so omitting one clears it - which is how a header link pointing at a page that no longer exists gets removed. Each is a JSON object: {\"label\":\"Contact\",\"pageId\":\"…\",\"fragment\":\"section\"} or {\"label\":\"Docs\",\"url\":\"https://…\"}. Pass null or omit to clear.")]
    public static async Task<object> SetHeaderActions(
        [Description("The site id.")] string siteId,
        [Description("The primary CTA as a JSON object, or null/omitted to clear it.")] string? ctaJson,
        [Description("The quiet link as a JSON object, or null/omitted to clear it.")] string? quietJson,
        [Description("Optional locale for the labels (default: the site's default locale).")] string? locale,
        ICommandDispatcher dispatcher, IConfiguration config, SiteOverview sites, CancellationToken ct = default)
    {
        if (!TrySiteId(siteId, out var sid)) return Fail("invalid siteId");
        var site = sites.Get(sid);
        if (site is null) return Fail("unknown site");
        var labelLocale = site.DefaultLocale;
        if (!string.IsNullOrWhiteSpace(locale) && !Locale.TryCreate(locale, out labelLocale)) return Fail($"'{locale}' is not a valid locale tag");

        HeaderAction? cta, quiet;
        try
        {
            cta = ParseAction(ctaJson, "cta", labelLocale);
            quiet = ParseAction(quietJson, "quiet", labelLocale);
        }
        catch (JsonException)
        {
            return Fail("ctaJson and quietJson must each be a JSON object, or omitted");
        }
        catch (ArgumentException ex)
        {
            return Fail(ex.Message);
        }

        return await Dispatch(dispatcher, config, new SetHeaderActionsCmd(sid, cta, quiet), ct,
            () => new { ok = true, siteId = sid.Compact, cta = cta is not null, quiet = quiet is not null });

        static HeaderAction? ParseAction(string? json, string name, Locale locale)
        {
            if (string.IsNullOrWhiteSpace(json) || json.Trim() is "null") return null;
            using var document = JsonDocument.Parse(json);
            var wrapper = JsonDocument.Parse($"{{\"{name}\":{json}}}");
            return AuthoringApi.ParseHeaderAction(wrapper.RootElement, name, locale);
        }
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

    [McpServerTool(Name = "remove_locale")]
    [Description("Stop publishing a language. Use it when a locale was added but never translated — an unseeded locale renders the DEFAULT language under its own path and looks finished while being empty, which is worse than not offering the language at all. Nothing is lost: translations stay in history and come back if the locale is re-added. The default locale cannot be removed.")]
    public static async Task<object> RemoveLocale(
        [Description("The site id.")] string siteId,
        [Description("The locale tag to stop publishing, e.g. 'da'.")] string locale,
        ICommandDispatcher dispatcher, IConfiguration config, SiteOverview sites, CancellationToken ct = default)
    {
        if (!TrySiteId(siteId, out var sid)) return Fail("invalid siteId");
        if (sites.Get(sid) is null) return Fail("unknown site");
        if (!Locale.TryCreate(locale, out _)) return Fail($"'{locale}' is not a valid locale tag");

        return await Dispatch(dispatcher, config, new RemoveLocaleCmd(sid, locale), ct,
            () => new { ok = true, siteId = sid.Compact, removed = locale });
    }

    [McpServerTool(Name = "seed_locale")]
    [Description("Copy every text the site holds in one locale into another, wherever the target has nothing yet — the chrome and every page. Run it straight after add_locale, so translating is editing rather than retyping. It fills gaps only: an already-translated string is never overwritten, and re-running is safe.")]
    public static async Task<object> SeedLocale(
        [Description("The site id.")] string siteId,
        [Description("The locale to fill, e.g. 'da'.")] string locale,
        [Description("The locale to copy from, e.g. 'en'.")] string source,
        ICommandDispatcher dispatcher, IConfiguration config, SiteOverview sites, CancellationToken ct = default)
    {
        if (!TrySiteId(siteId, out var sid)) return Fail("invalid siteId");
        if (sites.Get(sid) is null) return Fail("unknown site");

        return await Dispatch(dispatcher, config, new SeedLocaleCmd(sid, locale, source), ct,
            () => new { ok = true, siteId = sid.Compact, seeded = locale, from = source });
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

    [McpServerTool(Name = "page_history")]
    [Description("Every revision of a page, oldest first: version, timestamp, actor and what changed. "
                 + "An event-sourced CMS keeps them all by construction — this is how you read them. "
                 + "content=true also returns the TEXT of each text change, which is what makes recovering "
                 + "an overwritten sentence possible; without it the log stays compact but still carries each "
                 + "change's length, so a blanking is visible either way. Pair with restore_page_revision.")]
    public static async Task<object> PageHistory(
        [Description("The page id.")] string pageId,
        [Description("Include the text of each text change (default false).")] bool? content,
        IEventStore events, PageList pages, CancellationToken ct = default)
    {
        if (!TryPageId(pageId, out var pid)) return Fail("invalid pageId");
        if (pages.Get(pid) is null) return Fail("unknown page");

        var stream = await events.ReadStream(pid.Stream, ct: ct);
        return new
        {
            ok = true,
            pageId = pid.Compact,
            revisions = stream.Select(e => AuthoringApi.PageRevisionView(e, content ?? false)).ToList(),
        };
    }

    [McpServerTool(Name = "restore_page_revision")]
    [Description("Put a page's CONTENT back to how it stood at a given revision (see page_history). The "
                 + "restore is appended as a new revision rather than rewriting history, so the change being "
                 + "undone stays readable. Content only: slug, title, meta and published state are left alone, "
                 + "because silently reverting a slug would break every inbound link to the page. The page is "
                 + "left as a draft — publish it when the restored content is what you want live.")]
    public static async Task<object> RestorePageRevision(
        [Description("The page id.")] string pageId,
        [Description("The revision to restore to, as reported by page_history.")] long version,
        ICommandDispatcher dispatcher, IConfiguration config, CancellationToken ct = default)
    {
        if (!TryPageId(pageId, out var pid)) return Fail("invalid pageId");
        return await Dispatch(dispatcher, config, new RestorePageToRevisionCmd(pid, version), ct,
            () => new { ok = true, pageId = pid.Compact, restoredTo = version });
    }

    [McpServerTool(Name = "delete_page")]
    [Description("Delete a whole page from a site. Refused while the page is still in the site navigation "
                 + "(remove it there first), and refused for the only page on a site. Deleting a PUBLISHED page "
                 + "also withdraws its files on the next publisher pass, so the live URL stops resolving.")]
    public static async Task<object> DeletePage(
        [Description("The page id.")] string pageId,
        ICommandDispatcher dispatcher, IConfiguration config, CancellationToken ct = default)
    {
        if (!TryPageId(pageId, out var pid)) return Fail("invalid pageId");
        return await Dispatch(dispatcher, config, new DeletePageCmd(pid), ct, () => new { ok = true, pageId = pid.Compact, deleted = true });
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

    [McpServerTool(Name = "upload_asset_dark")]
    [Description("Attach a dark-mode rendition to an EXISTING image or SVG asset, from base64-encoded bytes. Without one, a single rendition is inlined into BOTH colour schemes — so an SVG authored with a light background ships onto a dark page. Processing is async: poll get_asset until Ready. Re-uploading supersedes the previous dark rendition.")]
    public static async Task<object> UploadAssetDark(
        [Description("The id of the asset to attach the dark rendition to.")] string assetId,
        [Description("The dark file's bytes, base64-encoded.")] string base64,
        [Description("The file name, e.g. 'logo-dark.svg' (its extension matters).")] string fileName,
        [Description("The media type, e.g. 'image/png' or 'image/svg+xml'.")] string contentType,
        ICommandDispatcher dispatcher, IConfiguration config, AssetLibrary assets, CancellationToken ct = default)
    {
        if (!AuthoringApi.TryAssetId(assetId, out var aid)) return Fail("invalid assetId");
        if (assets.Get(aid) is null) return Fail("unknown asset");
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
        await using var stream = new MemoryStream(bytes);
        return await Dispatch(dispatcher, config, new UploadAssetDarkVariantCmd(aid, fileName, type, bytes.Length, stream), ct,
            () => new { ok = true, assetId = aid.Compact, status = "Pending", dark = true });
    }

    [McpServerTool(Name = "list_posts")]
    [Description("List a site's blog posts: id, slug, title, status (Draft/InReview/ChangesRequested/Approved/Scheduled/Published/Modified), the reviewer's note if it was sent back, the go-live date and the published date.")]
    public static object ListPosts(
        [Description("The site id (compact or dashed GUID).")] string siteId,
        SiteOverview sites, PostList posts)
    {
        if (!TrySiteId(siteId, out var sid)) return Fail("invalid siteId");
        if (sites.Get(sid) is not { } site) return Fail("unknown site");
        return new { ok = true, posts = posts.All(sid).Select(post => AuthoringApi.PostView(post, site)).ToList() };
    }

    [McpServerTool(Name = "get_post")]
    [Description("One post in full, INCLUDING its markdown body and meta per locale — what list_posts deliberately leaves out. Use it to read back what was stored before editing it, so an edit replaces prose you have actually seen.")]
    public static async Task<object> GetPost(
        [Description("The post id (compact or dashed GUID).")] string postId,
        SiteOverview sites, PostList posts, IAggregateStore store, CancellationToken ct = default)
    {
        if (!AuthoringApi.TryPostIdPublic(postId, out var pid)) return Fail("invalid postId");
        if (posts.Get(pid) is not { } summary) return Fail("unknown post");
        if (await store.LoadOrDefault<Post>(pid.Stream, ct) is not { } post) return Fail("unknown post");
        return new { ok = true, post = AuthoringApi.PostDetailView(summary, sites.Get(summary.SiteId), post) };
    }

    [McpServerTool(Name = "create_post")]
    [Description("Create a blog post and return its id. The body is markdown, set separately with set_post_body. The slug is derived from the title unless given.")]
    public static async Task<object> CreatePost(
        [Description("The site id the post belongs to.")] string siteId,
        [Description("The post title.")] string title,
        [Description("Optional URL slug; derived from the title when omitted.")] string? slug,
        [Description("Optional locale tag; the site's default when omitted.")] string? locale,
        ICommandDispatcher dispatcher, IConfiguration config, SiteOverview sites, CancellationToken ct = default)
    {
        if (!TrySiteId(siteId, out var sid)) return Fail("invalid siteId");
        if (sites.Get(sid) is not { } site) return Fail("unknown site");
        if (string.IsNullOrWhiteSpace(title)) return Fail("title is required");

        var postId = PostId.New();
        var tag = locale is { Length: > 0 } given ? given : site.DefaultLocale.Value;
        var address = slug is { Length: > 0 } chosen ? chosen : Slug.Suggest(title);
        return await Dispatch(dispatcher, config, new CreatePostCmd(postId, sid, title, address, tag), ct,
            () => new { ok = true, postId = postId.Compact, slug = address, locale = tag });
    }

    [McpServerTool(Name = "set_post_body")]
    [Description("Replace a post's markdown body for one locale. Markdown is the authored truth — the node tree is rebuilt from it at publish, so a later converter fix reaches posts already written. Images are referenced as ![alt](media:{assetId}) and must stand alone in their paragraph.")]
    public static async Task<object> SetPostBody(
        [Description("The post id.")] string postId,
        [Description("The markdown body.")] string markdown,
        [Description("Optional locale tag; the site's default when omitted.")] string? locale,
        ICommandDispatcher dispatcher, IConfiguration config, PostList posts, SiteOverview sites, CancellationToken ct = default)
    {
        if (!AuthoringApi.TryPostIdPublic(postId, out var pid)) return Fail("invalid postId");
        if (posts.Get(pid) is not { } post) return Fail("unknown post");
        var tag = locale is { Length: > 0 } given ? given : sites.Get(post.SiteId)?.DefaultLocale.Value ?? "en";
        return await Dispatch(dispatcher, config, new ChangePostBodyCmd(pid, tag, markdown ?? ""), ct,
            () => new { ok = true, postId = pid.Compact, locale = tag });
    }

    [McpServerTool(Name = "set_post_meta")]
    [Description("Set a post's SEO meta title and description for one locale.")]
    public static async Task<object> SetPostMeta(
        [Description("The post id.")] string postId,
        [Description("Meta title, or null to leave it.")] string? metaTitle,
        [Description("Meta description, or null to leave it.")] string? metaDescription,
        [Description("Optional locale tag; the site's default when omitted.")] string? locale,
        ICommandDispatcher dispatcher, IConfiguration config, PostList posts, SiteOverview sites, CancellationToken ct = default)
    {
        if (!AuthoringApi.TryPostIdPublic(postId, out var pid)) return Fail("invalid postId");
        if (posts.Get(pid) is not { } post) return Fail("unknown post");
        var tag = locale is { Length: > 0 } given ? given : sites.Get(post.SiteId)?.DefaultLocale.Value ?? "en";
        return await Dispatch(dispatcher, config, new ChangePostMetaCmd(pid, tag, metaTitle, metaDescription), ct,
            () => new { ok = true, postId = pid.Compact, locale = tag });
    }

    [McpServerTool(Name = "submit_post_for_review")]
    [Description("Hand a post to the site's reviewer, with a proposed go-live date they may change (ISO 8601 with offset, e.g. 2026-09-01T09:00:00+02:00). Omit the date for 'to be decided'. Fails when the site has no reviewer configured — set one with set_site_reviewer. The reviewer is emailed if a mail relay is configured.")]
    public static async Task<object> SubmitPostForReview(
        [Description("The post id.")] string postId,
        [Description("Proposed go-live instant, ISO 8601 with offset; omit for 'to be decided'.")] string? proposedPublishAt,
        [Description("Optional note for the reviewer.")] string? note,
        ICommandDispatcher dispatcher, IConfiguration config, PostList posts, SiteOverview sites, CancellationToken ct = default)
    {
        if (!AuthoringApi.TryPostIdPublic(postId, out var pid)) return Fail("invalid postId");
        if (posts.Get(pid) is not { } post) return Fail("unknown post");
        if (!TryInstant(proposedPublishAt, out var at)) return Fail($"'{proposedPublishAt}' is not an ISO 8601 instant");
        var tag = sites.Get(post.SiteId)?.DefaultLocale.Value ?? "en";
        return await Dispatch(dispatcher, config, new SubmitPostForReviewCmd(pid, tag, at, note), ct,
            () => new { ok = true, postId = pid.Compact, status = "InReview", proposedPublishAt = at });
    }

    [McpServerTool(Name = "set_site_reviewer")]
    [Description("Name (or clear, with a blank email) the site's public-relations reviewer. With one set, posts on that site cannot be published directly: they are submitted, and the reviewer approves the words and the date. Naming someone does NOT grant them editor access — add them as a collaborator too.")]
    public static async Task<object> SetSiteReviewer(
        [Description("The site id.")] string siteId,
        [Description("The reviewer's name, e.g. 'Lasse'.")] string? name,
        [Description("The reviewer's email; blank clears the role.")] string? email,
        ICommandDispatcher dispatcher, IConfiguration config, CancellationToken ct = default)
    {
        if (!TrySiteId(siteId, out var sid)) return Fail("invalid siteId");
        return await Dispatch(dispatcher, config, new SetSiteReviewerCmd(sid, name, email), ct,
            () => new { ok = true, siteId = sid.Compact, reviewer = email });
    }

    private static bool TryInstant(string? text, out DateTimeOffset? instant)
    {
        instant = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;   // absent is "to be decided", not a parse failure
        }

        if (!DateTimeOffset.TryParse(text, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var parsed))
        {
            return false;
        }

        instant = parsed;
        return true;
    }

    [McpServerTool(Name = "tag_assets")]
    [Description("File assets under one or more tags — the library is a single shared shelf, and tags are what make a group findable again (e.g. all the figures of one post). Every tag is applied to every asset id given, so a whole group is one call. Already-carried tags are a no-op, not an error. Tags are compared case-insensitively; see the current ones in list_assets.")]
    public static Task<object> TagAssets(
        [Description("The asset ids to file (compact or dashed GUIDs).")] string[]? assetIds,
        [Description("The tags to apply, e.g. ['B20','Blog']. Each is trimmed; max 60 chars, 25 per asset.")] string[]? tags,
        ICommandDispatcher dispatcher, IConfiguration config, AssetLibrary assets, CancellationToken ct = default) =>
        ApplyTags(assetIds, tags, add: true, dispatcher, config, assets, ct);

    [McpServerTool(Name = "untag_assets")]
    [Description("Remove one or more tags from assets. Every tag is removed from every asset id given. A tag the asset never carried is a no-op, not an error. A tag stops existing when its last asset drops it.")]
    public static Task<object> UntagAssets(
        [Description("The asset ids to unfile (compact or dashed GUIDs).")] string[]? assetIds,
        [Description("The tags to remove.")] string[]? tags,
        ICommandDispatcher dispatcher, IConfiguration config, AssetLibrary assets, CancellationToken ct = default) =>
        ApplyTags(assetIds, tags, add: false, dispatcher, config, assets, ct);

    private static async Task<object> ApplyTags(
        string[]? assetIds, string[]? tags, bool add,
        ICommandDispatcher dispatcher, IConfiguration config, AssetLibrary assets, CancellationToken ct)
    {
        // Validated as a batch first: a half-applied call would leave the caller working out which
        // pairs landed before the typo'd id.
        var (ids, names, error) = AuthoringApi.ReadTagBatch(assetIds, tags, assets);
        if (error is not null) return Fail(error);

        foreach (var id in ids)
        {
            foreach (var tag in names)
            {
                ICommand command = add
                    ? new TagAssetCmd(id, tag)
                    : new UntagAssetCmd(id, tag);

                using var _ = EditorActor.BeginScope(ActorOf(config));
                var result = await dispatcher.Dispatch(command, ct);
                if (!result.Succeeded)
                {
                    return FailResult($"{(add ? "tag" : "untag")} '{tag}' on {id.Compact} failed", result);
                }
            }
        }

        return new
        {
            ok = true,
            tags = names,
            assets = ids.Select(id => AuthoringApi.AssetView(assets.Get(id)!)).ToList(),
        };
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

    [McpServerTool(Name = "set_llms_preamble")]
    [Description("Set (or clear) the site's llms.txt preamble - the markdown the site says about ITSELF, emitted above the generated page index. This is what a model reads to learn what the site IS; a list of page titles cannot tell it that. Write real markdown (headings, bullets). Pass null/empty to clear, and llms.txt falls back to the site name plus the home page description. Max 20000 characters.")]
    public static async Task<object> SetLlmsPreamble(
        [Description("The site id.")] string siteId,
        [Description("The markdown preamble, or null/empty to clear.")] string? preamble,
        ICommandDispatcher dispatcher, IConfiguration config, CancellationToken ct = default)
    {
        if (!TrySiteId(siteId, out var sid)) return Fail("invalid siteId");
        return await Dispatch(dispatcher, config, new SetLlmsPreambleCmd(sid, preamble), ct,
            () => new { ok = true, siteId = sid.Compact, length = preamble?.Length ?? 0 });
    }

    [McpServerTool(Name = "set_llms_excluded_paths")]
    [Description("Declare which path prefixes are published for search engines ONLY, and therefore stay out of llms.txt and llms-full.txt. Each prefix covers itself and everything nested under it: 'surveys/github' excludes surveys/github/... while leaving the /surveys/ index page listed. A trailing '*' on the LAST segment matches by segment prefix instead, so 'dimensions/rubric*' covers every dated dimensions/rubric-2026.08.19 snapshot without naming them one by one (and keeps covering new ones). Use this when a site serves many generated pages that are good SEO but noise to a model trying to learn what the site is. sitemap.xml is NEVER affected - those pages exist to be indexed, and this does not stop any crawler from fetching them. The LLM files state how many pages were left out and point at the sitemap, so the omission is never silent. Pass an empty array to clear. Max 20 prefixes.")]
    public static async Task<object> SetLlmsExcludedPaths(
        [Description("The site id.")] string siteId,
        [Description("Path prefixes, e.g. ['surveys/github','surveys/gitlab']. Empty clears the policy.")] string[]? paths,
        ICommandDispatcher dispatcher, IConfiguration config, CancellationToken ct = default)
    {
        if (!TrySiteId(siteId, out var sid)) return Fail("invalid siteId");
        return await Dispatch(dispatcher, config, new SetLlmsExcludedPathsCmd(sid, paths), ct,
            () => new { ok = true, siteId = sid.Compact, prefixes = paths?.Length ?? 0 });
    }

    [McpServerTool(Name = "set_social_image")]
    [Description("Set (or clear) the site's share card image - the og:image every page carries, shown when the URL is pasted into a chat app, a social platform or handed to a model. Wants a WIDE image (about 1200x630); the header logo is the wrong shape and is never used as a stand-in. Pass an uploaded image's asset id, or null/empty to clear. The asset must already exist.")]
    public static async Task<object> SetSocialImage(
        [Description("The site id.")] string siteId,
        [Description("The asset id to use, or null/empty to clear.")] string? assetId,
        ICommandDispatcher dispatcher, IConfiguration config, CancellationToken ct = default)
    {
        if (!TrySiteId(siteId, out var sid)) return Fail("invalid siteId");
        if (!TryOptionalAssetId(assetId, out var aid)) return Fail("invalid assetId");
        return await Dispatch(dispatcher, config, new SetSocialImageCmd(sid, aid), ct,
            () => new { ok = true, siteId = sid.Compact, socialImageAssetId = aid?.Compact });
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

    [McpServerTool(Name = "unpublish_page")]
    [Description("Unpublish ONE page: it leaves the site's output (its files sweep away, the sitemap and llms.txt drop it) while the DRAFT is kept, so it can be edited and published again later. Use for content that went live before it was ready.")]
    public static async Task<object> UnpublishPage(
        [Description("The page id.")] string pageId,
        ICommandDispatcher dispatcher, IConfiguration config, CancellationToken ct = default)
    {
        if (!TryPageId(pageId, out var pid)) return Fail("invalid pageId");
        return await Dispatch(dispatcher, config, new UnpublishPageCmd(pid), ct, () => new { ok = true, pageId = pid.Compact, published = false });
    }

    [McpServerTool(Name = "upsert_syndicated_page")]
    [Description("Create or replace a SYNDICATED page — one generated by another system and addressed by a NESTED path (e.g. 'dimensions/rubric-2026.08.19', 'surveys/lang/go'), rather than an authored page with a flat slug. Use this for machine-produced content a producer re-pushes on every run: it is keyed by path, content-hashed so a re-push that changes nothing reports changed:false, and the path nests up to 8 slug-shaped segments. 'node' is the page's content as a node spec — the same {type, …, children} shape the authoring API takes, with a section at the root. Publishing follows on the publisher's next pass.")]
    public static object UpsertSyndicatedPage(
        [Description("The site id.")] string siteId,
        [Description("The nested path, e.g. 'dimensions/rubric-2026.08.19'. Each segment is slug-shaped; 1-8 deep.")] string path,
        [Description("The page title.")] string title,
        [Description("The page's content as a JSON node spec, e.g. {\"type\":\"section\",\"children\":[…]}.")] string nodeJson,
        [Description("Optional SEO meta title.")] string? metaTitle,
        [Description("Optional SEO meta description.")] string? metaDescription,
        [Description("Optional locale (default: the site's default).")] string? locale,
        SiteOverview sites, SyndicatedPageStore store)
    {
        if (!TrySiteId(siteId, out var sid)) return Fail("invalid siteId");
        var site = sites.Get(sid);
        if (site is null) return Fail("unknown site");
        if (SyndicatedPath.Sanitize(path) is not { } cleanPath)
        {
            return Fail($"path segments must each be slug-shaped (a-z, 0-9, hyphen, dot), 1-{SyndicatedPath.MaxSegments} deep");
        }

        var loc = site.DefaultLocale;
        if (!string.IsNullOrWhiteSpace(locale) && !Locale.TryCreate(locale, out loc))
        {
            return Fail($"'{locale}' is not a valid locale tag");
        }

        JsonElement spec;
        try
        {
            spec = JsonDocument.Parse(nodeJson ?? "").RootElement.Clone();
        }
        catch (JsonException ex)
        {
            return Fail($"node is not valid JSON: {ex.Message}");
        }

        if (!AuthoringNodeJson.TryParse(spec, loc, out var node, out var nodeError))
        {
            return Fail(nodeError);
        }

        var t = LocalizedText.Of(loc, title ?? "");
        var mt = LocalizedText.Of(loc, metaTitle ?? "");
        var md = LocalizedText.Of(loc, metaDescription ?? "");
        var changed = store.Upsert(new SyndicatedPage(
            sid, cleanPath, t, mt, md, node,
            SyndicatedPageStore.HashOf(t, mt, md, node),
            DateTimeOffset.UtcNow));

        return new { ok = true, siteId = sid.Compact, path = cleanPath, changed };
    }

    [McpServerTool(Name = "delete_syndicated_page")]
    [Description("Withdraw a syndicated page. The publisher removes its files on the next pass — content that is no longer published must stop being served rather than linger as an orphan.")]
    public static object DeleteSyndicatedPage(
        [Description("The site id.")] string siteId,
        [Description("The nested path to withdraw.")] string path,
        SiteOverview sites, SyndicatedPageStore store)
    {
        if (!TrySiteId(siteId, out var sid)) return Fail("invalid siteId");
        if (sites.Get(sid) is null) return Fail("unknown site");
        if (SyndicatedPath.Sanitize(path) is not { } cleanPath) return Fail("invalid path");
        var removed = store.Remove(sid, cleanPath);
        return new { ok = true, siteId = sid.Compact, path = cleanPath, removed };
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
        PageLink page => new { kind = "page", pageId = page.PageId.Compact, slug = slugs.GetValueOrDefault(page.PageId), fragment = page.Fragment },
        ExternalLink external => new { kind = "external", url = external.Url },
        _ => null,
    };

    /// <summary>A site row for <c>list_sites</c>.</summary>
    public sealed record SiteInfo(string Id, string Name, string DefaultLocale);
}
