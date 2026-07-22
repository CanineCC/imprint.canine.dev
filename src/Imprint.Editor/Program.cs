using System.Text.Json;
using System.Threading.RateLimiting;
using Imprint.Authoring;
using Imprint.Authoring.Features.Assets;
using Imprint.Authoring.Features.Pages;
using Imprint.Authoring.Projections;
using Imprint.Editor.Api;
using Imprint.Editor.Auth;
using Imprint.Editor.Components;
using Imprint.Editor.Contact;
using Imprint.Editor.Mcp;
using Imprint.Editor.Services;
using Imprint.EventSourcing;
using Imprint.Media;
using Imprint.Publishing;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Optional overlay for deploy-injected secrets (the authoring/MCP bearer token) so they can be set
// from CI (a GitHub secret written into the published artifact) rather than only on-box — no LAN step.
// Optional + last so it wins over the base appsettings but an on-box env var still overrides it.
// Pinned to the binary's directory (AppContext.BaseDirectory) so it resolves regardless of the process
// working directory the host launches us with (the blue/green cutover starts us from elsewhere).
builder.Configuration.AddJsonFile(
    Path.Combine(AppContext.BaseDirectory, "appsettings.Authoring.json"), optional: true, reloadOnChange: false);

// The editor is habitually launched with `dotnet run` (any environment): load the
// static-web-assets manifest explicitly so framework/RCL assets resolve outside
// Development too. On published output (assets on disk) this is a silent no-op.
builder.WebHost.UseStaticWebAssets();

// All editor state lives under one data directory: the event store (truth), media
// files (bytes) and the published output (a projection). Point ImprintData somewhere
// else to host multiple installations side by side.
var dataDirectory = Path.GetFullPath(builder.Configuration["ImprintData"] ?? "data", builder.Environment.ContentRootPath);
Directory.CreateDirectory(dataDirectory);
var widgetsDirectory = ResolveWidgetsDirectory(builder);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddImprintAuthoring($"Data Source={Path.Combine(dataDirectory, "imprint.db")}");
builder.Services.AddImprintMedia(new MediaOptions
{
    RootPath = Path.Combine(dataDirectory, "media"),
    FfmpegPath = builder.Configuration["Ffmpeg"] ?? "ffmpeg",
});
builder.Services.AddImprintAssetProcessing();
builder.Services.AddImprintPublishing(new PublishingOptions
{
    OutputPath = Path.Combine(dataDirectory, "publish"),
    WidgetsDirectory = widgetsDirectory,
    BaseUrl = builder.Configuration["ImprintBaseUrl"],
});

// The merged catalog is built-in widgets ∪ approved submissions, so it needs the
// WidgetRegistry read model (auto-registered by AddImprintAuthoring's projection scan).
// A factory (not a pre-built instance) because the registry only exists post-Build.
builder.Services.AddSingleton(provider =>
    new EditorWidgetCatalog(widgetsDirectory, provider.GetRequiredService<WidgetRegistry>()));
builder.Services.AddSingleton<IWidgetCatalog>(provider => provider.GetRequiredService<EditorWidgetCatalog>());
builder.Services.AddSingleton<EditorRenderContextFactory>();

// The public preview plane: renders each site to its own preview folder on demand and
// serves the full published-style page (chrome + marketing CSS + hydrated live islands)
// at /preview/{site}, re-homing the origin-relative assets under that prefix.
var previewRoot = Path.Combine(dataDirectory, "preview");
builder.Services.AddSingleton(provider => new SitePreview(
    provider.GetRequiredService<SitePublisher>(),
    provider.GetRequiredService<Imprint.Authoring.Projections.SiteOverview>(),
    provider.GetRequiredService<Imprint.EventSourcing.ProjectionEngine>(),
    previewRoot,
    provider.GetRequiredService<ILoggerFactory>().CreateLogger<SitePreview>()));

