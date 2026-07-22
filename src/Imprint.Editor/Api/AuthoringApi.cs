using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Assets;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Projections;
using Imprint.Editor.Auth;
using Imprint.EventSourcing;
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

namespace Imprint.Editor.Api;

/// <summary>
/// A headless, token-authenticated authoring API — the machine equivalent of the Blazor editor.
/// It exists so content can be authored WITHOUT the interactive Keycloak/Google login (e.g. from
/// CI, a script, or an MCP running off-network): the single write path is the same
/// <see cref="ICommandDispatcher"/> the editor uses, so every guard, validator and the automatic
/// publish-on-catch-up all apply unchanged.
/// </summary>
/// <remarks>
/// SECURITY. This is a full write path into the CMS (insert nodes, publish live sites), so it is
/// locked down deliberately:
/// <list type="bullet">
/// <item>FAIL CLOSED — the endpoints are NOT mapped at all unless <c>Imprint:Authoring:Token</c> is
/// configured. No token ⇒ no surface.</item>
/// <item>A dedicated bearer-token gate (<see cref="RequireAuthoringToken"/>), independent of the
/// Keycloak scheme, so it works whether or not interactive auth is enabled; constant-time compare.</item>
/// <item>Every command is stamped with a fixed service actor (<c>Imprint:Authoring:Actor</c>) via
/// <see cref="EditorActor.BeginScope"/>, so events are attributed to the machine identity — never the
/// OS user.</item>
/// </list>
/// The token IS the authorization boundary; per-site ownership is not re-checked here (mirrors how the
/// editor's own command dispatch is not access-checked at the dispatcher). Serve over TLS only.
/// </remarks>
public static class AuthoringApi
{
    /// <summary>Config key for the shared secret that gates the API. Unset ⇒ the API is disabled.</summary>
    public const string TokenKey = "Imprint:Authoring:Token";

    /// <summary>Config key for the service actor stamped on authored events.</summary>
    public const string ActorKey = "Imprint:Authoring:Actor";

    public static void MapAuthoringApi(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var token = app.Configuration[TokenKey];
        if (string.IsNullOrWhiteSpace(token))
        {
            // Fail closed: no token configured ⇒ no authoring surface exists.
            return;
        }

        var actor = app.Configuration[ActorKey];
        if (string.IsNullOrWhiteSpace(actor))
        {
            actor = "service:authoring-api";
        }

        var api = app.MapGroup("/api/authoring").AddEndpointFilter(new BearerTokenFilter(token));

        // ── reads ────────────────────────────────────────────────────────────────────────────
        api.MapGet("/sites", (SiteOverview sites) => Results.Ok(
            sites.All.Select(s => new { id = s.Id.Compact, name = s.Name, defaultLocale = s.DefaultLocale.Value })));

        api.MapGet("/sites/{siteId}/pages", (string siteId, SiteOverview sites, PageList pages) =>
        {
            if (!TrySiteId(siteId, out var sid)) return Results.BadRequest(new { error = "invalid siteId" });
            var site = sites.Get(sid);
            if (site is null) return Results.NotFound(new { error = "unknown site" });
            var loc = site.DefaultLocale;
            return Results.Ok(pages.All(sid).Select(p => new
            {
                id = p.Id.Compact,
                slug = p.Slug.Value,
                title = p.Title.Resolve(loc, loc),
                status = p.Status.ToString(),
                isHome = p.IsHome,
                inNavigation = p.IsInNavigation,
                version = p.Version,
                publishedVersion = p.PublishedVersion,
            }));
        });

        // The node tree, flattened — so a caller can find a section to insert into (and at what
        // index), AND read the content it is about to change. Every node carries its parent and its
        // type-specific props (text as locale → value), because an editing agent's first move is
        // always "show me what is there now".
        api.MapGet("/pages/{pageId}/tree", (string pageId, PageDrafts drafts, bool? content) =>
        {
            if (!TryPageId(pageId, out var pid)) return Results.BadRequest(new { error = "invalid pageId" });
            var page = drafts.Get(pid);
            if (page is null) return Results.NotFound(new { error = "unknown page" });

            var withContent = content ?? true;
            var flat = new List<object>();
            void Walk(NodeList nodes, NodeId parent, int depth)
            {
                foreach (var n in nodes)
                {
                    var isContainer = n is IContainerNode;
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
                    if (isContainer) Walk(((IContainerNode)n).Children, n.Id, depth + 1);
                }
            }
            Walk(page.Tree.Roots, NodeId.Root, 0);
            return Results.Ok(new
            {
                pageId = pid.Compact,
                slug = page.Slug.Value,
                title = page.Title.Values.ToDictionary(kv => kv.Key.Value, kv => kv.Value),
                metaTitle = page.MetaTitle.Values.ToDictionary(kv => kv.Key.Value, kv => kv.Value),
                metaDescription = page.MetaDescription.Values.ToDictionary(kv => kv.Key.Value, kv => kv.Value),
                rootCount = page.Tree.Roots.Count,
                nodes = flat,
            });
        });

        // One site's chrome — locales, navigation, footer groups, header actions and the fine-print
        // copy line. The read a caller needs before reordering navigation or rewriting the footer,
        // since both of those commands carry the whole list.
        api.MapGet("/sites/{siteId}", (string siteId, SiteOverview sites, PageList pages) =>
        {
            if (!TrySiteId(siteId, out var sid)) return Results.BadRequest(new { error = "invalid siteId" });
            var site = sites.Get(sid);
            if (site is null) return Results.NotFound(new { error = "unknown site" });
            var slugs = pages.All(sid).ToDictionary(p => p.Id, p => p.Slug.Value);
            return Results.Ok(new
            {
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
            });
        });

        // ── writes ───────────────────────────────────────────────────────────────────────────
        api.MapPost("/sites", async (CreateSiteRequest body, ICommandDispatcher dispatcher, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(body?.Name)) return Results.BadRequest(new { error = "name is required" });
            var siteId = SiteId.New();
            var result = await DispatchAs(dispatcher, actor, new CreateSiteCmd(siteId, body.Name, string.IsNullOrWhiteSpace(body.DefaultLocale) ? "en" : body.DefaultLocale), ct);
            return result.Succeeded
                ? Results.Ok(new { siteId = siteId.Compact })
                : Results.BadRequest(new { error = "create site failed", details = result.Errors });
        });

