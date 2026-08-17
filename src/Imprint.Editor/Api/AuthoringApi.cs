using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Assets;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Projections;
using Imprint.Authoring.Syndication;
using Imprint.Editor.Auth;
using Imprint.EventSourcing;
using AddNodeCmd = Imprint.Authoring.Features.Pages.AddNode.AddNode;
using ChangeNavigationCmd = Imprint.Authoring.Features.Sites.ChangeNavigation.ChangeNavigation;
using SetFooterCmd = Imprint.Authoring.Features.Sites.SetFooter.SetFooter;
using AddLocaleCmd = Imprint.Authoring.Features.Sites.AddLocale.AddLocale;
using RemoveLocaleCmd = Imprint.Authoring.Features.Sites.RemoveLocale.RemoveLocale;
using SeedLocaleCmd = Imprint.Authoring.Features.Sites.SeedLocale.SeedLocale;
using SetHeaderActionsCmd = Imprint.Authoring.Features.Sites.SetHeaderActions.SetHeaderActions;
using ChangeNodePropsCmd = Imprint.Authoring.Features.Pages.ChangeNodeProps.ChangeNodeProps;
using ConfigureEnvironmentsCmd = Imprint.Authoring.Features.Sites.ConfigureEnvironments.ConfigureEnvironments;
using ChangePageMetaCmd = Imprint.Authoring.Features.Pages.ChangePageMeta.ChangePageMeta;
using ChangePageSlugCmd = Imprint.Authoring.Features.Pages.ChangeSlug.ChangeSlug;
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
using SetSocialImageCmd = Imprint.Authoring.Features.Sites.SetSocialImage.SetSocialImage;
using SetHeaderLogoCmd = Imprint.Authoring.Features.Sites.SetHeaderLogo.SetHeaderLogo;
using TagAssetCmd = Imprint.Authoring.Features.Assets.TagAsset.TagAsset;
using UntagAssetCmd = Imprint.Authoring.Features.Assets.UntagAsset.UntagAsset;
using UploadAssetCmd = Imprint.Authoring.Features.Assets.UploadAsset.UploadAsset;
using UploadAssetDarkVariantCmd = Imprint.Authoring.Features.Assets.UploadAssetDarkVariant.UploadAssetDarkVariant;

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
                // The read a caller needs before PUTting /environments back, and the one place the
                // site's canonical origin is visible to a machine.
                environments = site.Environments.Select(environment => (object)new
                {
                    name = environment.Name,
                    path = environment.Path,
                    baseUrl = environment.BaseUrl,
                }).ToList(),
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

        // Move a page to a different address. The domain and its handler have always supported this (slugs are
        // unique per site, and the publisher's sweep removes the old directory) — there was simply no headless way
        // to ask for it, so an off-network editor could create and retitle a page but never move one.
        api.MapPut("/pages/{pageId}/slug", async (
            string pageId, PageSlugRequest body, ICommandDispatcher dispatcher, PageDrafts drafts, CancellationToken ct) =>
        {
            if (!TryPageId(pageId, out var pid)) return Results.BadRequest(new { error = "invalid pageId" });
            if (drafts.Get(pid) is null) return Results.NotFound(new { error = "unknown page" });

            var result = await DispatchAs(dispatcher, actor, new ChangePageSlugCmd(pid, body?.Slug ?? string.Empty), ct);
            return result.Succeeded
                ? Results.Ok(new { pageId = pid.Compact, slug = body!.Slug })
                : Results.BadRequest(new { error = "slug change failed", details = result.Errors });
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

            parsed = CarryOtherLocales(parsed, site.Navigation, locale);

            var result = await DispatchAs(dispatcher, actor, new ChangeNavigationCmd(sid, parsed), ct);
            return result.Succeeded
                ? Results.Ok(new { siteId = sid.Compact, items = parsed.Count })
                : Results.BadRequest(new { error = "navigation change failed", details = result.Errors });
        });

        // The footer's named link columns. Like navigation, the whole footer travels in one call: it is small,
        // the editor rewrites it as a unit, and a partial-patch API over nested groups would be far easier to
        // corrupt than to use. Read /sites/{id} first and PUT back the shape you want.
        api.MapPut("/sites/{siteId}/footer", async (
            string siteId, JsonElement body, ICommandDispatcher dispatcher, SiteOverview sites, CancellationToken ct) =>
        {
            if (!TrySiteId(siteId, out var sid)) return Results.BadRequest(new { error = "invalid siteId" });
            var site = sites.Get(sid);
            if (site is null) return Results.NotFound(new { error = "unknown site" });
            if (!body.TryGetProperty("groups", out var groups) || groups.ValueKind != JsonValueKind.Array)
            {
                return Results.BadRequest(new { error = "a 'groups' array is required" });
            }

            var locale = site.DefaultLocale;
            if (Text(body, "locale") is { Length: > 0 } raw)
            {
                if (!Locale.TryCreate(raw, out locale)) return Results.BadRequest(new { error = $"'{raw}' is not a valid locale tag" });
            }

            List<FooterLinkGroup> parsed;
            try
            {
                parsed = [.. groups.EnumerateArray().Select(group => ParseFooterGroup(group, locale))];
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }

            parsed = CarryOtherLocales(parsed, site.FooterGroups, locale);

            var footerResult = await DispatchAs(dispatcher, actor, new SetFooterCmd(sid, parsed), ct);
            return footerResult.Succeeded
                ? Results.Ok(new { siteId = sid.Compact, groups = parsed.Count, links = parsed.Sum(g => g.Links.Count) })
                : Results.BadRequest(new { error = "footer change failed", details = footerResult.Errors });
        });

        // A second language for the site. Every text field is stored per locale, so adding one does not touch
        // existing content - it opens a slot the editor and these routes can write the translation into.
        api.MapPost("/sites/{siteId}/locales", async (
            string siteId, JsonElement body, ICommandDispatcher dispatcher, SiteOverview sites, CancellationToken ct) =>
        {
            if (!TrySiteId(siteId, out var sid)) return Results.BadRequest(new { error = "invalid siteId" });
            if (sites.Get(sid) is null) return Results.NotFound(new { error = "unknown site" });
            if (Text(body, "locale") is not { Length: > 0 } locale)
            {
                return Results.BadRequest(new { error = "a 'locale' is required, e.g. 'da'" });
            }

            var localeResult = await DispatchAs(dispatcher, actor, new AddLocaleCmd(sid, locale), ct);
            return localeResult.Succeeded
                ? Results.Ok(new { siteId = sid.Compact, locale })
                : Results.BadRequest(new { error = "add locale failed", details = localeResult.Errors });
        });

        // Pages this site serves but does not author: another system produces them and pushes them here. The body
        // carries CONTENT — a node spec, exactly what add_node takes — never markup, so the producer never has to
        // know this site's chrome, stylesheet or heading anchors. It gets them by being rendered here.
        //
        // The path may be nested (registry/github/jasperfx/marten). Every segment is validated as a slug, so a
        // pushed path cannot escape the output directory or shadow sitemap.xml — containment by alphabet rather
        // than by a traversal check somebody has to remember.
        api.MapPut("/sites/{siteId}/syndicated/{**path}", async (
            string siteId, string path, JsonElement body, SiteOverview sites, SyndicatedPageStore store, CancellationToken ct) =>
        {
            if (!TrySiteId(siteId, out var sid)) return Results.BadRequest(new { error = "invalid siteId" });
            var site = sites.Get(sid);
            if (site is null) return Results.NotFound(new { error = "unknown site" });
            if (SyndicatedPath.Sanitize(path) is not { } cleanPath)
            {
                // The depth in this message is the one SyndicatedPath actually enforces. It said 6 after the limit
                // was raised to 8 for nested namespaces, so the one caller who would ever hit it — a producer
                // publishing a deep GitLab subgroup — would have been told to shorten a path that was legal.
                return Results.BadRequest(new
                {
                    error = $"path segments must each be slug-shaped (a-z, 0-9, hyphen), 1–{SyndicatedPath.MaxSegments} deep",
                });
            }

            if (!body.TryGetProperty("node", out var nodeSpec))
            {
                return Results.BadRequest(new { error = "a 'node' object is required — the page's content" });
            }

            var locale = site.DefaultLocale;
            if (Text(body, "locale") is { Length: > 0 } rawLocale)
            {
                if (!Locale.TryCreate(rawLocale, out locale)) return Results.BadRequest(new { error = $"'{rawLocale}' is not a valid locale tag" });
            }

            if (!AuthoringNodeJson.TryParse(nodeSpec, locale, out var node, out var nodeError))
            {
                return Results.BadRequest(new { error = nodeError });
            }

            var title = LocalizedText.Of(locale, Text(body, "title") ?? "");
            var metaTitle = LocalizedText.Of(locale, Text(body, "metaTitle") ?? "");
            var metaDescription = LocalizedText.Of(locale, Text(body, "metaDescription") ?? "");

            var changed = store.Upsert(new SyndicatedPage(
                sid, cleanPath, title, metaTitle, metaDescription, node,
                SyndicatedPageStore.HashOf(title, metaTitle, metaDescription, node),
                DateTimeOffset.UtcNow));

            // "changed" is the useful half of the answer: the producer re-pushes everything it owns on every run,
            // and knowing which pushes were no-ops is how it can report real work instead of traffic.
            return Results.Ok(new { siteId = sid.Compact, path = cleanPath, changed });
        });

        // Withdraw one. The publisher's sweep removes the files on the next pass — a survey that is no longer
        // published must stop being served, not linger as an orphan outliving the thing it described.
        api.MapDelete("/sites/{siteId}/syndicated/{**path}", (
            string siteId, string path, SiteOverview sites, SyndicatedPageStore store) =>
        {
            if (!TrySiteId(siteId, out var sid)) return Results.BadRequest(new { error = "invalid siteId" });
            if (sites.Get(sid) is null) return Results.NotFound(new { error = "unknown site" });
            if (SyndicatedPath.Sanitize(path) is not { } cleanPath) return Results.BadRequest(new { error = "invalid path" });

            return Results.Ok(new { siteId = sid.Compact, path = cleanPath, removed = store.Remove(sid, cleanPath) });
        });

        // What this site currently serves on the producer's behalf, so it can reconcile: anything listed here and
        // no longer in its own set is something it should withdraw.
        api.MapGet("/sites/{siteId}/syndicated", (string siteId, SiteOverview sites, SyndicatedPageStore store) =>
        {
            if (!TrySiteId(siteId, out var sid)) return Results.BadRequest(new { error = "invalid siteId" });
            if (sites.Get(sid) is null) return Results.NotFound(new { error = "unknown site" });

            return Results.Ok(new
            {
                siteId = sid.Compact,
                pages = store.AllForSite(sid)
                    .Select(p => new { path = p.Path, contentHash = p.ContentHash, updatedAt = p.UpdatedAt })
                    .ToList(),
            });
        });

        // Stop publishing a language. The counterpart of POST /locales, and the reason it exists: a locale added
        // and never translated renders the DEFAULT language under its own path, so it looks finished while being
        // empty. Removing it is not destructive - translations stay in the page streams and return if the locale
        // is re-added, which is what makes this the safe move when a language is not ready yet.
        api.MapDelete("/sites/{siteId}/locales/{locale}", async (
            string siteId, string locale, ICommandDispatcher dispatcher, SiteOverview sites, CancellationToken ct) =>
        {
            if (!TrySiteId(siteId, out var sid)) return Results.BadRequest(new { error = "invalid siteId" });
            if (sites.Get(sid) is null) return Results.NotFound(new { error = "unknown site" });

            var removeResult = await DispatchAs(dispatcher, actor, new RemoveLocaleCmd(sid, locale), ct);
            return removeResult.Succeeded
                ? Results.Ok(new { siteId = sid.Compact, removed = locale })
                : Results.BadRequest(new { error = "remove locale failed", details = removeResult.Errors });
        });

        // Fill a locale's empty text from another one, across the chrome and every page. Run it straight after
        // adding a locale: text resolves through the default locale, so an unseeded locale renders the default
        // language and looks finished while being a copy. This makes the copy real, and translating an edit.
        // Gaps only, so it is safe to re-run - work already translated is never overwritten by its original.
        api.MapPost("/sites/{siteId}/locales/{locale}/seed-from/{source}", async (
            string siteId, string locale, string source,
            ICommandDispatcher dispatcher, SiteOverview sites, CancellationToken ct) =>
        {
            if (!TrySiteId(siteId, out var sid)) return Results.BadRequest(new { error = "invalid siteId" });
            if (sites.Get(sid) is null) return Results.NotFound(new { error = "unknown site" });

            var seedResult = await DispatchAs(dispatcher, actor, new SeedLocaleCmd(sid, locale, source), ct);
            return seedResult.Succeeded
                ? Results.Ok(new { siteId = sid.Compact, seeded = locale, from = source })
                : Results.BadRequest(new { error = "seed locale failed", details = seedResult.Errors });
        });

        // The header's primary CTA and its quiet link. They share a slot and are set together, so omitting one
        // CLEARS it - which is the only way to remove a header action that points somewhere that no longer exists.
        api.MapPut("/sites/{siteId}/header-actions", async (
            string siteId, JsonElement body, ICommandDispatcher dispatcher, SiteOverview sites, CancellationToken ct) =>
        {
            if (!TrySiteId(siteId, out var sid)) return Results.BadRequest(new { error = "invalid siteId" });
            var site = sites.Get(sid);
            if (site is null) return Results.NotFound(new { error = "unknown site" });

            var locale = site.DefaultLocale;
            if (Text(body, "locale") is { Length: > 0 } raw)
            {
                if (!Locale.TryCreate(raw, out locale)) return Results.BadRequest(new { error = $"'{raw}' is not a valid locale tag" });
            }

            HeaderAction? cta, quiet;
            try
            {
                cta = ParseHeaderAction(body, "cta", locale);
                quiet = ParseHeaderAction(body, "quiet", locale);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }

            var headerResult = await DispatchAs(dispatcher, actor, new SetHeaderActionsCmd(sid, cta, quiet), ct);
            return headerResult.Succeeded
                ? Results.Ok(new { siteId = sid.Compact, cta = cta is not null, quiet = quiet is not null })
                : Results.BadRequest(new { error = "header actions change failed", details = headerResult.Errors });
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

        // The site's ordered deploy environments. Like navigation and the footer, the whole list
        // travels in one call — the settings gear edits them as a unit and the aggregate validates
        // them as a set (unique names, folder shape). Read /sites/{id} first and PUT back the shape
        // you want.
        //
        // This exists because an environment's BaseUrl is the site's canonical origin — what every
        // canonical link, og:url, hreflang and sitemap entry names — so MOVING A SITE TO A NEW
        // DOMAIN is a BaseUrl change plus a republish. Without this endpoint that move was reachable
        // only by hand in the interactive editor, which is exactly the surface an off-network or
        // scripted operator does not have.
        api.MapPut("/sites/{siteId}/environments", async (
            string siteId, JsonElement body, ICommandDispatcher dispatcher, SiteOverview sites, CancellationToken ct) =>
        {
            if (!TrySiteId(siteId, out var sid)) return Results.BadRequest(new { error = "invalid siteId" });
            if (sites.Get(sid) is null) return Results.NotFound(new { error = "unknown site" });
            if (!body.TryGetProperty("environments", out var environments) || environments.ValueKind != JsonValueKind.Array)
            {
                return Results.BadRequest(new { error = "an 'environments' array is required" });
            }

            if (!TryParseEnvironments(environments, out var parsed, out var environmentsError))
            {
                return Results.BadRequest(new { error = environmentsError });
            }

            var result = await DispatchAs(dispatcher, actor, new ConfigureEnvironmentsCmd(sid, parsed), ct);
            return result.Succeeded
                ? Results.Ok(new { siteId = sid.Compact, environments = parsed.Count })
                : Results.BadRequest(new { error = "environments change failed", details = result.Errors });
        });

        // ── assets ──────────────────────────────────────────────────────────────────────────────
        // Upload a file. Two accepted shapes: a multipart form-file (field "file", the primary
        // path), or a raw request body with an X-Filename header and a Content-Type. Processing
        // (variants/sanitize/transcode) runs async in AssetProcessingWorker — the id returns at
        // once; GET /assets/{id} polls the status + variant URLs until it is Ready.
        api.MapPost("/assets", async (HttpContext http, ICommandDispatcher dispatcher, CancellationToken ct) =>
        {
            var (upload, error) = await ReadUpload(http, ct);
            if (upload is null) return error!;

            var assetId = AssetId.New();
            await using (upload.Content)
            {
                var result = await DispatchAs(dispatcher, actor, new UploadAssetCmd(assetId, upload.FileName, upload.ContentType, upload.ByteSize, upload.Content), ct);
                return result.Succeeded
                    ? Results.Ok(new { assetId = assetId.Compact, status = "Pending" })
                    : Results.BadRequest(new { error = "upload failed", details = result.Errors });
            }
        }).DisableAntiforgery();

        // Attach a dark-mode rendition to an EXISTING asset — the headless twin of the editor's
        // AssetDarkVariant panel. Without this a headless caller can only ever upload a light
        // asset, and SvgView then inlines that single rendition into both colour schemes: a
        // diagram authored with a light background rect ships onto a dark page. Same two body
        // shapes as POST /assets; processing is async exactly the same way, so poll
        // GET /assets/{id} until Ready. Re-uploading supersedes the previous dark rendition.
        api.MapPost("/assets/{assetId}/dark", async (string assetId, HttpContext http, ICommandDispatcher dispatcher, AssetLibrary assets, CancellationToken ct) =>
        {
            if (!TryAssetId(assetId, out var aid)) return Results.BadRequest(new { error = "invalid assetId" });
            // Checked before the bytes are read so a typo'd id fails fast rather than after an upload.
            if (assets.Get(aid) is null) return Results.NotFound(new { error = "unknown asset" });

            var (upload, error) = await ReadUpload(http, ct);
            if (upload is null) return error!;

            await using (upload.Content)
            {
                var result = await DispatchAs(dispatcher, actor, new UploadAssetDarkVariantCmd(aid, upload.FileName, upload.ContentType, upload.ByteSize, upload.Content), ct);
                return result.Succeeded
                    ? Results.Ok(new { assetId = aid.Compact, status = "Pending", dark = true })
                    : Results.BadRequest(new { error = "dark-variant upload failed", details = result.Errors });
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

        // Filing, in bulk. The shelf is one shared library, so "which post is this figure from?"
        // is a question only tags answer — and a caller putting a post's figures in order would
        // otherwise make one round trip per (file, tag) pair. Both directions take a set of assets
        // and a set of tags and apply the cross product; each is idempotent in the aggregate, so
        // re-running a script is not an error.
        api.MapPost("/assets/tags/add", (AssetTagsRequest? body, ICommandDispatcher dispatcher, AssetLibrary assets, CancellationToken ct) =>
            ApplyTags(body, add: true, dispatcher, assets, actor, ct));

        api.MapPost("/assets/tags/remove", (AssetTagsRequest? body, ICommandDispatcher dispatcher, AssetLibrary assets, CancellationToken ct) =>
            ApplyTags(body, add: false, dispatcher, assets, actor, ct));

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

        api.MapPut("/sites/{siteId}/social-image", async (string siteId, SetAssetRefRequest? body, ICommandDispatcher dispatcher, CancellationToken ct) =>
        {
            if (!TrySiteId(siteId, out var sid)) return Results.BadRequest(new { error = "invalid siteId" });
            if (!TryOptionalAssetId(body?.AssetId, out var aid)) return Results.BadRequest(new { error = "invalid assetId" });
            var result = await DispatchAs(dispatcher, actor, new SetSocialImageCmd(sid, aid), ct);
            return result.Succeeded
                ? Results.Ok(new { siteId = sid.Compact, socialImageAssetId = aid?.Compact })
                : Results.BadRequest(new { error = "set social image failed", details = result.Errors });
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

    /// <summary>
    /// Applies (or removes) every tag on every named asset. Shared by the two endpoints and the
    /// MCP tools. Unknown ids and blank tags are rejected up front rather than half-applied — a
    /// caller fixing a typo should not have to work out which half of its batch already landed.
    /// </summary>
    internal static async Task<IResult> ApplyTags(
        AssetTagsRequest? body, bool add, ICommandDispatcher dispatcher, AssetLibrary assets, string actor, CancellationToken ct)
    {
        var (ids, tags, error) = ReadTagBatch(body?.AssetIds, body?.Tags, assets);
        if (error is not null) return Results.BadRequest(new { error });

        var failures = new List<string>();
        foreach (var id in ids)
        {
            foreach (var tag in tags)
            {
                ICommand command = add ? new TagAssetCmd(id, tag) : new UntagAssetCmd(id, tag);
                var result = await DispatchAs(dispatcher, actor, command, ct);
                if (!result.Succeeded)
                {
                    failures.AddRange(result.Errors.Select(message => $"{id.Compact} / '{tag}': {message}"));
                }
            }
        }

        return failures.Count > 0
            ? Results.BadRequest(new { error = add ? "tagging failed" : "untagging failed", details = failures })
            : Results.Ok(new
            {
                assets = ids.Select(id => AssetView(assets.Get(id)!)).ToList(),
                tags,
            });
    }

    /// <summary>Validates a tag batch: every asset must exist, and at least one non-blank tag.</summary>
    internal static (IReadOnlyList<AssetId> Ids, IReadOnlyList<string> Tags, string? Error) ReadTagBatch(
        IEnumerable<string>? assetIds, IEnumerable<string>? tags, AssetLibrary assets)
    {
        var ids = new List<AssetId>();
        foreach (var candidate in assetIds ?? [])
        {
            if (!TryAssetId(candidate, out var aid))
            {
                return ([], [], $"invalid assetId '{candidate}'");
            }

            if (assets.Get(aid) is null)
            {
                return ([], [], $"unknown asset '{candidate}'");
            }

            ids.Add(aid);
        }

        if (ids.Count == 0)
        {
            return ([], [], "assetIds is required");
        }

        var names = (tags ?? [])
            .Select(AssetTag.Normalize)
            .Where(tag => tag.Length > 0)
            .Distinct(AssetTag.Comparer)
            .ToList();

        return names.Count == 0 ? ([], [], "tags is required") : (ids, names, null);
    }

    /// <summary>A caller-facing asset view: identity, processing status and resolved /media URLs.</summary>
    internal static object AssetView(Asset asset) => new
    {
        id = asset.Id.Compact,
        name = asset.Name,
        kind = asset.Kind.ToString(),
        status = asset.Status.ToString(),
        contentType = asset.ContentType,
        // The filing labels. Present on every asset view so a caller can see what a shelf is
        // already organised by before adding to it.
        tags = asset.Tags,
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
        PageLink page => new { kind = "page", pageId = page.PageId.Compact, slug = slugs.GetValueOrDefault(page.PageId), fragment = page.Fragment },
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
    /// <summary>One header action, or null when the property is absent or explicitly null - which is how an
    /// action is CLEARED. Uses the same link parser and href allow-list as navigation and the footer.</summary>
    internal static HeaderAction? ParseHeaderAction(JsonElement body, string property, Locale locale)
    {
        if (!body.TryGetProperty(property, out var element) || element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException($"'{property}' must be a JSON object, or null to clear it.");
        }

        var label = Text(element, "label") is { Length: > 0 } text
            ? LocalizedText.Of(locale, text)
            : throw new ArgumentException($"A header '{property}' action needs a label.");

        return new HeaderAction(label, ParseNavigationLink(element)
            ?? throw new ArgumentException($"A header '{property}' action needs a pageId or a url."));
    }

    /// <summary>One footer column: a heading plus its links. Reuses the navigation link parser, so a footer
    /// link accepts exactly the same <c>pageId</c> / <c>url</c> forms and the same href allow-list.</summary>
    internal static FooterLinkGroup ParseFooterGroup(JsonElement group, Locale locale)
    {
        if (group.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("Each footer group must be a JSON object.");
        }

        var heading = Text(group, "heading") is { Length: > 0 } text
            ? LocalizedText.Of(locale, text)
            : throw new ArgumentException("A footer group needs a heading.");

        if (!group.TryGetProperty("links", out var links) || links.ValueKind != JsonValueKind.Array)
        {
            throw new ArgumentException($"Footer group '{Text(group, "heading")}' needs a 'links' array.");
        }

        return new FooterLinkGroup(heading, [.. links.EnumerateArray().Select(link => ParseFooterLink(link, locale))]);
    }

    private static FooterLink ParseFooterLink(JsonElement link, Locale locale)
    {
        if (link.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("Each footer link must be a JSON object.");
        }

        return new FooterLink(
            Text(link, "label") is { Length: > 0 } label ? LocalizedText.Of(locale, label) : null,
            ParseNavigationLink(link) ?? throw new ArgumentException("A footer link needs a pageId or a url."));
    }

    /// <summary>
    /// Carry every OTHER locale's chrome labels over from what the site has now, matching
    /// by link. Callers send labels for one locale at a time, so without this, translating
    /// the English navigation would silently delete the Danish one.
    /// </summary>
    /// <remarks>
    /// Matching is by link identity, not by position: a link keeps its translations when it
    /// is reordered, renamed or moved between footer columns, and loses them only when the
    /// link itself changes — which is a different destination, so old labels would be wrong
    /// anyway. An item the site does not have yet is new, and correctly carries only the
    /// locale it arrived in.
    /// <para>
    /// The incoming locale always wins for its own value; this only ever fills in locales
    /// the caller did not speak for.
    /// </para>
    /// </remarks>
    internal static List<NavigationItem> CarryOtherLocales(
        List<NavigationItem> incoming, IReadOnlyList<NavigationItem> existing, Locale locale)
    {
        var byLink = new Dictionary<Link, NavigationItem>();
        var childrenByLink = new Dictionary<Link, NavigationChild>();
        foreach (var item in existing)
        {
            if (item.Link is { } link)
            {
                byLink[link] = item;
            }

            foreach (var child in item.Children)
            {
                childrenByLink[child.Link] = child;
            }
        }

        // A group heading has no link to match on, so — like a footer column's heading — it
        // matches by position. Without it, translating the English menu deletes the Danish
        // name of every dropdown, which is invisible while the two languages spell it the
        // same ("Onboarding") and silent data loss the moment they do not.
        LocalizedText? PreviousLabel(NavigationItem item, int index) =>
            item.Link is { } link
                ? byLink.TryGetValue(link, out var was) ? was.Label : null
                : existing.ElementAtOrDefault(index) is { IsGroup: true } sameSlot ? sameSlot.Label : null;

        return [.. incoming.Select((item, index) => item with
        {
            Label = Merged(item.Label, PreviousLabel(item, index), locale),
            Children = [.. item.Children.Select(child =>
            {
                var previous = childrenByLink.GetValueOrDefault(child.Link);
                return child with
                {
                    Label = Merged(child.Label, previous?.Label, locale),
                    Description = Merged(child.Description, previous?.Description, locale),
                };
            })],
        })];
    }

    /// <summary>
    /// Reads the wire shape of the deploy-environment list. Name and folder uniqueness are the
    /// aggregate's business — this only refuses what would reach it malformed, and normalises the
    /// one field with a shape the domain does not constrain: <c>baseUrl</c> is an ORIGIN, so a
    /// relative value is refused and a trailing slash is trimmed rather than left to double up
    /// against the paths the publisher concatenates onto it.
    /// </summary>
    internal static bool TryParseEnvironments(
        JsonElement environments, out List<DeployEnvironment> parsed, out string? error)
    {
        parsed = [];
        foreach (var environment in environments.EnumerateArray())
        {
            if (environment.ValueKind != JsonValueKind.Object)
            {
                error = "each environment must be a JSON object";
                return false;
            }

            if (Text(environment, "name") is not { Length: > 0 } name)
            {
                error = "each environment needs a 'name'";
                return false;
            }

            if (Text(environment, "path") is not { Length: > 0 } path)
            {
                error = $"environment '{name}' needs a 'path'";
                return false;
            }

            var baseUrl = Text(environment, "baseUrl");
            if (baseUrl is { Length: > 0 })
            {
                if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var origin)
                    || (origin.Scheme != Uri.UriSchemeHttps && origin.Scheme != Uri.UriSchemeHttp))
                {
                    error = $"'{baseUrl}' is not an absolute http(s) origin";
                    return false;
                }

                baseUrl = baseUrl.TrimEnd('/');
            }

            parsed.Add(new DeployEnvironment(name, path, string.IsNullOrWhiteSpace(baseUrl) ? null : baseUrl));
        }

        error = null;
        return true;
    }

    /// <inheritdoc cref="CarryOtherLocales(List{NavigationItem}, IReadOnlyList{NavigationItem}, Locale)"/>
    internal static List<FooterLinkGroup> CarryOtherLocales(
        List<FooterLinkGroup> incoming, IReadOnlyList<FooterLinkGroup> existing, Locale locale)
    {
        // Links match by link across ALL old columns, so moving one between columns keeps its
        // translations. A heading has no link to match on, so it matches by position — the
        // order the caller just supplied is the only identity a column has.
        var linksByLink = existing
            .SelectMany(group => group.Links)
            .GroupBy(link => link.Link)
            .ToDictionary(g => g.Key, g => g.First());

        return [.. incoming.Select((group, index) => new FooterLinkGroup(
            Merged(group.Heading, index < existing.Count ? existing[index].Heading : null, locale) ?? group.Heading,
            [.. group.Links.Select(link => link with
            {
                Label = Merged(link.Label, linksByLink.GetValueOrDefault(link.Link)?.Label, locale),
            })]))];
    }

    /// <summary>The incoming value for its own locale, over everything the previous value held elsewhere.</summary>
    private static LocalizedText? Merged(LocalizedText? incoming, LocalizedText? previous, Locale locale)
    {
        if (previous is null)
        {
            return incoming;
        }

        var carried = previous.With(locale, string.Empty); // drop the stale value for this locale
        foreach (var (otherLocale, value) in incoming?.Values ?? [])
        {
            carried = carried.With(otherLocale, value);
        }

        return carried.IsEmpty ? null : carried;
    }

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
                throw new ArgumentException(
                    $"'{url}' must be an https, http or mailto address, or a #section of this site's page.");
            }

            return new ExternalLink(url);
        }

        if (Text(element, "pageId") is { Length: > 0 } page)
        {
            if (!Guid.TryParseExact(page, "N", out var guid) && !Guid.TryParse(page, out guid))
            {
                throw new ArgumentException($"'{page}' is not a valid page id.");
            }

            return new PageLink(PageId.From(guid), Text(element, "fragment"));
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

    /// <summary>One uploaded file, however it arrived on the request.</summary>
    internal sealed record UploadBody(string FileName, string ContentType, long ByteSize, Stream Content);

    /// <summary>
    /// Reads an upload in either accepted shape — a multipart form-file (field "file", the
    /// primary path) or a raw body with an X-Filename header — so the base and dark-variant
    /// upload routes cannot diverge on how they parse a request. Returns the body, or a null
    /// body plus the <see cref="IResult"/> to return.
    /// </summary>
    internal static async Task<(UploadBody? Body, IResult? Error)> ReadUpload(HttpContext http, CancellationToken ct)
    {
        string fileName;
        string contentType;
        Stream content;
        long byteSize;

        if (http.Request.HasFormContentType)
        {
            var form = await http.Request.ReadFormAsync(ct);
            var file = form.Files.GetFile("file") ?? form.Files.FirstOrDefault();
            if (file is null) return (null, Results.BadRequest(new { error = "no file in the multipart form (expected a 'file' field)" }));
            fileName = file.FileName;
            contentType = string.IsNullOrWhiteSpace(file.ContentType) || !file.ContentType.Contains('/')
                ? "application/octet-stream" : file.ContentType;
            byteSize = file.Length;
            content = file.OpenReadStream();
        }
        else
        {
            fileName = http.Request.Headers["X-Filename"].ToString().Trim();
            if (string.IsNullOrWhiteSpace(fileName)) return (null, Results.BadRequest(new { error = "a raw upload needs an X-Filename header" }));
            contentType = string.IsNullOrWhiteSpace(http.Request.ContentType) || !http.Request.ContentType.Contains('/')
                ? "application/octet-stream" : http.Request.ContentType;
            // Buffer to learn the length (the upload commands need ByteSize; the request stream
            // is not seekable). The upload cap is enforced by the aggregate.
            var buffer = new MemoryStream();
            await http.Request.Body.CopyToAsync(buffer, ct);
            buffer.Position = 0;
            byteSize = buffer.Length;
            content = buffer;
        }

        if (byteSize <= 0)
        {
            await content.DisposeAsync();
            return (null, Results.BadRequest(new { error = "the uploaded file is empty" }));
        }

        return (new UploadBody(fileName, contentType, byteSize, content), null);
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

    /// <summary>The new address for a page. A slug is not localized — an address written down once has to resolve
    /// wherever it is followed.</summary>
    public sealed record PageSlugRequest(string Slug);

    /// <summary>Request body for changing a page's SEO meta (null leaves a field as it is).</summary>
    public sealed record PageMetaRequest(string? Locale, string? MetaTitle, string? MetaDescription);

    /// <summary>Request body for the footer's fine-print copy line (empty text clears it).</summary>
    public sealed record CopyLineRequest(string? Locale, string? Text);

    /// <summary>Request body for setting a brand asset reference — null/absent clears it.</summary>
    public sealed record SetAssetRefRequest(string? AssetId);

    /// <summary>Request body for tagging or untagging a batch: every tag is applied to every asset.</summary>
    public sealed record AssetTagsRequest(string[]? AssetIds, string[]? Tags);
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
