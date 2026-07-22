using System.Security.Cryptography;
using System.Text;
using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Assets;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Projections;
using Imprint.Editor.Auth;
using Imprint.EventSourcing;
using AddNodeCmd = Imprint.Authoring.Features.Pages.AddNode.AddNode;
using ChangeNodePropsCmd = Imprint.Authoring.Features.Pages.ChangeNodeProps.ChangeNodeProps;
using CreatePageCmd = Imprint.Authoring.Features.Pages.CreatePage.CreatePage;
using CreateSiteCmd = Imprint.Authoring.Features.Sites.CreateSite.CreateSite;
using PublishAllStaleCmd = Imprint.Authoring.Features.Pages.PublishAllStale.PublishAllStale;
using RemoveNodeCmd = Imprint.Authoring.Features.Pages.RemoveNode.RemoveNode;
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

        // The node tree, flattened — so a caller can find a section to insert into (and at what index).
        api.MapGet("/pages/{pageId}/tree", (string pageId, PageDrafts drafts) =>
        {
            if (!TryPageId(pageId, out var pid)) return Results.BadRequest(new { error = "invalid pageId" });
            var page = drafts.Get(pid);
            if (page is null) return Results.NotFound(new { error = "unknown page" });

            var flat = new List<object>();
            void Walk(NodeList nodes, int depth)
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
                    });
                    if (isContainer) Walk(((IContainerNode)n).Children, depth + 1);
                }
            }
            Walk(page.Tree.Roots, 0);
            return Results.Ok(new { pageId = pid.Compact, rootCount = page.Tree.Roots.Count, nodes = flat });
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

        // Replace a widget's props (same node id + tag; other node types are rejected).
        api.MapPut("/pages/{pageId}/nodes/{nodeId}/props", async (
            string pageId, string nodeId, SetPropsRequest body, ICommandDispatcher dispatcher, PageDrafts drafts, CancellationToken ct) =>
        {
            if (!TryPageId(pageId, out var pid)) return Results.BadRequest(new { error = "invalid pageId" });
            if (!NodeId.TryParse(nodeId, out var nid)) return Results.BadRequest(new { error = "invalid nodeId" });
            var page = drafts.Get(pid);
            if (page is null) return Results.NotFound(new { error = "unknown page" });
            if (page.Tree.Find(nid) is not WidgetNode widget) return Results.BadRequest(new { error = "node is not a widget" });

            var replacement = widget with { Props = ToPropBag(body?.Props) };
            var result = await DispatchAs(dispatcher, actor, new ChangeNodePropsCmd(pid, replacement), ct);
            return result.Succeeded
                ? Results.Ok(new { nodeId = nid.Compact })
                : Results.BadRequest(new { error = "update failed", details = result.Errors });
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
