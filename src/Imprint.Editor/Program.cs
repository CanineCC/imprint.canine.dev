using Imprint.Authoring;
using Imprint.Authoring.Features.Assets;
using Imprint.Authoring.Features.Pages;
using Imprint.Authoring.Projections;
using Imprint.Editor.Auth;
using Imprint.Editor.Components;
using Imprint.Editor.Services;
using Imprint.EventSourcing;
using Imprint.Media;
using Imprint.Publishing;

var builder = WebApplication.CreateBuilder(args);

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

// Per-circuit (per browser tab) editor state and its write path.
builder.Services.AddScoped<EditorSession>();
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<CommandRunner>();
builder.Services.AddScoped<CanvasBridge>();

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

await app.Services.InitializeImprintEventSourcing();

app.Run();

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
