using ContentSeeder;
using Imprint.Authoring;
using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Features.Assets;
using Imprint.Authoring.Features.Pages;
using Imprint.Authoring.Features.Sites.CreateSite;
using Imprint.Authoring.Projections;
using Imprint.Editor.Services;
using Imprint.EventSourcing;
using Imprint.Media;
using Imprint.Publishing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// ── args ─────────────────────────────────────────────────────────────────────
// ContentSeeder --db <path> [--cms <cmsRoot>] [--widgets <dir>] [--publish <dir>]
//               [--media <dir>] [--api-base <url>] [--no-publish]
// --api-base is the kennel PUBLIC-API origin the CAI data widgets fetch live from
// (e.g. https://api.watchdog.canine.dev). Empty ⇒ widgets ship their labelled sample
// as the offline default. The PARENT sets this to Track K's real URL at reseed time.
// Defaults target a fresh LOCAL verify store under ./_seedtest.
var opts = Args.Parse(args);
var cmsRoot = opts.Cms ?? "/home/jimmy/RiderProjects/cms.canine.dev";
var repoRoot = FindRepoRoot() ?? Directory.GetCurrentDirectory();
var widgetsDir = opts.Widgets ?? Path.Combine(repoRoot, "widgets");
var seedTestRoot = Path.Combine(repoRoot, "_seedtest");
var dbPath = opts.Db ?? Path.Combine(seedTestRoot, "imprint.db");
var publishRoot = opts.Publish ?? Path.Combine(seedTestRoot, "publish");
var mediaRoot = opts.Media ?? Path.Combine(seedTestRoot, "media");

Console.WriteLine("== Imprint ContentSeeder ==");
Console.WriteLine($"  CMS source : {cmsRoot}");
Console.WriteLine($"  Widgets    : {widgetsDir}");
Console.WriteLine($"  Target DB  : {dbPath}");
Console.WriteLine($"  Publish to : {publishRoot}");
Console.WriteLine($"  API base   : {(string.IsNullOrWhiteSpace(opts.ApiBase) ? "(none — widgets ship the labelled sample)" : opts.ApiBase)}");
Console.WriteLine();

Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
Directory.CreateDirectory(mediaRoot);

// ── host ─────────────────────────────────────────────────────────────────────
var services = new ServiceCollection();
services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
services.AddImprintAuthoring($"Data Source={dbPath}");
services.AddImprintMedia(new MediaOptions { RootPath = mediaRoot });
services.AddImprintAssetProcessing();
services.AddImprintPublishing(new PublishingOptions
{
    OutputPath = publishRoot,
    WidgetsDirectory = widgetsDir,
    BaseUrl = null,
});
// The page slices validate WidgetNodes against the real widget manifest.
services.AddSingleton(p => new EditorWidgetCatalog(widgetsDir, p.GetRequiredService<WidgetRegistry>()));
services.AddSingleton<IWidgetCatalog>(p => p.GetRequiredService<EditorWidgetCatalog>());

await using var provider = services.BuildServiceProvider();
provider.GetRequiredService<EventMetadataProvider>().ActorSource = () => "content-seeder";
await provider.InitializeImprintEventSourcing();

var dispatcher = provider.GetRequiredService<ICommandDispatcher>();
var siteOverview = provider.GetRequiredService<SiteOverview>();
var pageList = provider.GetRequiredService<PageList>();

// The four target sites: watchdog/assay/cai read the CMS checkout; canine's transcribed
// content ships inside this repo (tools/ContentSeeder/canine), resolved via repoRoot.
// --only narrows the run to named site keys — the way to add ONE new site to a store
// that already carries the others (MigrateSite assumes its pages don't exist yet).
var sites = Sites.All(cmsRoot, repoRoot);
if (opts.Only is { Count: > 0 } only)
{
    sites = [.. sites.Where(site => only.Contains(site.Key, StringComparer.OrdinalIgnoreCase))];
    Console.WriteLine($"  Only       : {string.Join(", ", sites.Select(s => s.Key))}");
}

// ── 1. ensure the four sites + their empty home pages exist (mirror prod ids) ──
// --publish-only skips authoring (steps 1-2) for an already-seeded store: the
// cutover mode — configure environments and/or render static output, nothing else.
foreach (var site in opts.PublishOnly ? [] : sites)
{
    if (siteOverview.Get(site.SiteId) is null)
    {
        await Dispatch(new CreateSite(site.SiteId, site.BrandName, "en"), $"CreateSite {site.Key}");
    }

    if (pageList.Get(site.HomePageId) is null)
    {
        await Dispatch(
            new Imprint.Authoring.Features.Pages.CreatePage.CreatePage(
                site.HomePageId, site.SiteId, $"{site.BrandName} — home", "home", "en"),
            $"CreatePage {site.Key}/home");
    }
}

