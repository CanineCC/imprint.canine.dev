using System.Text;
using System.Text.Json;
using Imprint.Authoring;
using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Assets;
using Imprint.Authoring.Domain.Blocks;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Features.Assets;
using Imprint.EventSourcing;
using Imprint.TestKit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Imprint.Publishing.Tests.Pipeline;

/// <summary>
/// The pipeline-test host: a REAL SQLite (in-memory) event store, real projections,
/// fake media ports, and a <see cref="SitePublisher"/> writing to a per-test temp
/// directory. Command slices are out of scope here, so scenarios drive the domain
/// aggregates directly through the real <see cref="IAggregateStore"/> and let the
/// projection engine catch up — the same event path production takes.
/// </summary>
internal sealed class PublishingTestHost : IAsyncDisposable
{
    public static readonly Locale En = new("en");
    public static readonly Locale Da = new("da");

    private readonly SqliteTestDatabase _database = new();
    private readonly DirectoryInfo _root;

    public PublishingTestHost(string? baseUrl = null)
    {
        _root = Directory.CreateTempSubdirectory("imprint-publish-");
        OutputPath = Path.Combine(_root.FullName, "output");
        WidgetsDirectory = Path.Combine(_root.FullName, "widgets");
        Directory.CreateDirectory(WidgetsDirectory);
        File.WriteAllText(Path.Combine(WidgetsDirectory, "manifest.json"), "[]");

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        services.AddImprintAuthoring(_database.ConnectionString);
        services.AddSingleton<IMediaStore, InMemoryMediaStore>();
        services.AddSingleton<IMediaProcessor, FakeMediaProcessor>();
        services.AddImprintPublishing(new PublishingOptions
        {
            OutputPath = OutputPath,
            WidgetsDirectory = WidgetsDirectory,
            BaseUrl = baseUrl,
            DebounceMilliseconds = 50,
        });
        Services = services.BuildServiceProvider();
        Services.InitializeImprintEventSourcing().GetAwaiter().GetResult();
    }

    public ServiceProvider Services { get; }

    public string OutputPath { get; }

    public string WidgetsDirectory { get; }

    public SitePublisher Publisher => Services.GetRequiredService<SitePublisher>();
    public PublisherStatus Status => Services.GetRequiredService<PublisherStatus>();
    public ProjectionEngine Projections => Services.GetRequiredService<ProjectionEngine>();
    public InMemoryMediaStore Media => (InMemoryMediaStore)Services.GetRequiredService<IMediaStore>();

    private IAggregateStore Store => Services.GetRequiredService<IAggregateStore>();

    // ------------------------------------------------------------- output helpers

    public string FullPath(string relative) => Path.Combine(OutputPath, relative);

    public bool FileExists(string relative) => File.Exists(FullPath(relative));

    public string ReadText(string relative) => File.ReadAllText(FullPath(relative));

    public byte[] ReadBytes(string relative) => File.ReadAllBytes(FullPath(relative));

    public IReadOnlyList<string> AllFiles() =>
    [
        .. Directory.EnumerateFiles(OutputPath, "*", SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(OutputPath, file).Replace('\\', '/'))
            .Order(StringComparer.Ordinal),
    ];

    public IReadOnlyList<string> FilesMatching(string prefix, string suffix = "") =>
        [.. AllFiles().Where(f => f.StartsWith(prefix, StringComparison.Ordinal) && f.EndsWith(suffix, StringComparison.Ordinal))];

    /// <summary>Relative path → last write time, for zero-rewrite / untouched-file proofs.</summary>
    public Dictionary<string, DateTime> SnapshotWriteTimes() =>
        AllFiles().ToDictionary(file => file, file => File.GetLastWriteTimeUtc(FullPath(file)));

    public JsonDocument ReadManifest() => JsonDocument.Parse(ReadBytes("publish-manifest.json"));

    // ---------------------------------------------------------- aggregate drivers

    private async Task Commit(AggregateRoot aggregate)
    {
        await Store.Save(aggregate);
        await Projections.CatchUp();
    }

    public async Task<SiteId> CreateSite(string name = "Acme Studio", string defaultLocale = "en")
    {
        var id = SiteId.New();
        await Commit(Site.Create(id, name, new Locale(defaultLocale)));
        return id;
    }

    public async Task AddLocale(SiteId siteId, string locale)
    {
        var site = await Store.Load<Site>(siteId.Stream);
        site.AddLocale(new Locale(locale));
        await Commit(site);
    }

