using Imprint.Authoring;
using Imprint.Authoring.Features.Assets;
using Imprint.Authoring.Features.Pages;
using Imprint.Editor.Components;
using Imprint.Editor.Services;
using Imprint.EventSourcing;
using Imprint.Media;
using Imprint.Publishing;

var builder = WebApplication.CreateBuilder(args);

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

var widgetCatalog = new EditorWidgetCatalog(widgetsDirectory);
builder.Services.AddSingleton(widgetCatalog);
builder.Services.AddSingleton<IWidgetCatalog>(widgetCatalog);
builder.Services.AddSingleton<EditorRenderContextFactory>();

// Per-circuit (per browser tab) editor state and its write path.
builder.Services.AddScoped<EditorSession>();
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<CommandRunner>();
builder.Services.AddScoped<CanvasBridge>();

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

// Canvas media: serves originals and derivatives to the editor UI. The store rejects
// keys that resolve outside its root, so the wildcard is traversal-safe.
app.MapGet("/media/{**storageKey}", (string storageKey, IMediaStore store) =>
{
    try
    {
        var path = store.PhysicalPathOf(storageKey);
        if (!File.Exists(path))
        {
            return Results.NotFound();
        }

        var contentType = Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".webp" => "image/webp",
            ".webm" => "video/webm",
            ".svg" => "image/svg+xml",
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