// ── 2. migrate ──
var migrator = new Migrator(dispatcher, opts.ApiBase);
var results = new List<Migrator.SiteResult>();
foreach (var site in opts.PublishOnly ? [] : sites)
{
    var r = await migrator.MigrateSite(site);
    results.Add(r);
    Console.WriteLine($"  {site.Key,-9} authored {r.PagesAuthored} pages + {r.DocsAuthored} docs, published {r.Published}");
}

Console.WriteLine();

// ── 2b. production environment config (cutover): one "Production" deploy target per
// site in the run — the estate's convention ({root}/{key}, BaseUrl = the site's
// public origin). The aggregate no-ops when unchanged, so this is re-runnable.
if (opts.ProdEnvRoot is { Length: > 0 } envRoot)
{
    foreach (var site in sites)
    {
        await Dispatch(
            new Imprint.Authoring.Features.Sites.ConfigureEnvironments.ConfigureEnvironments(
                site.SiteId,
                [new DeployEnvironment("Production", $"{envRoot}/{site.Key}", site.Origin)]),
            $"ConfigureEnvironments {site.Key}");
        Console.WriteLine($"  env {site.Key}: Production → {envRoot}/{site.Key} ({site.Origin})");
    }
}

// ── 3. publish static output (unless suppressed) ──
if (!opts.NoPublish)
{
    var publisher = provider.GetRequiredService<SitePublisher>();
    var projections = provider.GetRequiredService<ProjectionEngine>();
    await projections.CatchUp();
    foreach (var site in sites)
    {
        var aggregate = await provider.GetRequiredService<IAggregateStore>().Load<Site>(site.SiteId.Stream);
        var target = new PublishTarget(aggregate, Path.Combine(publishRoot, site.Key), site.Origin);
        var report = await publisher.Synchronize(target);
        Console.WriteLine($"  published {site.Key}: {report.PagesRendered} rendered, {report.FilesWritten} files" +
                          (report.Errors.Count > 0 ? $", {report.Errors.Count} ERRORS" : ""));
        foreach (var err in report.Errors)
        {
            Console.WriteLine($"    ERROR {err.PageId.Compact}: {err.Message}");
        }
    }
}

Console.WriteLine();

// ── 4. verify ──
var ok = await Verify.Run(provider, sites, publishRoot, opts.NoPublish);

Console.WriteLine();
Console.WriteLine("== FLAGS (blocks/copy that could not map 1:1 — never invented) ==");
foreach (var flag in migrator.AllFlags.Distinct().OrderBy(f => f, StringComparer.Ordinal))
{
    Console.WriteLine($"  - {flag}");
}

Console.WriteLine();
Console.WriteLine(ok ? "== VERIFY: PASS ==" : "== VERIFY: FAIL ==");
return ok ? 0 : 1;

async Task Dispatch(ICommand command, string what)
{
    var result = await dispatcher.Dispatch(command);
    if (!result.Succeeded)
    {
        throw new InvalidOperationException($"{what} FAILED: {result.ErrorMessage}");
    }
}

static string? FindRepoRoot()
{
    for (var dir = new DirectoryInfo(Directory.GetCurrentDirectory()); dir is not null; dir = dir.Parent)
    {
        if (File.Exists(Path.Combine(dir.FullName, "Imprint.slnx")))
        {
            return dir.FullName;
        }
    }

    // Fall back to walking up from the assembly location (bin/Debug/net10.0).
    for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
    {
        if (File.Exists(Path.Combine(dir.FullName, "Imprint.slnx")))
        {
            return dir.FullName;
        }
    }

    return null;
}

internal sealed record Options(
    string? Db, string? Cms, string? Widgets, string? Publish, string? Media, string? ApiBase, bool NoPublish,
    IReadOnlyList<string>? Only, bool PublishOnly, string? ProdEnvRoot);

internal static class Args
{
    public static Options Parse(string[] args)
    {
        string? db = null, cms = null, widgets = null, publish = null, media = null, apiBase = null;
        var noPublish = false;
        var publishOnly = false;
        string? prodEnvRoot = null;
        List<string>? only = null;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--db": db = args[++i]; break;
                case "--cms": cms = args[++i]; break;
                case "--widgets": widgets = args[++i]; break;
                case "--publish": publish = args[++i]; break;
                case "--media": media = args[++i]; break;
                case "--api-base": apiBase = args[++i]; break;
                case "--no-publish": noPublish = true; break;
                case "--only": only = [.. args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)]; break;
                case "--publish-only": publishOnly = true; break;
                case "--prod-env-root": prodEnvRoot = args[++i]; break;
                default:
                    Console.Error.WriteLine($"Unknown argument: {args[i]}");
                    break;
            }
        }

        return new Options(db, cms, widgets, publish, media, apiBase, noPublish, only, publishOnly, prodEnvRoot);
    }
}