        api.MapPost("/sites/{siteId}/pages", async (string siteId, CreatePageRequest body, ICommandDispatcher dispatcher, CancellationToken ct) =>
        {
            if (!TrySiteId(siteId, out var sid)) return Results.BadRequest(new { error = "invalid siteId" });
            if (string.IsNullOrWhiteSpace(body?.Title)) return Results.BadRequest(new { error = "title is required" });
            var pageId = PageId.New();
            var result = await DispatchAs(dispatcher, actor, new CreatePageCmd(pageId, sid, body.Title, body.Slug ?? string.Empty, string.IsNullOrWhiteSpace(body.Locale) ? "en" : body.Locale), ct);
            return result.Succeeded
                ? Results.Ok(new { pageId = pageId.Compact })
                : Results.BadRequest(new { error = "create page failed", details = result.Errors });
        });

        // Insert a widget. If sectionId is given the widget goes into that section; otherwise a new
        // top-level section is created to hold it (widgets cannot live at the page root).
        api.MapPost("/pages/{pageId}/widgets", async (
            string pageId, InsertWidgetRequest body, ICommandDispatcher dispatcher, PageDrafts drafts, CancellationToken ct) =>
        {
            if (!TryPageId(pageId, out var pid)) return Results.BadRequest(new { error = "invalid pageId" });
            if (string.IsNullOrWhiteSpace(body?.Tag)) return Results.BadRequest(new { error = "tag is required" });
            var page = drafts.Get(pid);
            if (page is null) return Results.NotFound(new { error = "unknown page" });

            NodeId sectionId;
            int childIndex;
            if (!string.IsNullOrWhiteSpace(body.SectionId))
            {
                if (!NodeId.TryParse(body.SectionId, out sectionId)) return Results.BadRequest(new { error = "invalid sectionId" });
                if (page.Tree.Find(sectionId) is not SectionNode existing) return Results.BadRequest(new { error = "sectionId is not a section on this page" });
                childIndex = body.Index ?? existing.Children.Count;
            }
            else
            {
                // Create a fresh section to hold the widget (append at the end of the page by default).
                sectionId = NodeId.New();
                var rootIndex = body.Index ?? page.Tree.Roots.Count;
                var sectionResult = await DispatchAs(dispatcher, actor, new AddNodeCmd(pid, NodeId.Root, rootIndex, new SectionNode { Id = sectionId }), ct);
                if (!sectionResult.Succeeded) return Results.BadRequest(new { error = "could not create section", details = sectionResult.Errors });
                childIndex = 0;
            }

            var widgetId = NodeId.New();
            var widget = new WidgetNode { Id = widgetId, Tag = body.Tag, Props = ToPropBag(body.Props) };
            var result = await DispatchAs(dispatcher, actor, new AddNodeCmd(pid, sectionId, childIndex, widget), ct);
            return result.Succeeded
                ? Results.Ok(new { widgetId = widgetId.Compact, sectionId = sectionId.Compact })
                : Results.BadRequest(new { error = "insert failed", details = result.Errors });
        });