    public async Task SetNavigation(SiteId siteId, params PageId[] pages)
    {
        var site = await Store.Load<Site>(siteId.Stream);
        site.SetNavigation([.. pages.Select(page => new NavigationItem(page, null))]);
        await Commit(site);
    }

    public async Task SetThemeToken(SiteId siteId, string token, string light, string dark)
    {
        var site = await Store.Load<Site>(siteId.Stream);
        site.SetThemeToken(token, light, dark);
        await Commit(site);
    }

    public async Task<PageId> CreatePage(SiteId siteId, string slug, string title)
    {
        var id = PageId.New();
        Assert.True(Slug.TryCreate(slug, out var parsed, out var error), error);
        await Commit(Page.Create(id, siteId, parsed, En, title));
        return id;
    }

    public async Task AddSection(PageId pageId, SectionNode section)
    {
        var page = await Store.Load<Page>(pageId.Stream);
        page.AddNode(NodeId.Root, page.Tree.Roots.Count, section);
        await Commit(page);
    }

    public async Task SetTitle(PageId pageId, string locale, string title)
    {
        var page = await Store.Load<Page>(pageId.Stream);
        page.ChangeTitle(new Locale(locale), title);
        await Commit(page);
    }

    public async Task SetMeta(PageId pageId, string locale, string? metaTitle, string? metaDescription)
    {
        var page = await Store.Load<Page>(pageId.Stream);
        page.ChangeMeta(new Locale(locale), metaTitle, metaDescription);
        await Commit(page);
    }

    public async Task ChangeSlug(PageId pageId, string slug)
    {
        Assert.True(Slug.TryCreate(slug, out var parsed, out var error), error);
        var page = await Store.Load<Page>(pageId.Stream);
        page.ChangeSlug(parsed);
        await Commit(page);
    }

    public async Task Publish(PageId pageId)
    {
        var page = await Store.Load<Page>(pageId.Stream);
        page.Publish();
        await Commit(page);
    }

    public async Task Unpublish(PageId pageId)
    {
        var page = await Store.Load<Page>(pageId.Stream);
        page.Unpublish();
        await Commit(page);
    }

    public async Task<long> SiteVersion(SiteId siteId) => (await Store.Load<Site>(siteId.Stream)).Version;

    // --------------------------------------------------------------- media assets

    public async Task<AssetId> CreateImageAsset(string name = "photo", params int[] widths)
    {
        if (widths.Length == 0)
        {
            widths = [480, 960, 1440];
        }

        var id = AssetId.New();
        var originalKey = $"originals/{id.Compact}/{name}.jpg";
        Media.Seed(originalKey, Bytes($"{name}-original"));
        await Commit(Asset.Upload(id, $"{name}.jpg", "image/jpeg", AssetKind.Image, 1000, originalKey));

        var variants = new List<ImageVariant>();
        foreach (var width in widths)
        {
            var key = $"derived/{id.Compact}/{width}.webp";
            Media.Seed(key, Bytes($"{name}-{width}"));
            variants.Add(new ImageVariant(width, width * 2 / 3, key, 1000));
        }

        var asset = await Store.Load<Asset>(id.Stream);
        asset.CompleteImageVariants(variants);
        await Commit(asset);
        return id;
    }

    /// <summary>
    /// Simulates a re-process: the derived storage keys stay, their content changes —
    /// which is exactly what the publisher's content-hash staleness must detect.
    /// </summary>
    public void MutateImageVariants(AssetId id, string salt)
    {
        foreach (var key in Media.Files.Keys.Where(k => k.StartsWith($"derived/{id.Compact}/", StringComparison.Ordinal)).ToList())
        {
            Media.Seed(key, Bytes(key + salt));
        }
    }

    public async Task<AssetId> CreateSvgAsset(
        string svg = "<svg viewBox=\"0 0 10 10\"><path d=\"M0 0h10v10z\"/></svg>")
    {
        var id = AssetId.New();
        var originalKey = $"originals/{id.Compact}/icon.svg";
        Media.Seed(originalKey, Encoding.UTF8.GetBytes(svg));
        await Commit(Asset.Upload(id, "icon.svg", "image/svg+xml", AssetKind.Vector, svg.Length, originalKey));

        var cleanKey = $"derived/{id.Compact}/clean.svg";
        Media.Seed(cleanKey, Encoding.UTF8.GetBytes(svg));
        var asset = await Store.Load<Asset>(id.Stream);
        asset.CompleteSvgSanitize(cleanKey, 0);
        await Commit(asset);
        return id;
    }