// Per-circuit (per browser tab) editor state and its write path.
builder.Services.AddScoped<EditorSession>();
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<CommandRunner>();
builder.Services.AddScoped<CanvasBridge>();

// The public contact intake (/api/contact): this app's one anonymous write surface.
// CORS pins it to the published marketing origins and a small per-IP window blunts
// drive-by spam. Neither registration defines a default policy / global limiter, so
// nothing else in the pipeline changes — only the contact endpoint opts in below.
builder.Services.AddCors(cors => cors.AddPolicy("contact", policy => policy
    .WithOrigins(
        "https://canine.dev", "https://www.canine.dev",
        "https://watchdog.canine.dev", "https://assay.canine.dev", "https://cai.canine.dev")
    .WithMethods("POST")
    .WithHeaders("Content-Type")));
builder.Services.AddRateLimiter(limiter =>
{
    limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    limiter.AddPolicy("contact", http => RateLimitPartition.GetFixedWindowLimiter(
        http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1) }));
});
builder.Services.AddSingleton(provider => new ContactIntake(
    provider.GetRequiredService<IConfiguration>(),
    dataDirectory,
    provider.GetRequiredService<ILogger<ContactIntake>>(),
    // Recipients resolve per submission: the submitting site's private contact-form
    // widget prop (live from the read models — an editor change needs no republish),
    // then Contact:Recipients config, then journal-only. See ContactRecipientResolver.
    new ContactRecipientResolver(
        builder.Configuration,
        new SiteContactRecipients(
            provider.GetRequiredService<SiteOverview>(),
            provider.GetRequiredService<PageDrafts>()).Find)));

// The headless authoring MCP server (an AI agent drives the same command path as the editor).
// Registration is unconditional; the /mcp endpoint is mounted below only when the authoring
// token is configured (fail-closed).
builder.Services.AddImprintAuthoringMcp();

// Optional in-app Keycloak/OIDC protection. Off until Keycloak:Authority is configured;
// refuses to run unauthenticated in Production (see ImprintAuthExtensions).
var authOptions = builder.AddImprintEditorAuth();

var app = builder.Build();

// Stamp every appended event with the signed-in editor's email so sites are owned by, and
// history is attributed to, the real user. Falls back to the OS user when auth is off. Set
// before any command can run. See EditorActor for how a circuit's identity reaches this
// process-wide delegate.
app.Services.GetRequiredService<EventMetadataProvider>().ActorSource =
    () => EditorActor.Current ?? Environment.UserName;

// CORS + rate limiting exist ONLY for /api/contact (RequireCors/RequireRateLimiting on
// that endpoint); with no default policy and no global limiter these middlewares are
// no-ops for every other request. CORS runs before authentication so a cross-origin
// preflight is answered without ever touching the auth stack.
app.UseCors();
app.UseRateLimiter();

// TLS terminates on the reverse proxy; when auth is enabled the app must see the forwarded
// scheme/host to build correct redirect URIs, and enforce login before anything else runs.
if (authOptions.Enabled)
{
    app.UseForwardedHeaders();
    app.UseAuthentication();
    app.UseAuthorization();
}

// MapStaticAssets (not UseStaticFiles): it serves the framework script and the
// Razor-class-library assets (_content/…) from the build manifest in every
// environment — plain UseStaticFiles only composes those providers in Development.
app.MapStaticAssets();
app.UseAntiforgery();

// /healthz (always) and, when auth is enabled, the login/logout endpoints.
app.MapImprintAuthEndpoints(authOptions);

// The editor requires a signed-in user whenever Keycloak is configured; the challenge
// redirects an anonymous visitor to Keycloak. Anonymous otherwise (dev / trusted-LAN).
var editor = app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
if (authOptions.Enabled)
{
    editor.RequireAuthorization();
}