        // Add any node (and its whole subtree in one call) — the general form of the widget insert
        // above. parentId omitted ⇒ the page root, which only accepts sections. Every id is minted
        // server-side by AuthoringNodeJson, so an add can never collide with an existing node.
        api.MapPost("/pages/{pageId}/nodes", async (
            string pageId, AddNodeRequest body, ICommandDispatcher dispatcher, PageDrafts drafts,
            SiteOverview sites, CancellationToken ct) =>
        {
            if (!TryPageId(pageId, out var pid)) return Results.BadRequest(new { error = "invalid pageId" });
            var page = drafts.Get(pid);
            if (page is null) return Results.NotFound(new { error = "unknown page" });
            if (body is null || body.Node.ValueKind != JsonValueKind.Object) return Results.BadRequest(new { error = "a 'node' spec object is required" });

            var parentId = NodeId.Root;
            if (!string.IsNullOrWhiteSpace(body.ParentId) && !NodeId.TryParse(body.ParentId, out parentId))
            {
                return Results.BadRequest(new { error = "invalid parentId" });
            }

            var locale = LocaleFor(sites, page, body.Locale, out var localeError);
            if (localeError is not null) return Results.BadRequest(new { error = localeError });
            if (!AuthoringNodeJson.TryParse(body.Node, locale, out var spec, out var specError)) return Results.BadRequest(new { error = specError });

            var siblings = parentId.IsRoot
                ? page.Tree.Roots.Count
                : page.Tree.Find(parentId) is IContainerNode container ? container.Children.Count : 0;
            var result = await DispatchAs(dispatcher, actor, new AddNodeCmd(pid, parentId, body.Index ?? siblings, spec), ct);
            return result.Succeeded
                ? Results.Ok(new { nodeId = spec.Id.Compact, parentId = parentId.IsRoot ? null : parentId.Compact })
                : Results.BadRequest(new { error = "add failed", details = result.Errors });
        });

        // Change a node's props. Any node type: a partial patch is applied over what is there, so
        // "make this section Wide" doesn't have to restate its background and padding. A widget's
        // props stay whole-bag by contract (an absent 'props' clears them), as before.
        api.MapPut("/pages/{pageId}/nodes/{nodeId}/props", async (
            string pageId, string nodeId, JsonElement body, ICommandDispatcher dispatcher, PageDrafts drafts,
            SiteOverview sites, CancellationToken ct) =>
        {
            if (!TryPageId(pageId, out var pid)) return Results.BadRequest(new { error = "invalid pageId" });
            if (!NodeId.TryParse(nodeId, out var nid)) return Results.BadRequest(new { error = "invalid nodeId" });
            var page = drafts.Get(pid);
            if (page is null) return Results.NotFound(new { error = "unknown page" });
            if (page.Tree.Find(nid) is not { } current) return Results.BadRequest(new { error = "unknown nodeId on this page" });

            var patch = body.ValueKind == JsonValueKind.Object && body.TryGetProperty("props", out var inner) && inner.ValueKind == JsonValueKind.Object
                ? inner
                : body;
            if (patch.ValueKind != JsonValueKind.Object) return Results.BadRequest(new { error = "props must be a JSON object" });

            var locale = LocaleFor(sites, page, Text(body, "locale"), out var localeError);
            if (localeError is not null) return Results.BadRequest(new { error = localeError });
            if (!AuthoringNodeJson.TryApply(current, patch, locale, out var replacement, out var applyError)) return Results.BadRequest(new { error = applyError });

            var result = await DispatchAs(dispatcher, actor, new ChangeNodePropsCmd(pid, replacement), ct);
            return result.Succeeded
                ? Results.Ok(new { nodeId = nid.Compact })
                : Results.BadRequest(new { error = "update failed", details = result.Errors });
        });

        // Rewrite one text field on one node, in one locale. THE copy-editing endpoint: field is
        // 'text' (heading), 'html' (rich text — the canonical inline subset), 'label' (button) or
        // 'alt' (image/graphic). Locale defaults to the site's default.
        api.MapPut("/pages/{pageId}/nodes/{nodeId}/text", async (
            string pageId, string nodeId, EditTextRequest body, ICommandDispatcher dispatcher, PageDrafts drafts,
            SiteOverview sites, CancellationToken ct) =>
        {
            if (!TryPageId(pageId, out var pid)) return Results.BadRequest(new { error = "invalid pageId" });
            if (!NodeId.TryParse(nodeId, out var nid)) return Results.BadRequest(new { error = "invalid nodeId" });
            if (string.IsNullOrWhiteSpace(body?.Field)) return Results.BadRequest(new { error = "field is required (text, html, label or alt)" });
            var page = drafts.Get(pid);
            if (page is null) return Results.NotFound(new { error = "unknown page" });

            var locale = LocaleFor(sites, page, body.Locale, out var localeError);
            if (localeError is not null) return Results.BadRequest(new { error = localeError });

            var result = await DispatchAs(dispatcher, actor, new EditTextCmd(pid, nid, body.Field, locale.Value, body.Value ?? string.Empty), ct);
            return result.Succeeded
                ? Results.Ok(new { nodeId = nid.Compact, field = body.Field, locale = locale.Value })
                : Results.BadRequest(new { error = "edit failed", details = result.Errors });
        });

