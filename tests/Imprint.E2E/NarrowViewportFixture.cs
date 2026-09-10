using System.Diagnostics;
using System.Net;
using Imprint.Authoring;
using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Features.Assets;
using Imprint.Authoring.Syndication;
using Imprint.EventSourcing;
using Imprint.Publishing;
using Imprint.TestKit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace Imprint.E2E;

/// <summary>
/// A real published site, served by the real delivery host, opened in a real phone-width browser.
///
/// <para>★ Everything else in this suite reads the published output as BYTES, and bytes cannot
/// answer "does this page scroll sideways on a phone". Nobody had asked: 1440×900 was the only
/// viewport the suite had ever used. A producer syndicating ~2,400 pages headed <c>owner/name</c>
/// shipped a horizontal scrollbar on every one of them while every byte-level clause of the
/// delivery contract stayed green.</para>
///
/// <para>The site is built through the real aggregates and the real <see cref="SitePublisher"/>
/// rather than through the editor UI: the shapes under test are CONTENT shapes, and authoring them
/// by drag-and-drop would test the canvas instead of the stylesheet. It is then served by the real
/// <c>Imprint.Site</c> host — <c>file://</c> is not an option, because a published page links its
/// stylesheet, fonts and brand mark root-absolutely, and a layout measured in fallback font metrics
/// is a layout nobody is served.</para>
/// </summary>
public sealed class NarrowViewportFixture : IAsyncLifetime
{
    /// <summary>The reported shape: an owner/repository pair, with no space near the break point.</summary>
    public const string RepositoryHeading = "nordisk/harbour-api";

    /// <summary>An advisory identifier. Hyphenated — which turns out to matter; see the tests.</summary>
    public const string AdvisoryHeading = "GHSA-35jh-r3h4-6jhm";

    /// <summary>A scoped package name in an index table's first column.</summary>
    public const string PackageName = "@nordisk-platform/harbour-api-client-runtime";

    /// <summary>An artefact digest: 71 characters, and not one break opportunity among them.</summary>
    public const string Digest = "sha256:9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08";

    /// <summary>A bare advisory URL in body copy — the other place a long token lands in prose.</summary>
    public const string AdvisoryUrl =
        "https://github.com/nordisk/harbour-api/security/advisories/GHSA-35jh-r3h4-6jhm";

    /// <summary>
    /// The control. Ordinary prose, every word short enough to fit a phone line on its own, so a
    /// rule that breaks any of these words mid-word is breaking words it was never asked to break.
    /// </summary>
    public const string ControlProse =
        "The harbour service keeps a manifest of every vessel that has called at the port since the "
        + "registry was opened, and the crew updates it whenever a ship arrives or leaves again.";

    private static readonly Locale En = new("en");

    private SqliteTestDatabase? _database;
    private ServiceProvider? _services;
    private DirectoryInfo? _root;
    private Process? _host;
    private IPlaywright? _playwright;

    public IBrowser Browser { get; private set; } = null!;

    /// <summary>Origin of the running <c>Imprint.Site</c> serving the published output.</summary>
    public string BaseUrl { get; private set; } = null!;

    public string OutputPath { get; private set; } = null!;

    public string HostLogPath { get; private set; } = "";

    public async ValueTask InitializeAsync()
    {
        _root = Directory.CreateTempSubdirectory("imprint-narrow-");
        OutputPath = Path.Combine(_root.FullName, "output");
        var widgets = Path.Combine(_root.FullName, "widgets");
        Directory.CreateDirectory(widgets);
        await File.WriteAllTextAsync(Path.Combine(widgets, "manifest.json"), "[]");

        _database = new SqliteTestDatabase();
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        services.AddImprintAuthoring(_database.ConnectionString);
        services.AddSingleton<IMediaStore, InMemoryMediaStore>();
        services.AddSingleton<IMediaProcessor, FakeMediaProcessor>();
        services.AddImprintPublishing(new PublishingOptions
        {
            OutputPath = OutputPath,
            WidgetsDirectory = widgets,
            DebounceMilliseconds = 50,
        });
        _services = services.BuildServiceProvider();
        await _services.InitializeImprintEventSourcing();

        await BuildAndPublishSite();

        BaseUrl = $"http://127.0.0.1:{FreePort()}";
        var siteProject = EditorFixture.FindRepoPath("src/Imprint.Site");
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments =
                $"run --project \"{siteProject}\" --no-build --ImprintPublish=\"{OutputPath}\" --urls={BaseUrl}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = EditorFixture.FindRepoPath("."),
        };
        _host = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start Imprint.Site.");

