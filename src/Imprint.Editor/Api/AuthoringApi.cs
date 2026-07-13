using System.Security.Cryptography;
using System.Text;
using Imprint.Authoring.Domain;
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
    }

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

    /// <summary>Request body for creating a site.</summary>
    public sealed record CreateSiteRequest(string Name, string? DefaultLocale);

    /// <summary>Request body for creating a page.</summary>
    public sealed record CreatePageRequest(string Title, string? Slug, string? Locale);

    /// <summary>Request body for inserting a widget.</summary>
    public sealed record InsertWidgetRequest(string Tag, Dictionary<string, string>? Props, string? SectionId, int? Index);

    /// <summary>Request body for replacing a widget's props.</summary>
    public sealed record SetPropsRequest(Dictionary<string, string>? Props);
}

/// <summary>
/// The bearer-token gate for the authoring API. Accepts <c>Authorization: Bearer &lt;token&gt;</c> or
/// <c>X-Imprint-Authoring-Token: &lt;token&gt;</c>, compared against the configured secret in constant
/// time. Any mismatch or absence ⇒ 401. Independent of the Keycloak/OIDC scheme.
/// </summary>
internal sealed class BearerTokenFilter(string configuredToken) : IEndpointFilter
{
    private readonly byte[] _expected = Encoding.UTF8.GetBytes(configuredToken);

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var http = context.HttpContext;
        var presented = ExtractToken(http.Request);
        if (presented is null || !FixedTimeEquals(presented))
        {
            return Results.Unauthorized();
        }

        return await next(context);
    }

    private static string? ExtractToken(HttpRequest request)
    {
        var auth = request.Headers.Authorization.ToString();
        if (auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return auth["Bearer ".Length..].Trim();
        }

        var header = request.Headers["X-Imprint-Authoring-Token"].ToString();
        return string.IsNullOrWhiteSpace(header) ? null : header.Trim();
    }

    private bool FixedTimeEquals(string presented) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(presented), _expected);
}