        // Reorder / re-parent a node. parentId omitted ⇒ the page root (sections only).
        api.MapPost("/pages/{pageId}/nodes/{nodeId}/move", async (
            string pageId, string nodeId, MoveNodeRequest body, ICommandDispatcher dispatcher, CancellationToken ct) =>
        {
            if (!TryPageId(pageId, out var pid)) return Results.BadRequest(new { error = "invalid pageId" });
            if (!NodeId.TryParse(nodeId, out var nid)) return Results.BadRequest(new { error = "invalid nodeId" });
            var parentId = NodeId.Root;
            if (!string.IsNullOrWhiteSpace(body?.ParentId) && !NodeId.TryParse(body.ParentId, out parentId))
            {
                return Results.BadRequest(new { error = "invalid parentId" });
            }

            var result = await DispatchAs(dispatcher, actor, new MoveNodeCmd(pid, nid, parentId, body?.Index ?? 0), ct);
            return result.Succeeded
                ? Results.Ok(new { nodeId = nid.Compact })
                : Results.BadRequest(new { error = "move failed", details = result.Errors });
        });

        // Copy a node and its subtree next to the original — how a card grid grows a card without
        // restating the whole spec.
        api.MapPost("/pages/{pageId}/nodes/{nodeId}/duplicate", async (
            string pageId, string nodeId, ICommandDispatcher dispatcher, CancellationToken ct) =>
        {
            if (!TryPageId(pageId, out var pid)) return Results.BadRequest(new { error = "invalid pageId" });
            if (!NodeId.TryParse(nodeId, out var nid)) return Results.BadRequest(new { error = "invalid nodeId" });
            var copyId = NodeId.New();
            var result = await DispatchAs(dispatcher, actor, new DuplicateNodeCmd(pid, nid, copyId), ct);
            return result.Succeeded
                ? Results.Ok(new { nodeId = copyId.Compact, copyOf = nid.Compact })
                : Results.BadRequest(new { error = "duplicate failed", details = result.Errors });
        });

        // The page's own title and SEO meta. Both localized; locale defaults to the site's default.
        api.MapPut("/pages/{pageId}/title", async (
            string pageId, PageTitleRequest body, ICommandDispatcher dispatcher, PageDrafts drafts, SiteOverview sites, CancellationToken ct) =>
        {
            if (!TryPageId(pageId, out var pid)) return Results.BadRequest(new { error = "invalid pageId" });
            var page = drafts.Get(pid);
            if (page is null) return Results.NotFound(new { error = "unknown page" });
            var locale = LocaleFor(sites, page, body?.Locale, out var localeError);
            if (localeError is not null) return Results.BadRequest(new { error = localeError });

            var result = await DispatchAs(dispatcher, actor, new ChangePageTitleCmd(pid, locale.Value, body?.Title ?? string.Empty), ct);
            return result.Succeeded
                ? Results.Ok(new { pageId = pid.Compact })
                : Results.BadRequest(new { error = "title change failed", details = result.Errors });
        });

        api.MapPut("/pages/{pageId}/meta", async (
            string pageId, PageMetaRequest body, ICommandDispatcher dispatcher, PageDrafts drafts, SiteOverview sites, CancellationToken ct) =>
        {
            if (!TryPageId(pageId, out var pid)) return Results.BadRequest(new { error = "invalid pageId" });
            var page = drafts.Get(pid);
            if (page is null) return Results.NotFound(new { error = "unknown page" });
            var locale = LocaleFor(sites, page, body?.Locale, out var localeError);
            if (localeError is not null) return Results.BadRequest(new { error = localeError });

            var result = await DispatchAs(dispatcher, actor, new ChangePageMetaCmd(pid, locale.Value, body?.MetaTitle, body?.MetaDescription), ct);
            return result.Succeeded
                ? Results.Ok(new { pageId = pid.Compact })
                : Results.BadRequest(new { error = "meta change failed", details = result.Errors });
        });