        HostLogPath = Path.Combine(_root.FullName, "site-console.log");
        var log = new StreamWriter(HostLogPath) { AutoFlush = true };
        void Write(string line)
        {
            lock (log)
            {
                log.WriteLine($"{DateTime.UtcNow:HH:mm:ss.fff} {line}");
            }
        }

        _host.OutputDataReceived += (_, e) => { if (e.Data is not null) { Write(e.Data); } };
        _host.ErrorDataReceived += (_, e) => { if (e.Data is not null) { Write("ERR " + e.Data); } };
        _host.BeginOutputReadLine();
        _host.BeginErrorReadLine();

        await WaitForHttp(new Uri(BaseUrl + "/"), TimeSpan.FromSeconds(60));

        _playwright = await Playwright.CreateAsync();
        Browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = Environment.GetEnvironmentVariable("IMPRINT_E2E_HEADED") != "1",
        });
    }

    /// <summary>
    /// A page at phone width. 400px is the width the report measured and the width
    /// <c>tools/mobile-sweep.js</c> sweeps near; the device scale factor stays at 1 so a CSS pixel
    /// in an assertion message means what it says.
    /// </summary>
    public async Task<IPage> NewNarrowPage()
    {
        var context = await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 400, Height = 900 },
            BaseURL = BaseUrl,
        });
        context.SetDefaultTimeout(60_000);
        return await context.NewPageAsync();
    }

    /// <summary>Loads a published path at phone width and hands the loaded page to <paramref name="measure"/>.</summary>
    public async Task<T> OnNarrowPage<T>(string path, Func<IPage, Task<T>> measure)
    {
        var page = await NewNarrowPage();
        try
        {
            await page.GotoAsync(path, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            // The self-hosted fonts change every measurement on the page, so nothing is measured
            // until they have landed.
            await page.EvaluateAsync("() => document.fonts.ready");
            return await measure(page);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>
    /// What the browser thinks the page's width is: the widest thing on it, the room it had, and
    /// the viewport it was given.
    /// </summary>
    public Task<PageWidth> Measure(string path) => OnNarrowPage(path, async page =>
    {
        var metrics = await page.EvaluateAsync<int[]>(
            """
            () => [
                document.documentElement.scrollWidth,
                document.documentElement.clientWidth,
                window.innerWidth,
            ]
            """);
        return new PageWidth(path, metrics[0], metrics[1], metrics[2]);
    });

    /// <summary>
    /// Every element inside <c>main</c> that has become its own horizontal scroller, named. A page
    /// can measure exactly the viewport width and still hide a token behind a scrollbar a thumb has
    /// to find, so "the document fits" is not on its own the whole question.
    /// </summary>
    public Task<string[]> HorizontalScrollersOn(string path) => OnNarrowPage(path, page =>
        page.EvaluateAsync<string[]>(
            """
            () => [...document.querySelectorAll('main *')]
                .filter(el => el.scrollWidth > el.clientWidth + 1)
                .map(el => el.tagName.toLowerCase()
                    + (typeof el.className === 'string' && el.className
                        ? '.' + el.className.trim().split(/\s+/).join('.') : '')
                    + ` (${el.scrollWidth}px of content in ${el.clientWidth}px)`)
            """));

    public async ValueTask DisposeAsync()
    {
        if (Browser is not null)
        {
            await Browser.DisposeAsync();
        }

        _playwright?.Dispose();

        if (_host is { HasExited: false })
        {
            _host.Kill(entireProcessTree: true);
            await _host.WaitForExitAsync();
        }

        _host?.Dispose();
        if (_services is not null)
        {
            await _services.DisposeAsync();
        }

        _database?.Dispose();
        // The published output is deliberately left on disk: it is the post-mortem for a red run.
    }

    private async Task BuildAndPublishSite()
    {
        var services = _services!;
        var store = services.GetRequiredService<IAggregateStore>();
        var projections = services.GetRequiredService<ProjectionEngine>();
        var syndicated = services.GetRequiredService<SyndicatedPageStore>();

        async Task Commit(AggregateRoot aggregate)
        {
            await store.Save(aggregate);
            await projections.CatchUp();
        }

        // Publishing is a standalone decision the aggregate refuses to fold into the same commit as
        // an edit, so a page is written first and its publish decision taken on a reloaded
        // aggregate — the order the PublishPage slice takes.
        async Task PublishPage(PageId id)
        {
            var page = await store.Load<Page>(id.Stream);
            page.Publish();
            await Commit(page);
        }

        async Task<PageId> AuthorPage(SiteId site, string slug, string title, params Node[] children)
        {
            var id = PageId.New();
            Assert.True(Slug.TryCreate(slug, out var parsed, out var error), error);
            var page = Page.Create(id, site, parsed, En, title);
            page.AddNode(NodeId.Root, 0, Section(children));
            await Commit(page);
            await PublishPage(id);
            return id;
        }

        var siteId = SiteId.New();
        await Commit(Site.Create(siteId, "Harbour Registry", En, SiteKind.Site));

        // A home page so the site has chrome and a nav to render; the shapes under test live on
        // their own pages, one measurement each.
        var homeId = await AuthorPage(siteId, "home", "Home", Heading(1, "Harbour Registry"));

        var proseId = await AuthorPage(siteId, "prose", "Prose",
            Heading(1, "About the registry"),
            Prose($"<p>{ControlProse}</p>"),
            Prose($"<p>See <a href=\"{AdvisoryUrl}\">{AdvisoryUrl}</a> for the details.</p>"));

        // An index table of the ordinary kind, and the harsh one.
        var packagesId = await AuthorPage(siteId, "packages", "Packages",
            Heading(1, "Packages"),
            Table(("Package", "Version"), (PackageName, "4.2.0")));
        var artifactsId = await AuthorPage(siteId, "artifacts", "Artifacts",
            Heading(1, "Artifacts"),
            Table(("Digest", "Size"), (Digest, "12 MB")));

        var site = await store.Load<Site>(siteId.Stream);
        site.SetNavigation(
            [NavigationItem.Page(homeId), NavigationItem.Page(proseId),
             NavigationItem.Page(packagesId), NavigationItem.Page(artifactsId)]);
        site.SetHomePage(homeId);
        await Commit(site);

        // The reported pages. A syndicated page has no aggregate: it is a plain section holding an
        // h1, a paragraph and a sub-heading — exactly the producer's shape.
        syndicated.Upsert(Syndicated(siteId, "registry/github/nordisk/harbour-api", RepositoryHeading));
        syndicated.Upsert(Syndicated(siteId, "advisories/ghsa-35jh-r3h4-6jhm", AdvisoryHeading));

        var report = await services.GetRequiredService<SitePublisher>().Synchronize();
        Assert.Empty(report.Errors);
    }

    private static SectionNode Section(params Node[] children) =>
        new() { Id = NodeId.New(), Children = NodeList.Of(children) };

    private static HeadingNode Heading(int level, string text) =>
        new() { Id = NodeId.New(), Level = level, Text = LocalizedText.Of(En, text) };

    private static RichTextNode Prose(string html) =>
        new() { Id = NodeId.New(), Html = LocalizedText.Of(En, html) };

    private static TableNode Table((string Left, string Right) head, params (string Left, string Right)[] rows) =>
        new()
        {
            Id = NodeId.New(),
            Head = [LocalizedText.Of(En, head.Left), LocalizedText.Of(En, head.Right)],
            Rows =
            [
                .. rows.Select(row => (IReadOnlyList<LocalizedText>)
                    [LocalizedText.Of(En, row.Left), LocalizedText.Of(En, row.Right)]),
            ],
        };

    private static SyndicatedPage Syndicated(SiteId siteId, string path, string heading) =>
        new(siteId, path,
            LocalizedText.Of(En, heading),
            LocalizedText.Empty,
            LocalizedText.Of(En, "A published survey of one project."),
            Section(Heading(1, heading), Prose($"<p>{ControlProse}</p>"), Heading(2, "The condition")),
            ContentHash: "hash-1",
            UpdatedAt: DateTimeOffset.Parse("2026-09-10T00:00:00Z"));

    private static int FreePort()
    {
        using var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static async Task WaitForHttp(Uri url, TimeSpan timeout)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (HttpRequestException) { }
            catch (TaskCanceledException) { }
            await Task.Delay(250);
        }

        throw new TimeoutException($"Imprint.Site did not become reachable at {url} within {timeout}.");
    }
}

/// <summary>What a browser measured on one published page at phone width.</summary>
/// <param name="Path">The path that was loaded.</param>
/// <param name="ScrollWidth">The widest extent of the document — what the visitor can scroll to.</param>
/// <param name="ClientWidth">The room the document actually had, scrollbar excluded.</param>
/// <param name="ViewportWidth">The viewport the page was given.</param>
public sealed record PageWidth(string Path, int ScrollWidth, int ClientWidth, int ViewportWidth)
{
    /// <summary>How far past its own width the page reaches. Zero on a page that does not scroll sideways.</summary>
    public int Overflow => ScrollWidth - ClientWidth;

    public override string ToString() =>
        $"{Path}: scrollWidth {ScrollWidth}px in a {ClientWidth}px document ({ViewportWidth}px viewport)";
}