// Canvas media: serves originals and derivatives to the editor UI. The store rejects
// keys that resolve outside its root, so the wildcard is traversal-safe.
var media = app.MapGet("/media/{**storageKey}", (string storageKey, HttpContext http, IMediaStore store) =>
{
    try
    {
        var path = store.PhysicalPathOf(storageKey);
        if (!File.Exists(path))
        {
            return Results.NotFound();
        }

        var extension = Path.GetExtension(path).ToLowerInvariant();
        http.Response.Headers["X-Content-Type-Options"] = "nosniff";

        // SVG is the one media type that runs script when browsed as a document, and
        // ORIGINALS are stored raw — only the derived copy is sanitized. So a raw
        // original SVG is never served as a renderable image; it is an inert download.
        // A sanitized derived SVG may render (thumbnails use <img>, which never runs
        // SVG script anyway), but even then a locked-down CSP neuters script for the
        // direct-navigation case in the unlikely event the sanitizer ever regressed.
        if (extension == ".svg")
        {
            var isSanitized = storageKey.Replace('\\', '/').StartsWith("derived/", StringComparison.Ordinal);
            http.Response.Headers.ContentSecurityPolicy = "default-src 'none'; style-src 'unsafe-inline'; sandbox";
            return isSanitized
                ? Results.File(path, "image/svg+xml", enableRangeProcessing: true)
                : Results.File(path, "application/octet-stream",
                    fileDownloadName: Path.GetFileName(path), enableRangeProcessing: true);
        }

        var contentType = extension switch
        {
            ".webp" => "image/webp",
            ".webm" => "video/webm",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            _ => "application/octet-stream",
        };
        return Results.File(path, contentType, enableRangeProcessing: true);
    }
    catch (ArgumentException)
    {
        return Results.NotFound();
    }
});
if (authOptions.Enabled)
{
    media.RequireAuthorization();
}

// Canvas widget bundles: each catalog widget's ES module at /widgets/{tag}.js so
// islands hydrate INSIDE the editor canvas (see EditorRenderContextFactory). no-store:
// approved bundles are live read-model state and built-ins may change under dev.
var widgetBundles = app.MapGet("/widgets/{tag}.js", (string tag, EditorWidgetCatalog catalog, HttpContext http) =>
{
    if (catalog.BundleBytesOf(tag) is not { } bytes)
    {
        return Results.NotFound();
    }

    http.Response.Headers.CacheControl = "no-store";
    http.Response.Headers["X-Content-Type-Options"] = "nosniff";
    return Results.Bytes(bytes, "text/javascript; charset=utf-8");
});
if (authOptions.Enabled)
{
    widgetBundles.RequireAuthorization();
}

// Public contact intake: the published marketing sites' <contact-form> islands POST here
// (fetch, form-encoded; JSON works too for API callers). Fully anonymous BY DESIGN, like
// /healthz — a visitor sending a sales note has no Keycloak login, and RequireAuthorization
// above is per-endpoint, so it never covers this route. The recipient inbox lives
// server-side only — the site's private contact-form widget prop, or Contact:Recipients
// config (ContactIntake) — no email address ever appears in page markup.
app.MapPost("/api/contact", async (HttpContext http, ContactIntake intake, CancellationToken ct) =>
{
    var fields = await ReadContactFields(http, ct);
    if (fields is null)
    {
        return Results.BadRequest(new { ok = false, errors = new[] { "unreadable submission" } });
    }

    var errors = await intake.Handle(fields, ct);
    return errors.Count == 0
        ? Results.Ok(new { ok = true })
        : Results.BadRequest(new { ok = false, errors });
})
.AllowAnonymous()
.RequireCors("contact")
.RequireRateLimiting("contact");