        api.MapDelete("/pages/{pageId}/nodes/{nodeId}", async (
            string pageId, string nodeId, ICommandDispatcher dispatcher, CancellationToken ct) =>
        {
            if (!TryPageId(pageId, out var pid)) return Results.BadRequest(new { error = "invalid pageId" });
            if (!NodeId.TryParse(nodeId, out var nid)) return Results.BadRequest(new { error = "invalid nodeId" });
            var result = await DispatchAs(dispatcher, actor, new RemoveNodeCmd(pid, nid), ct);
            return result.Succeeded
                ? Results.Ok(new { nodeId = nid.Compact })
                : Results.BadRequest(new { error = "remove failed", details = result.Errors });
        });

        // Publish every stale page in the site. The static files follow automatically (the publisher
        // re-renders on the projection catch-up the dispatch triggers).
        api.MapPost("/sites/{siteId}/publish", async (string siteId, ICommandDispatcher dispatcher, CancellationToken ct) =>
        {
            if (!TrySiteId(siteId, out var sid)) return Results.BadRequest(new { error = "invalid siteId" });
            var result = await DispatchAs(dispatcher, actor, new PublishAllStaleCmd(sid), ct);
            return result.Succeeded
                ? Results.Ok(new { siteId = sid.Compact, published = true })
                : Results.BadRequest(new { error = "publish failed", details = result.Errors });
        });

        // Publish ONE page. The precise form: publishing a whole site also ships every other page
        // that happens to be sitting stale, which on a live marketing site is not always what the
        // caller meant.
        api.MapPost("/pages/{pageId}/publish", async (string pageId, ICommandDispatcher dispatcher, CancellationToken ct) =>
        {
            if (!TryPageId(pageId, out var pid)) return Results.BadRequest(new { error = "invalid pageId" });
            var result = await DispatchAs(dispatcher, actor, new PublishPageCmd(pid), ct);
            return result.Succeeded
                ? Results.Ok(new { pageId = pid.Compact, published = true })
                : Results.BadRequest(new { error = "publish failed", details = result.Errors });
        });