    /// <summary>
    /// Attaches a ready dark-mode variant to an existing image asset. Its derived files
    /// live under a distinct <c>dark/{id}/</c> namespace so they never collide with (or
    /// get mutated alongside) the base variants under <c>derived/{id}/</c>.
    /// </summary>
    public async Task AddDarkImageVariant(AssetId id, params int[] widths)
    {
        if (widths.Length == 0)
        {
            widths = [480, 960, 1440];
        }

        var darkOriginal = $"originals/{id.Compact}/dark-source.png";
        Media.Seed(darkOriginal, Bytes($"{id.Compact}-dark-original"));
        var asset = await Store.Load<Asset>(id.Stream);
        asset.UploadDarkVariant(darkOriginal, "image/png");
        await Commit(asset);

        var variants = new List<ImageVariant>();
        foreach (var width in widths)
        {
            var key = $"dark/{id.Compact}/{width}.webp";
            Media.Seed(key, Bytes($"{id.Compact}-dark-{width}"));
            variants.Add(new ImageVariant(width, width * 2 / 3, key, 1000));
        }

        asset = await Store.Load<Asset>(id.Stream);
        asset.CompleteDarkImageVariants(variants);
        await Commit(asset);
    }

    /// <summary>Re-processes the dark variant: same keys, new content — the content-hash staleness trigger.</summary>
    public void MutateDarkImageVariants(AssetId id, string salt)
    {
        foreach (var key in Media.Files.Keys
                     .Where(k => k.StartsWith($"dark/{id.Compact}/", StringComparison.Ordinal)).ToList())
        {
            Media.Seed(key, Bytes(key + salt));
        }
    }

    /// <summary>Reverts an asset to neutral by dropping its dark-mode variant.</summary>
    public async Task RemoveDarkVariant(AssetId id)
    {
        var asset = await Store.Load<Asset>(id.Stream);
        asset.RemoveDarkVariant();
        await Commit(asset);
    }

    /// <summary>Attaches a ready dark-mode SVG variant to an existing vector asset (inlined, never a file).</summary>
    public async Task AddDarkSvgVariant(
        AssetId id, string svg = "<svg viewBox=\"0 0 10 10\"><circle cx=\"5\" cy=\"5\" r=\"4\"/></svg>")
    {
        var darkOriginal = $"originals/{id.Compact}/dark-icon.svg";
        Media.Seed(darkOriginal, Encoding.UTF8.GetBytes(svg));
        var asset = await Store.Load<Asset>(id.Stream);
        asset.UploadDarkVariant(darkOriginal, "image/svg+xml");
        await Commit(asset);

        var cleanKey = $"dark/{id.Compact}/clean.svg";
        Media.Seed(cleanKey, Encoding.UTF8.GetBytes(svg));
        asset = await Store.Load<Asset>(id.Stream);
        asset.CompleteDarkSvgSanitize(cleanKey, 0);
        await Commit(asset);
    }

    public async Task<BlockDefinitionId> DefineBlock(string name, Node spec)
    {
        var id = BlockDefinitionId.New();
        await Commit(BlockDefinition.Define(id, name, spec));
        return id;
    }

    public async Task UpdateBlockSpec(BlockDefinitionId id, Node spec)
    {
        var block = await Store.Load<BlockDefinition>(id.Stream);
        block.ChangeSpec(spec);
        await Commit(block);
    }

    // -------------------------------------------------------------------- widgets

    /// <summary>Rewrites the widgets directory: manifest plus one bundle file per entry.</summary>
    public void WriteWidgets(params (string Tag, string Js)[] widgets)
    {
        var manifest = widgets.Select(widget => new
        {
            tag = widget.Tag,
            name = widget.Tag,
            bundle = $"{widget.Tag}.js",
            placeholder = $"{widget.Tag} placeholder",
            props = new[] { new { name = "text", label = "Text" } },
        });
        File.WriteAllText(Path.Combine(WidgetsDirectory, "manifest.json"), JsonSerializer.Serialize(manifest));
        foreach (var (tag, js) in widgets)
        {
            File.WriteAllText(Path.Combine(WidgetsDirectory, $"{tag}.js"), js);
        }
    }

    private static byte[] Bytes(string seed) => Encoding.UTF8.GetBytes($"imprint-test-content:{seed}");

    public async ValueTask DisposeAsync()
    {
        await Services.DisposeAsync();
        _database.Dispose();
        try
        {
            _root.Delete(recursive: true);
        }
        catch (IOException)
        {
            // Temp cleanup is best effort.
        }
    }
}