// Public preview: the founder reviews the real published page (chrome + marketing CSS +
// hydrated live islands) at /preview/{site} and /preview/{site}/{slug}. Anonymous by
// design — this is public marketing content, not the editor. /preview (no site) lists the
// sites available to preview. The site is addressed by its slug (watchdog/cai/assay) or id.
app.MapGet("/preview", (SitePreview preview) =>
{
    var sites = preview.Sites();
    if (sites.Count == 0)
    {
        return Results.Content("<!doctype html><meta charset=utf-8><title>Preview</title><p>No sites to preview yet.</p>", "text/html");
    }

    var links = string.Join("", sites.Select(s =>
        $"<li><a href=\"/preview/{Uri.EscapeDataString(s.Slug)}/\">{System.Net.WebUtility.HtmlEncode(s.Name)}</a> " +
        $"<code>/preview/{System.Net.WebUtility.HtmlEncode(s.Slug)}</code></li>"));
    var body =
        "<!doctype html><meta charset=utf-8><title>Site previews</title>" +
        "<style>body{font:16px/1.6 system-ui,sans-serif;max-width:40rem;margin:3rem auto;padding:0 1rem}code{background:#eee;padding:1px 5px;border-radius:4px}</style>" +
        "<h1>Site previews</h1><p>The full published-style pages, hydrated with live widgets.</p><ul>" + links + "</ul>";
    return Results.Content(body, "text/html; charset=utf-8");
}).AllowAnonymous();

// /preview/{site} (no trailing path) serves the site's home page; the wildcard variant
// serves everything under it (nested pages + assets). The preview is regenerated on
// demand and rewritten per request — never cached, so an edit shows on the next hit.
static async Task<IResult> ServePreview(string site, string path, SitePreview preview, HttpContext http)
{
    var file = await preview.Serve(site, path);
    if (file is null)
    {
        return Results.NotFound();
    }

    http.Response.Headers.CacheControl = "no-store";
    http.Response.Headers["X-Content-Type-Options"] = "nosniff";
    return Results.Bytes(file.Bytes, file.ContentType);
}

app.MapGet("/preview/{site}", (string site, SitePreview preview, HttpContext http) =>
    ServePreview(site, "", preview, http)).AllowAnonymous();

app.MapGet("/preview/{site}/{**path}", (string site, string? path, SitePreview preview, HttpContext http) =>
    ServePreview(site, path ?? "", preview, http)).AllowAnonymous();

// Headless, token-authenticated authoring API (off-network content authoring via the same command
// path as the editor). Fail-closed: mapped ONLY when Imprint:Authoring:Token is configured.
app.MapAuthoringApi();

// The headless authoring MCP server at /mcp, gated by the same token (fail-closed).
app.MapImprintAuthoringMcp();

await app.Services.InitializeImprintEventSourcing();

app.Run();

// The island posts application/x-www-form-urlencoded (a preflight-free "simple" CORS
// request); JSON is accepted for programmatic callers. Both carry the same wire names.
static async Task<ContactFields?> ReadContactFields(HttpContext http, CancellationToken ct)
{
    if (http.Request.HasFormContentType)
    {
        var form = await http.Request.ReadFormAsync(ct);
        return new(form["topic"], form["name"], form["email"], form["org"], form["message"], form["website"], form["site"]);
    }

    try
    {
        return await http.Request.ReadFromJsonAsync<ContactFields>(ct);
    }
    catch (JsonException)
    {
        return null; // malformed body → one 400, not an unhandled 500
    }
}

static string ResolveWidgetsDirectory(WebApplicationBuilder builder)
{
    if (builder.Configuration["ImprintWidgets"] is { } configured)
    {
        return Path.GetFullPath(configured, builder.Environment.ContentRootPath);
    }

    // Development default: the repo's widgets/ directory, found by walking up.
    for (var directory = new DirectoryInfo(builder.Environment.ContentRootPath);
         directory is not null;
         directory = directory.Parent)
    {
        var candidate = Path.Combine(directory.FullName, "widgets");
        if (File.Exists(Path.Combine(candidate, "manifest.json")))
        {
            return candidate;
        }
    }

    return Path.Combine(builder.Environment.ContentRootPath, "widgets");
}