        // ── site chrome ─────────────────────────────────────────────────────────────────────────
        // Navigation travels as the whole list (mirroring the command and the editor's reorder-as-a-
        // unit shape), so GET /sites/{id} first, change the order, PUT it back.
        api.MapPut("/sites/{siteId}/navigation", async (
            string siteId, JsonElement body, ICommandDispatcher dispatcher, SiteOverview sites, CancellationToken ct) =>
        {
            if (!TrySiteId(siteId, out var sid)) return Results.BadRequest(new { error = "invalid siteId" });
            var site = sites.Get(sid);
            if (site is null) return Results.NotFound(new { error = "unknown site" });
            if (!body.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
            {
                return Results.BadRequest(new { error = "an 'items' array is required" });
            }

            var locale = site.DefaultLocale;
            if (Text(body, "locale") is { Length: > 0 } raw)
            {
                if (!Locale.TryCreate(raw, out locale)) return Results.BadRequest(new { error = $"'{raw}' is not a valid locale tag" });
            }

            List<NavigationItem> parsed;
            try
            {
                parsed = [.. items.EnumerateArray().Select(item => ParseNavigationItem(item, locale))];
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }

            var result = await DispatchAs(dispatcher, actor, new ChangeNavigationCmd(sid, parsed), ct);
            return result.Succeeded
                ? Results.Ok(new { siteId = sid.Compact, items = parsed.Count })
                : Results.BadRequest(new { error = "navigation change failed", details = result.Errors });
        });

        // The footer's fine-print line, on every page of the site. Null/empty text clears it.
        api.MapPut("/sites/{siteId}/copy-line", async (
            string siteId, CopyLineRequest? body, ICommandDispatcher dispatcher, SiteOverview sites, CancellationToken ct) =>
        {
            if (!TrySiteId(siteId, out var sid)) return Results.BadRequest(new { error = "invalid siteId" });
            var site = sites.Get(sid);
            if (site is null) return Results.NotFound(new { error = "unknown site" });

            var locale = site.DefaultLocale;
            if (!string.IsNullOrWhiteSpace(body?.Locale) && !Locale.TryCreate(body.Locale, out locale))
            {
                return Results.BadRequest(new { error = $"'{body.Locale}' is not a valid locale tag" });
            }

            // Editing one locale must not drop the others, so the existing value is the base.
            var text = site.CopyLine?.Text ?? LocalizedText.Empty;
            var updated = text.With(locale, body?.Text ?? string.Empty);
            var result = await DispatchAs(dispatcher, actor, new SetCopyLineCmd(sid, updated.IsEmpty ? null : new CopyLine(updated)), ct);
            return result.Succeeded
                ? Results.Ok(new { siteId = sid.Compact, copyLine = Localized(updated) })
                : Results.BadRequest(new { error = "copy line change failed", details = result.Errors });
        });

        // ── assets ──────────────────────────────────────────────────────────────────────────────
        // Upload a file. Two accepted shapes: a multipart form-file (field "file", the primary
        // path), or a raw request body with an X-Filename header and a Content-Type. Processing
        // (variants/sanitize/transcode) runs async in AssetProcessingWorker — the id returns at
        // once; GET /assets/{id} polls the status + variant URLs until it is Ready.
        api.MapPost("/assets", async (HttpContext http, ICommandDispatcher dispatcher, CancellationToken ct) =>
        {
            string fileName;
            string contentType;
            Stream content;
            long byteSize;

            if (http.Request.HasFormContentType)
            {
                var form = await http.Request.ReadFormAsync(ct);
                var file = form.Files.GetFile("file") ?? form.Files.FirstOrDefault();
                if (file is null) return Results.BadRequest(new { error = "no file in the multipart form (expected a 'file' field)" });
                fileName = file.FileName;
                contentType = string.IsNullOrWhiteSpace(file.ContentType) || !file.ContentType.Contains('/')
                    ? "application/octet-stream" : file.ContentType;
                byteSize = file.Length;
                content = file.OpenReadStream();
            }
            else
            {
                fileName = http.Request.Headers["X-Filename"].ToString().Trim();
                if (string.IsNullOrWhiteSpace(fileName)) return Results.BadRequest(new { error = "a raw upload needs an X-Filename header" });
                contentType = string.IsNullOrWhiteSpace(http.Request.ContentType) || !http.Request.ContentType.Contains('/')
                    ? "application/octet-stream" : http.Request.ContentType;
                // Buffer to learn the length (UploadAsset needs ByteSize; the request stream is
                // not seekable). The upload cap is enforced by the aggregate.
                var buffer = new MemoryStream();
                await http.Request.Body.CopyToAsync(buffer, ct);
                buffer.Position = 0;
                byteSize = buffer.Length;
                content = buffer;
            }

            if (byteSize <= 0) return Results.BadRequest(new { error = "the uploaded file is empty" });

            var assetId = AssetId.New();
            await using (content)
            {
                var result = await DispatchAs(dispatcher, actor, new UploadAssetCmd(assetId, fileName, contentType, byteSize, content), ct);
                return result.Succeeded
                    ? Results.Ok(new { assetId = assetId.Compact, status = "Pending" })
                    : Results.BadRequest(new { error = "upload failed", details = result.Errors });
            }
        }).DisableAntiforgery();

        // Poll one asset's processing status + its resolved variant URLs.
        api.MapGet("/assets/{assetId}", (string assetId, AssetLibrary assets) =>
        {
            if (!TryAssetId(assetId, out var aid)) return Results.BadRequest(new { error = "invalid assetId" });
            var asset = assets.Get(aid);
            return asset is null ? Results.NotFound(new { error = "unknown asset" }) : Results.Ok(AssetView(asset));
        });

        // The asset library is a single shared shelf (not per-site), so this lists every asset;
        // the siteId only scopes the URL and is validated to exist. A caller discovers images to
        // use as a favicon / header logo here.
        api.MapGet("/sites/{siteId}/assets", (string siteId, SiteOverview sites, AssetLibrary assets) =>
        {
            if (!TrySiteId(siteId, out var sid)) return Results.BadRequest(new { error = "invalid siteId" });
            if (sites.Get(sid) is null) return Results.NotFound(new { error = "unknown site" });
            return Results.Ok(assets.All().Select(AssetView));
        });

        // ── brand imagery ───────────────────────────────────────────────────────────────────────
        api.MapPut("/sites/{siteId}/favicon", async (string siteId, SetAssetRefRequest? body, ICommandDispatcher dispatcher, CancellationToken ct) =>
        {
            if (!TrySiteId(siteId, out var sid)) return Results.BadRequest(new { error = "invalid siteId" });
            if (!TryOptionalAssetId(body?.AssetId, out var aid)) return Results.BadRequest(new { error = "invalid assetId" });
            var result = await DispatchAs(dispatcher, actor, new SetFaviconCmd(sid, aid), ct);
            return result.Succeeded
                ? Results.Ok(new { siteId = sid.Compact, faviconAssetId = aid?.Compact })
                : Results.BadRequest(new { error = "set favicon failed", details = result.Errors });
        });

        api.MapPut("/sites/{siteId}/header-logo", async (string siteId, SetAssetRefRequest? body, ICommandDispatcher dispatcher, CancellationToken ct) =>
        {
            if (!TrySiteId(siteId, out var sid)) return Results.BadRequest(new { error = "invalid siteId" });
            if (!TryOptionalAssetId(body?.AssetId, out var aid)) return Results.BadRequest(new { error = "invalid assetId" });
            var result = await DispatchAs(dispatcher, actor, new SetHeaderLogoCmd(sid, aid), ct);
            return result.Succeeded
                ? Results.Ok(new { siteId = sid.Compact, headerLogoAssetId = aid?.Compact })
                : Results.BadRequest(new { error = "set header logo failed", details = result.Errors });
        });
    }

    /// <summary>A caller-facing asset view: identity, processing status and resolved /media URLs.</summary>
    internal static object AssetView(Asset asset) => new
    {
        id = asset.Id.Compact,
        name = asset.Name,
        kind = asset.Kind.ToString(),
        status = asset.Status.ToString(),
        contentType = asset.ContentType,
        variants = asset.Variants.Select(v => new { url = $"/media/{v.StorageKey}", v.Width, v.Height }).ToList(),
        // A single representative URL: the largest raster variant, else the sanitized SVG, else
        // the original file.
        url = asset.Variants.Count > 0
            ? $"/media/{asset.Variants[^1].StorageKey}"
            : asset.DerivedStorageKey is { } derived
                ? $"/media/{derived}"
                : $"/media/{asset.OriginalStorageKey}",
    };

    private static async Task<Result> DispatchAs(ICommandDispatcher dispatcher, string actor, ICommand command, CancellationToken ct)
    {
        // Stamp the machine identity onto every event (synchronous push before the await, so it flows
        // into the dispatch's execution context — see EditorActor).
        using var _ = EditorActor.BeginScope(actor);
        return await dispatcher.Dispatch(command, ct);
    }

    private static PropBag ToPropBag(IDictionary<string, string>? props) =>
        props is null || props.Count == 0
            ? PropBag.Empty
            : PropBag.Of(props.Select(kv => new KeyValuePair<string, string>(kv.Key, kv.Value ?? string.Empty)));

    private static Dictionary<string, string> Localized(LocalizedText text) =>
        text.Values.ToDictionary(kv => kv.Key.Value, kv => kv.Value, StringComparer.Ordinal);

    private static object? LinkView(Link? link, IReadOnlyDictionary<PageId, string> slugs) => link switch
    {
        PageLink page => new { kind = "page", pageId = page.PageId.Compact, slug = slugs.GetValueOrDefault(page.PageId) },
        ExternalLink external => new { kind = "external", url = external.Url },
        _ => null,
    };

    /// <summary>
    /// The locale a page write lands in: the caller's if given and valid, else the owning site's
    /// default. Callers on a single-locale site should never have to name it.
    /// </summary>
    private static Locale LocaleFor(SiteOverview sites, Imprint.Authoring.Domain.Pages.Page page, string? requested, out string? error)
    {
        error = null;
        if (!string.IsNullOrWhiteSpace(requested))
        {
            if (!Locale.TryCreate(requested, out var explicitLocale))
            {
                error = $"'{requested}' is not a valid locale tag (expected e.g. 'en' or 'de-AT').";
                return default;
            }

            return explicitLocale;
        }

        var site = sites.Get(page.SiteId);
        if (site is null)
        {
            error = "the page's site is unknown";
            return default;
        }

        return site.DefaultLocale;
    }

    private static string? Text(JsonElement element, string key) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    /// <summary>
    /// One navigation entry: a direct page link (<c>pageId</c>), a direct external link
    /// (<c>url</c>), or a group (<c>children</c>). Label/description are plain strings in the
    /// request's locale — the aggregate enforces which of them are mandatory.
    /// </summary>
    internal static NavigationItem ParseNavigationItem(JsonElement item, Locale locale)
    {
        if (item.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("Each navigation item must be a JSON object.");
        }

        var label = Text(item, "label") is { Length: > 0 } text ? LocalizedText.Of(locale, text) : null;

        if (item.TryGetProperty("children", out var children) && children.ValueKind == JsonValueKind.Array && children.GetArrayLength() > 0)
        {
            return NavigationItem.Group(
                label ?? throw new ArgumentException("A navigation group needs a label."),
                [.. children.EnumerateArray().Select(child => ParseNavigationChild(child, locale))]);
        }

        return new NavigationItem { Label = label, Link = ParseNavigationLink(item) };
    }

    private static NavigationChild ParseNavigationChild(JsonElement child, Locale locale)
    {
        if (child.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("Each navigation child must be a JSON object.");
        }

        return new NavigationChild(
            Text(child, "label") is { Length: > 0 } label ? LocalizedText.Of(locale, label) : null,
            ParseNavigationLink(child) ?? throw new ArgumentException("A navigation child needs a pageId or a url."),
            Text(child, "description") is { Length: > 0 } description ? LocalizedText.Of(locale, description) : null);
    }

    private static Link? ParseNavigationLink(JsonElement element)
    {
        if (Text(element, "url") is { Length: > 0 } url)
        {
            if (!CanonicalHtml.IsAllowedHref(url))
            {
                throw new ArgumentException($"'{url}' must be an https, http or mailto address.");
            }

            return new ExternalLink(url);
        }

        if (Text(element, "pageId") is { Length: > 0 } page)
        {
            if (!Guid.TryParseExact(page, "N", out var guid) && !Guid.TryParse(page, out guid))
            {
                throw new ArgumentException($"'{page}' is not a valid page id.");
            }

            return new PageLink(PageId.From(guid));
        }

        return null;
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

    internal static bool TryAssetId(string? s, out AssetId id)
    {
        if (Guid.TryParseExact(s, "N", out var g) || Guid.TryParse(s, out g)) { id = AssetId.From(g); return true; }
        id = default;
        return false;
    }

    // Blank/null means "clear" (returns null, true); a present-but-unparseable value is an error
    // (false). Lets the favicon/logo endpoints accept an explicit null to remove the image.
    private static bool TryOptionalAssetId(string? s, out AssetId? id)
    {
        if (string.IsNullOrWhiteSpace(s)) { id = null; return true; }
        if (TryAssetId(s, out var parsed)) { id = parsed; return true; }
        id = null;
        return false;
    }

    /// <summary>Request body for creating a site.</summary>
    public sealed record CreateSiteRequest(string Name, string? DefaultLocale);

    /// <summary>Request body for creating a page.</summary>
    public sealed record CreatePageRequest(string Title, string? Slug, string? Locale);

    /// <summary>Request body for inserting a widget.</summary>
    public sealed record InsertWidgetRequest(string Tag, Dictionary<string, string>? Props, string? SectionId, int? Index);

    /// <summary>Request body for replacing a widget's props.</summary>
    public sealed record SetPropsRequest(Dictionary<string, string>? Props);

    /// <summary>Request body for adding a node: where it goes, and the spec of the node itself.</summary>
    public sealed record AddNodeRequest(string? ParentId, int? Index, JsonElement Node, string? Locale);

    /// <summary>Request body for moving a node (ParentId omitted ⇒ the page root).</summary>
    public sealed record MoveNodeRequest(string? ParentId, int? Index);

    /// <summary>Request body for rewriting one text field: text | html | label | alt.</summary>
    public sealed record EditTextRequest(string Field, string? Locale, string Value);

    /// <summary>Request body for changing a page's title.</summary>
    public sealed record PageTitleRequest(string? Locale, string Title);

    /// <summary>Request body for changing a page's SEO meta (null leaves a field as it is).</summary>
    public sealed record PageMetaRequest(string? Locale, string? MetaTitle, string? MetaDescription);

    /// <summary>Request body for the footer's fine-print copy line (empty text clears it).</summary>
    public sealed record CopyLineRequest(string? Locale, string? Text);

    /// <summary>Request body for setting a brand asset reference — null/absent clears it.</summary>
    public sealed record SetAssetRefRequest(string? AssetId);
}

/// <summary>
/// The shared bearer-token check for the headless authoring surfaces (the authoring API endpoint
/// filter and the MCP endpoint branch). Accepts <c>Authorization: Bearer &lt;token&gt;</c> or
/// <c>X-Imprint-Authoring-Token: &lt;token&gt;</c>, compared against the configured secret in
/// constant time.
/// </summary>
internal static class AuthoringToken
{
    public static string? Extract(HttpRequest request)
    {
        var auth = request.Headers.Authorization.ToString();
        if (auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return auth["Bearer ".Length..].Trim();
        }

        var header = request.Headers["X-Imprint-Authoring-Token"].ToString();
        return string.IsNullOrWhiteSpace(header) ? null : header.Trim();
    }

    public static bool Matches(HttpRequest request, string configuredToken)
    {
        var presented = Extract(request);
        return presented is not null
            && CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(presented), Encoding.UTF8.GetBytes(configuredToken));
    }
}

/// <summary>
/// The bearer-token gate for the authoring API. Accepts <c>Authorization: Bearer &lt;token&gt;</c> or
/// <c>X-Imprint-Authoring-Token: &lt;token&gt;</c>, compared against the configured secret in constant
/// time. Any mismatch or absence ⇒ 401. Independent of the Keycloak/OIDC scheme.
/// </summary>
internal sealed class BearerTokenFilter(string configuredToken) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (!AuthoringToken.Matches(context.HttpContext.Request, configuredToken))
        {
            return Results.Unauthorized();
        }

        return await next(context);
    }
}
