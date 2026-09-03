using System.Diagnostics;
using System.Text;
using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Assets;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Posts;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Features.Assets;
using Imprint.Authoring.Projections;
using Imprint.Authoring.Syndication;
using Imprint.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RenderMode = Imprint.Rendering.RenderMode;

namespace Imprint.Publishing;

/// <summary>One site rendered to one output folder, addressed by one base URL — the unit a publish pass acts on.</summary>
/// <param name="IncludeDrafts">Render draft state too: pages from their current draft trees
/// (published or not) and unpublished posts from their current markdown. Only the /preview
/// plane sets this: a preview exists to answer "how will this look" BEFORE the decision to
/// publish, and every real deploy target must show exactly what was approved.</param>
public sealed record PublishTarget(Site Site, string OutputPath, string? BaseUrl, bool IncludeDrafts = false);

/// <summary>
/// The file-system projection: keeps an output folder equal to "the published state of
/// a site, rendered". <see cref="Synchronize(PublishTarget, CancellationToken)"/> is
/// idempotent and diff-driven — the publish manifest in that folder is the durable
/// checkpoint, staleness is manifest vs. current read models, and same inputs produce
/// byte-identical outputs (content hashes included), so an up-to-date pass writes
/// nothing at all. Each (site, folder) target converges independently against its own
/// manifest, which is what lets one site publish to several environment folders.
/// </summary>
public sealed class SitePublisher(
    PublishingOptions options,
    SiteOverview siteOverview,
    PublishedContent publishedContent,
    PublishedPosts publishedPosts,
    SyndicatedPageStore syndicated,
    AssetLibrary assetLibrary,
    BlockLibrary blockLibrary,
    WidgetRegistry widgetRegistry,
    IMediaStore mediaStore,
    PublisherStatus status,
    PublishGate gate,
    ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<SitePublisher>();

    /// <summary>
    /// Legacy single-site sync: the first-created site to the globally configured
    /// <see cref="PublishingOptions.OutputPath"/>. Retained for single-site installs and
    /// as the hosted service's fallback for a site with no environments configured.
    /// </summary>
    public Task<PublishReport> Synchronize(CancellationToken ct = default)
    {
        var site = siteOverview.Current;
        return site is null
            // Nothing exists to publish yet; report empty (the folder is left untouched
            // by the pass, which never runs) rather than sweeping a real output away.
            ? Task.FromResult(new PublishReport(0, 0, 0, 0, [], DateTimeOffset.UtcNow, TimeSpan.Zero))
            : Synchronize(new PublishTarget(site, options.OutputPath, options.BaseUrl), ct);
    }

    /// <summary>Render one site's published content to one output folder — the per-site, per-environment projection.</summary>
    public Task<PublishReport> Synchronize(PublishTarget target, CancellationToken ct = default) =>
        gate.RunExclusive(async () =>
        {
            var pass = new Pass(
                options, target.Site, target.OutputPath, target.BaseUrl, target.IncludeDrafts,
                publishedContent, publishedPosts, syndicated,
                assetLibrary, blockLibrary, widgetRegistry, mediaStore, loggerFactory, _logger);
            var report = await pass.Run(ct);
            status.Record(report);
            return report;
        }, ct);

    /// <summary>
    /// One synchronize pass over an immutable snapshot of the inputs. Read models
    /// mutate live while the publisher runs — an accepted race: the snapshot is taken
    /// version-first, so a torn read records an *older* version in the manifest than
    /// the data it rendered, and the next (debounced, CaughtUp-triggered) pass sees
    /// the version move and re-renders. The race converges instead of sticking.
    /// </summary>
    private sealed class Pass(
        PublishingOptions options,
        Site site,
        string outputPath,
        string? baseUrl,
        bool includeDrafts,
        PublishedContent publishedContent,
        PublishedPosts publishedPosts,
        SyndicatedPageStore syndicated,
        AssetLibrary assetLibrary,
        BlockLibrary blockLibrary,
        WidgetRegistry widgetRegistry,
        IMediaStore mediaStore,
        ILoggerFactory loggerFactory,
        ILogger logger)
    {
        private sealed record PagePlan(
            PublishedPage Page,
            string? SlugPath,
            IReadOnlyList<string> Paths,
            IReadOnlyList<string> AssetHashes,
            IReadOnlyList<string> Dependencies,
            bool Stale);

        private readonly string _outputRoot = Path.GetFullPath(outputPath);
        private readonly string? _baseUrl = baseUrl?.TrimEnd('/');

        // Serializes a block definition's spec to a stable content hash. Node
        // polymorphism + value-object converters come from the authoring context.
        private static readonly System.Text.Json.JsonSerializerOptions BlockJson = CreateBlockJson();
        private readonly Dictionary<BlockDefinitionId, string> _blockHashes = [];

        private readonly Dictionary<PageId, string> _errors = [];
        /// <summary>
        /// The renderer's identity, as the module version id of the assembly the node views
        /// live in — a fresh GUID on every build that actually changes it, which is exactly
        /// the question being asked: "is this the code that produced the published markup?"
        /// Nothing weaker works: an assembly version is hand-maintained and would be forgotten,
        /// and hashing the views would miss a change in anything they call.
        /// </summary>
        private static readonly string RendererVersion =
            typeof(RenderContext).Assembly.ManifestModule.ModuleVersionId.ToString("N")[..16];

        private readonly HashSet<PageId> _failed = [];
        private readonly HashSet<string> _written = new(StringComparer.Ordinal);
        private int _filesWritten;
        private long _bytesWritten;

        // Snapshot state, filled by Run before any file is touched.
        private long _siteVersion;
        private string _siteName = "";
        private SiteKind _siteKind;
        private Locale _defaultLocale;
        private IReadOnlyList<Locale> _locales = [];
        private IReadOnlyList<NavigationItem> _navigation = [];
        private IReadOnlyList<FooterLinkGroup> _footerGroups = [];
        private HeaderAction? _headerCta;
        private HeaderAction? _headerQuiet;
        private CopyLine? _copyLine;
        private AssetId? _faviconAssetId;
        private AssetId? _headerLogoAssetId;
        private AssetId? _socialImageAssetId;
        private string? _faviconUrl;
        private string? _logoUrl;
        private string? _logoSvg;
        private string? _socialImageUrl;
        private string? _llmsPreamble;
        private IReadOnlyList<string> _llmsExcludedPaths = [];

        // Bounded so llms.txt stays a map a model reads whole, not a corpus dump.
        private const int MaxLlmsPages = 200;

        // llms-full.txt IS the corpus dump, so it is bounded by size rather than by page
        // count: what decides whether it is useful is whether it fits the context it gets
        // read into, and a thousand short pages cost less than fifty long ones. Counted in
        // chars, which for this content (overwhelmingly ASCII prose) tracks UTF-8 bytes
        // closely enough for a budget — it is a ceiling, not an exact byte guarantee.
        private const int MaxLlmsFullChars = 1_000_000;
        private IReadOnlyList<PublishedPage> _pages = [];
        private Dictionary<PageId, PublishedPage> _pageById = [];
        private Dictionary<PageId, string> _slugPathOf = [];
        private Dictionary<PageId, IReadOnlyList<string>> _pageWidgetTags = [];
        private Dictionary<string, WidgetDescriptor> _descriptors = [];
        private HashSet<string> _builtInWidgetTags = new(StringComparer.Ordinal);
        private SortedDictionary<string, (string RelativePath, string Hash, byte[] Bytes)> _widgetFiles = new(StringComparer.Ordinal);
        private PublishedAssetCatalog _assets = null!;
        private string _cssFile = "";

        public async Task<PublishReport> Run(CancellationToken ct)
        {
            var startedTimestamp = Stopwatch.GetTimestamp();
            Directory.CreateDirectory(_outputRoot);

            // Version FIRST, data after (see the class comment for why that order matters).
            _siteVersion = site.Version;
            _siteName = site.Name;
            _siteKind = site.Kind;
            var theme = site.Theme;
            _defaultLocale = site.DefaultLocale;
            _locales = [.. site.Locales];
            _navigation = [.. site.Navigation];
            _footerGroups = [.. site.FooterGroups];
            _headerCta = site.HeaderCta;
            _headerQuiet = site.HeaderQuiet;
            _copyLine = site.CopyLine;
            // Brand imagery is not necessarily referenced by any page, so its ids are
            // captured here and fed into the SAME published-asset catalog the page images
            // use (below). That copies their bytes into /assets/… and lets us resolve the
            // favicon/logo to a PUBLISHED /assets URL — one that exists in the deploy output
            // and the /preview plane, unlike the editor-only /media route.
            _faviconAssetId = site.FaviconAssetId;
            _headerLogoAssetId = site.HeaderLogoAssetId;
            _socialImageAssetId = site.SocialImageAssetId;
            _llmsPreamble = site.LlmsPreamble;
            _llmsExcludedPaths = [.. site.LlmsExcludedPaths];
            // Only THIS site's published pages — a target folder holds exactly one site. Pages
            // syndicated from another system join them here and are otherwise indistinguishable:
            // same views, same chrome, same sitemap, same sweep. Everything the renderer learns,
            // they learn too, because there is only one renderer.
            _posts = includeDrafts
                ? publishedPosts.AllForSiteWithDrafts(site.Id, DateTimeOffset.UtcNow)
                : publishedPosts.AllForSite(site.Id);
            _pages =
            [
                // The preview plane asks for drafts, and that must cover PAGES, not only posts:
                // an author reviewing "how will this look" is reviewing the tree they just
                // edited. Every real deploy target keeps reading the published projection.
                .. includeDrafts
                    ? publishedContent.AllForSiteWithDrafts(site.Id)
                    : publishedContent.AllForSite(site.Id),
                .. SyndicatedPagesOf(site.Id),
                .. PostPagesOf(),
            ];
            _pageById = _pages.ToDictionary(page => page.Id);

            var oldManifest =
                PublishManifest.Load(Path.Combine(_outputRoot, PublishManifest.FileName)) ?? new PublishManifest();

            // ---- stylesheet: tokens + structural styles + the marketing chrome/appearance
            //      layer, one hashed file. Order matters: tokens define the vars the two
            //      style layers consume; the marketing layer comes last so it can build on
            //      (and, where intended, override) the structural defaults.
            var cssText = ThemeCss.Emit(theme) + "\n" + ThemeCss.StructuralCss + "\n" + ThemeCss.MarketingCss;
            var cssBytes = Encoding.UTF8.GetBytes(cssText);
            var cssHash = Hashing.Hash16(cssBytes);
            _cssFile = $"css/site.{cssHash}.css";

            _descriptors = WidgetManifest
                .Load(Path.Combine(options.WidgetsDirectory, "manifest.json"))
                .ToDictionary(descriptor => descriptor.Tag, StringComparer.Ordinal);
            _builtInWidgetTags = [.. _descriptors.Keys];

            // Approved submissions render exactly like built-ins — ResolveWidget emits the
            // same custom element. A built-in tag wins a collision (it can never be
            // shadowed), so only non-colliding approved tags are added.
            foreach (var approved in widgetRegistry.Approved)
            {
                if (!_builtInWidgetTags.Contains(approved.Tag))
                {
                    _descriptors[approved.Tag] = ApprovedWidgetDescriptors.ToDescriptor(approved);
                }
            }

            var ordered = OrderPages();
            ClaimPaths(ordered, oldManifest);

            // ---- per-page dependencies (blocks resolved: instance content lives in the definition).
            var pageAssetIds = ordered.ToDictionary(
                page => page.Id,
                page => (IReadOnlyList<AssetId>)
                    [.. NodesOf(page).SelectMany(AssetReferencesOf).Distinct()]);
            _pageWidgetTags = ordered.ToDictionary(
                page => page.Id,
                page => (IReadOnlyList<string>)
                    [.. NodesOf(page).OfType<WidgetNode>().Select(widget => widget.Tag).Distinct().Order(StringComparer.Ordinal)]);

            // Brand assets ride the same catalog as page images: their bytes land under
            // assets/ (CopyAssets) and stay unswept (DesiredFiles), so the published/preview
            // <link rel=icon>/<img> point at real files.
            var brandAssetIds = new[] { _faviconAssetId, _headerLogoAssetId, _socialImageAssetId }
                .Where(id => id.HasValue).Select(id => id!.Value);
            _assets = await PublishedAssetCatalog.Build(
                pageAssetIds.Values.SelectMany(ids => ids).Concat(brandAssetIds),
                assetLibrary, mediaStore, logger, ct,
                // The share card is fetched by link scrapers, not browsers. Several of
                // them (LinkedIn among them) skip a WebP og:image and fall back to a
                // no-image card, so this one asset also ships in the format it was
                // uploaded in.
                withOriginals: _socialImageAssetId is { } social ? [social] : []);

            // Now the catalog exists, resolve the brand imagery to its PUBLISHED /assets URL.
            _faviconUrl = BrandPublishedUrl(_faviconAssetId, preferSmallest: true);
            _logoUrl = BrandPublishedUrl(_headerLogoAssetId, preferSmallest: false);
            _logoSvg = BrandInlineSvg(_headerLogoAssetId);
            _socialImageUrl = SocialImagePublishedUrl(_socialImageAssetId);
            await LoadWidgetBundles(ct);

            var plans = PlanPages(ordered, pageAssetIds, oldManifest, cssHash);

            // ---- render what is stale.
            var pagesRendered = await RenderStalePages(plans, ct);
            await using var renderServices = new ServiceCollection().BuildServiceProvider();
            await using (var renderer = new HtmlRenderer(renderServices, loggerFactory))
            {
                await WriteIfChanged("404.html", Encoding.UTF8.GetBytes(await RenderNotFound(renderer, ct)), ct);
            }

            // ---- fixed outputs.
            await WriteIfChanged(_cssFile, cssBytes, ct);
            await WriteIfChanged("sitemap.xml", Encoding.UTF8.GetBytes(BuildSitemap(plans)), ct);
            await WriteIfChanged("robots.txt", Encoding.UTF8.GetBytes(BuildRobots()), ct);
            if (_posts.Count > 0)
            {
                // Only when there is something to syndicate: an empty feed is a broken promise a
                // reader's aggregator would keep polling.
                await WriteIfChanged("feed.xml", Encoding.UTF8.GetBytes(BuildFeed()), ct);
            }
            await WriteIfChanged("llms.txt", Encoding.UTF8.GetBytes(BuildLlmsTxt(plans)), ct);
            await WriteIfChanged("llms-full.txt", Encoding.UTF8.GetBytes(BuildLlmsFullTxt(plans)), ct);
            await CopyFonts(ct);
            await CopyAssets(ct);
            await CopyWidgetBundles(ct);

            var desired = DesiredFiles(plans);
            await Precompress(desired, ct);

            // ---- the sweep: anything on disk not reachable from the desired state is
            // a leftover (unpublished page, rotated hash) and goes away.
            Sweep(desired);

            // ---- the checkpoint is written last: a crash before this point leaves the
            // old manifest in place, and the next pass simply redoes the missing work.
            await WriteIfChanged(PublishManifest.FileName, BuildManifest(plans, cssHash).ToUtf8Json(), ct);

            var pagesRemoved = oldManifest.Pages.Keys.Count(key => !_pageById.Keys.Any(id => id.Compact == key));
            var errors = ordered
                .Where(page => _errors.ContainsKey(page.Id))
                .Select(page => new PublishReport.PageError(page.Id, _errors[page.Id]))
                .ToList();
            return new PublishReport(
                pagesRendered, pagesRemoved, _filesWritten, _bytesWritten, errors,
                DateTimeOffset.UtcNow, Stopwatch.GetElapsedTime(startedTimestamp));
        }

        // ------------------------------------------------------------------ planning

        /// <summary>
        /// Deterministic page order: home first, then navigation order, then slug, then
        /// id — the order path claims are resolved in when the manifest holds no memory
        /// of a previous owner.
        /// </summary>
        private List<PublishedPage> OrderPages()
        {
            var homeId = HomePageId();
            return
            [
                .. _pages
                    .OrderByDescending(page => homeId is { } home && page.Id == home)
                    .ThenBy(page => NavigationOrder(page.Id))
                    .ThenBy(page => page.PublicPath, StringComparer.Ordinal)
                    .ThenBy(page => page.Id.Compact, StringComparer.Ordinal),
            ];
        }

        /// <summary>
        /// The nav-first *published* page renders at the site root; without one there is no
        /// root page. Only a top-level DIRECT page link is a home candidate — group
        /// headings and external links carry no page identity.
        /// </summary>
        private PageId? HomePageId()
        {
            foreach (var item in _navigation)
            {
                if (item.PageId is { } pageId && _pageById.ContainsKey(pageId))
                {
                    return pageId;
                }
            }

            return null;
        }

        private int NavigationOrder(PageId id)
        {
            for (var i = 0; i < _navigation.Count; i++)
            {
                if (_navigation[i].PageId == id)
                {
                    return i;
                }
            }

            return int.MaxValue;
        }

        private void ClaimPaths(List<PublishedPage> ordered, PublishManifest oldManifest)
        {
            var homeId = HomePageId();
            foreach (var group in ordered.GroupBy(page => homeId is { } home && page.Id == home ? "" : page.PublicPath))
            {
                var claimants = group.ToList();
                var winner = claimants[0];
                if (claimants.Count > 1)
                {
                    // Slug collision between published pages. First-in-wins: the page
                    // the checkpoint says already owned the path keeps it; a brand-new
                    // tie falls back to the deterministic order. BOTH pages get the
                    // error — the editor must show the problem on each of them.
                    var defaultPath = DirectoryPath(group.Key, _defaultLocale);
                    winner = claimants.FirstOrDefault(claimant =>
                            oldManifest.Pages.GetValueOrDefault(claimant.Id.Compact)?.Paths
                                .Contains(defaultPath, StringComparer.Ordinal) == true)
                        ?? claimants[0];
                    foreach (var claimant in claimants)
                    {
                        _errors[claimant.Id] =
                            $"The slug '{group.Key}' is used by more than one published page; only one can be served at {defaultPath}.";
                    }
                }

                _slugPathOf[winner.Id] = group.Key;
            }
        }

        /// <summary>
        /// Every same-site page a chrome link (nav — top-level or a group child — the
        /// footer columns, and the header actions) points at. Its label and href come from
        /// *that page's* published state (slug, title), which the site version does not
        /// cover, so each is a staleness input. External links carry no page identity.
        /// </summary>
        private IEnumerable<PageId> ChromePageLinks()
        {
            foreach (var item in _navigation)
            {
                // The Link, not item.PageId: a link into a section of a page still shows that
                // page's slug, so it is still a staleness input even though it is not that page.
                if (item.Link is PageLink { PageId: var topLevel })
                {
                    yield return topLevel;
                }

                foreach (var child in item.Children)
                {
                    if (child.PageId is { } childPage)
                    {
                        yield return childPage;
                    }
                }
            }

            foreach (var group in _footerGroups)
            {
                foreach (var link in group.Links)
                {
                    if (link.PageId is { } footerPage)
                    {
                        yield return footerPage;
                    }
                }
            }

            foreach (var action in new[] { _headerCta, _headerQuiet })
            {
                if (action?.PageId is { } actionPage)
                {
                    yield return actionPage;
                }
            }
        }

        /// <summary>
        /// The chrome (nav, footer, header) is shared markup rendered into every page, but
        /// its hrefs and labels come from *other pages'* published state (slug, title),
        /// which the site version does not cover. The manifest records each page's
        /// publishedVersion and paths, so "did anything a chrome link shows change?" is
        /// answerable from the checkpoint alone — when it did, every page is stale.
        /// </summary>
        private bool ChromeStale(PublishManifest oldManifest)
        {
            foreach (var pageId in ChromePageLinks().Distinct())
            {
                var old = oldManifest.Pages.GetValueOrDefault(pageId.Compact);
                if (_pageById.GetValueOrDefault(pageId) is not { } current ||
                    !_slugPathOf.TryGetValue(pageId, out var slugPath))
                {
                    if (old is { Paths.Count: > 0 })
                    {
                        return true; // the link just vanished from every page's chrome
                    }

                    continue;
                }

                if (old is null ||
                    old.PublishedVersion != current.PublishedVersion || // published title (the label) may have moved
                    !old.Paths.SequenceEqual(PathsOf(slugPath), StringComparer.Ordinal)) // the href moved
                {
                    return true;
                }
            }

            return false;
        }

        private List<PagePlan> PlanPages(
            List<PublishedPage> ordered,
            Dictionary<PageId, IReadOnlyList<AssetId>> pageAssetIds,
            PublishManifest oldManifest,
            string cssHash)
        {
            var plans = new List<PagePlan>(ordered.Count);
            var chromeStale = ChromeStale(oldManifest);
            foreach (var page in ordered)
            {
                var isOwner = _slugPathOf.TryGetValue(page.Id, out var slugPath);
                IReadOnlyList<string> paths = isOwner ? PathsOf(slugPath!) : [];
                var assetHashes = _assets.HashesOf(pageAssetIds[page.Id]);
                var dependencies = DependencyTokensOf(page);
                var old = oldManifest.Pages.GetValueOrDefault(page.Id.Compact);

                // A used widget's bundle "moved" when its current hash (or absence)
                // differs from what the page was rendered against.
                var widgetMoved = _pageWidgetTags[page.Id].Any(tag =>
                    (_widgetFiles.TryGetValue(tag, out var file) ? file.Hash : null) !=
                    oldManifest.WidgetBundles.GetValueOrDefault(tag));

                var stale =
                    old is null
                    || old.Error is not null // errored pages re-evaluate every pass, so transient failures retry
                    || _errors.ContainsKey(page.Id)
                    || old.PublishedVersion < page.PublishedVersion
                    || old.RenderedAtSiteVersion < _siteVersion
                    || chromeStale
                    || oldManifest.CssHash != cssHash
                    || oldManifest.RendererVersion != RendererVersion
                    || !old.Paths.SequenceEqual(paths, StringComparer.Ordinal)
                    || !old.AssetHashes.SequenceEqual(assetHashes, StringComparer.Ordinal)
                    || !old.Dependencies.SequenceEqual(dependencies, StringComparer.Ordinal)
                    || widgetMoved
                    || paths.Any(path => !File.Exists(FullPath(IndexFileOf(path))));

                plans.Add(new PagePlan(page, isOwner ? slugPath : null, paths, assetHashes, dependencies, stale));
            }

            return plans;
        }

        // ----------------------------------------------------------------- rendering

        private async Task<int> RenderStalePages(List<PagePlan> plans, CancellationToken ct)
        {
            var rendered = 0;
            await using var renderServices = new ServiceCollection().BuildServiceProvider();
            await using var renderer = new HtmlRenderer(renderServices, loggerFactory);
            foreach (var plan in plans)
            {
                if (!plan.Stale || plan.SlugPath is null)
                {
                    continue;
                }

                try
                {
                    // All locale variants render before anything is written, so a
                    // failing locale cannot leave the page half-updated on disk.
                    var files = new List<(string Relative, byte[] Bytes)>();
                    foreach (var locale in _locales)
                    {
                        var html = await RenderPage(renderer, plan.Page, plan.SlugPath, locale, ct);
                        files.Add((IndexFileOf(DirectoryPath(plan.SlugPath, locale)), Encoding.UTF8.GetBytes(html)));
                    }

                    foreach (var (relative, bytes) in files)
                    {
                        await WriteIfChanged(relative, bytes, ct);
                    }

                    rendered++;
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    // Never let one page take the publisher down. The failed page is
                    // withdrawn (its files sweep away — an honest 404 beats silently
                    // stale bytes) and the error surfaces in the manifest and editor.
                    logger.LogError(e, "Rendering page {PageId} failed; withdrawing it from the output.", plan.Page.Id);
                    _errors[plan.Page.Id] = $"Rendering failed: {e.Message}";
                    _failed.Add(plan.Page.Id);
                }
            }

            return rendered;
        }

        private async Task<string> RenderPage(
            HtmlRenderer renderer, PublishedPage page, string slugPath, Locale locale, CancellationToken ct)
        {
            var context = new RenderContext
            {
                Mode = RenderMode.Static,
                Locale = locale,
                DefaultLocale = _defaultLocale,
                ResolveAsset = _assets.Resolve,
                ResolvePagePath = id => _slugPathOf.TryGetValue(id, out var target) ? DirectoryPath(target, locale) : null,
                ResolveBlock = id => blockLibrary.Get(id)?.Spec,
                ResolveWidget = tag => _descriptors.GetValueOrDefault(tag),
                ResolveWidgetBundle = tag => _widgetFiles.TryGetValue(tag, out var file) ? $"/{file.RelativePath}" : null,
            };

            var chrome = new StaticPageChrome
            {
                Lang = locale.Value,
                Title = DocumentTitle(page, locale),
                MetaDescription = MetaDescriptionOf(page, locale),
                CanonicalHref = Absolute(DirectoryPath(slugPath, locale)),
                Alternates = AlternatesOf(slugPath),
                Social = SocialCardFor(page, slugPath, locale),
                JsonLd = JsonLdFor(page, slugPath, locale),
                StylesheetHref = $"/{_cssFile}",
                SiteName = _siteName,
                HomeHref = HomeHref(locale),
                Nav = NavItemsFor(page.Id, locale),
                HeaderCta = HeaderLinkFor(_headerCta, locale),
                HeaderQuiet = HeaderLinkFor(_headerQuiet, locale),
                FooterGroups = FooterColumnsFor(locale),
                CopyLine = CopyLineFor(locale),
                FaviconUrl = _faviconUrl,
                LogoUrl = _logoUrl,
                LogoSvg = _logoSvg,
                // Exact by construction: WidgetView emits data-island precisely when
                // the tag has a descriptor AND ResolveWidgetBundle returns a URL —
                // the same condition, so no second render pass is needed.
                IncludeIslandLoader = _pageWidgetTags[page.Id].Any(_widgetFiles.ContainsKey),
            };

            return await RenderDocument(renderer, chrome, context, page.Tree.Roots, content: null, ct);
        }

        private async Task<string> RenderNotFound(HtmlRenderer renderer, CancellationToken ct)
        {
            var chrome = new StaticPageChrome
            {
                Lang = _defaultLocale.Value,
                Title = $"Page not found · {_siteName}",
                MetaDescription = null,
                CanonicalHref = null, // a 404 has no canonical URL
                Alternates = [],
                // Nothing here is a page: no share card, no structured data, and an
                // explicit refusal to be indexed — the one page that needs to say so.
                Social = null,
                JsonLd = [],
                RobotsDirective = "noindex, follow",
                StylesheetHref = $"/{_cssFile}",
                SiteName = _siteName,
                HomeHref = "/",
                Nav = NavItemsFor(currentPage: null, _defaultLocale),
                HeaderCta = HeaderLinkFor(_headerCta, _defaultLocale),
                HeaderQuiet = HeaderLinkFor(_headerQuiet, _defaultLocale),
                FooterGroups = FooterColumnsFor(_defaultLocale),
                CopyLine = CopyLineFor(_defaultLocale),
                FaviconUrl = _faviconUrl,
                LogoUrl = _logoUrl,
                LogoSvg = _logoSvg,
                IncludeIslandLoader = false,
            };

            RenderFragment body = builder =>
            {
                builder.OpenElement(0, "h1");
                builder.AddContent(1, "Page not found");
                builder.CloseElement();
                builder.OpenElement(2, "p");
                builder.OpenElement(3, "a");
                builder.AddAttribute(4, "href", "/");
                builder.AddContent(5, "Go to the front page");
                builder.CloseElement();
                builder.CloseElement();
            };

            return await RenderDocument(renderer, chrome, context: null, roots: [], body, ct);
        }

        private static async Task<string> RenderDocument(
            HtmlRenderer renderer,
            StaticPageChrome chrome,
            RenderContext? context,
            IReadOnlyList<Node> roots,
            RenderFragment? content,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var html = await renderer.Dispatcher.InvokeAsync(async () =>
            {
                var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
                {
                    [nameof(StaticPageDocument.Chrome)] = chrome,
                    [nameof(StaticPageDocument.Ctx)] = context,
                    [nameof(StaticPageDocument.Roots)] = roots,
                    [nameof(StaticPageDocument.Content)] = content,
                });
                var root = await renderer.RenderComponentAsync<StaticPageDocument>(parameters);
                return root.ToHtmlString();
            });

            // HtmlRenderer emits the component markup only; the doctype is ours.
            return "<!doctype html>\n" + html;
        }

        // -------------------------------------------------------------------- chrome

        /// <summary>
        /// Resolve a brand asset id to a single PUBLISHED <c>/assets/…</c> URL from the
        /// asset catalog (whose bytes CopyAssets writes into the output). A favicon prefers
        /// the smallest raster variant; a logo prefers a modest header-height variant (the
        /// second-smallest, or the only one). Null id or an unresolvable/unready asset yields
        /// null, so the caller emits no favicon / falls back to the brand dot.
        /// </summary>
        /// <remarks>
        /// Raster (PNG/WebP) is fully supported. Vector brand images are NOT yet: the catalog
        /// only ever INLINES SVGs (no standalone <c>assets/</c> file), so there is no file URL
        /// to reference from <c>&lt;link rel="icon"&gt;</c> / the header <c>&lt;img&gt;</c>.
        /// A vector favicon/logo therefore resolves to null (graceful brand-dot fallback) —
        /// emitting an SVG file variant for brand vectors is a tracked follow-up.
        /// </remarks>
        private string? BrandPublishedUrl(AssetId? assetId, bool preferSmallest)
        {
            if (assetId is not { } id || _assets.Resolve(id) is not { } info)
            {
                return null;
            }

            // Raster brand image: pick a small (favicon) / modest (logo) published variant.
            // ImageVariants are ordered smallest-first by the catalog.
            if (info.Kind == AssetKind.Image && info.ImageVariants.Count > 0)
            {
                var pick = preferSmallest
                    ? info.ImageVariants[0]
                    : info.ImageVariants[Math.Min(1, info.ImageVariants.Count - 1)];
                return pick.Url;
            }

            // Vector (inline-only, no file) or any non-raster kind: no published file URL.
            return null;
        }

        /// <summary>
        /// The header logo as sanitized inline SVG when the brand asset is a vector — the
        /// tracked follow-up the raster-only path above pointed at. Inlining is what lets a
        /// mark drawn in currentColor take the header's ink and follow the theme with no
        /// second rendition. The markup comes from the catalog's sanitize-and-recheck path,
        /// the same gate every inlined SvgNode passes.
        /// </summary>
        private string? BrandInlineSvg(AssetId? assetId) =>
            assetId is { } id && _assets.Resolve(id) is { Kind: AssetKind.Vector, InlineSvg: { Length: > 0 } svg }
                ? svg
                : null;

        /// <summary>
        /// The share card image, resolved to its LARGEST published variant — the opposite
        /// of the favicon's preference and the reason this is not just another
        /// <see cref="BrandPublishedUrl"/> call. Platforms want roughly 1200px wide and
        /// degrade to a text-only card below their minimum, so the widest render we have is
        /// always the right one. Vector and non-raster assets have no published file, so
        /// they resolve to null and no <c>og:image</c> is emitted at all.
        /// </summary>
        private string? SocialImagePublishedUrl(AssetId? assetId)
        {
            if (assetId is not { } id || _assets.Resolve(id) is not { } info)
            {
                return null;
            }

            // The unconverted upload when we asked for one: a WebP share card is silently
            // dropped by some scrapers, and a card that does not render is worth less than
            // a slightly larger PNG. Falls back to the widest variant if no original
            // shipped, which is still better than emitting nothing.
            if (info.OriginalUrl is { Length: > 0 } original)
            {
                return original;
            }

            // ImageVariants are ordered smallest-first by the catalog.
            return info is { Kind: AssetKind.Image, ImageVariants.Count: > 0 }
                ? info.ImageVariants[^1].Url
                : null;
        }

        /// <summary>The page's own title, without the site-name suffix the document title adds.</summary>
        private string PageTitle(PublishedPage page, Locale locale)
        {
            var title = page.MetaTitle.Resolve(locale, _defaultLocale);
            return title.Length > 0 ? title : page.Title.Resolve(locale, _defaultLocale);
        }

        private string DocumentTitle(PublishedPage page, Locale locale)
        {
            var title = PageTitle(page, locale);
            if (title.Length == 0 || string.Equals(title, _siteName, StringComparison.OrdinalIgnoreCase))
            {
                // "Canine Blog · Canine Blog" — what a blog site's index produced, because its
                // heading IS the site's name. A page named after its site is titled once.
                return _siteName;
            }

            return $"{title} · {_siteName}";
        }

        /// <summary>
        /// What a social platform or a model sees when handed the URL instead of the page.
        /// Derived from the same title and description the search snippet uses — the two
        /// describe one page, so authoring them separately only creates a way to disagree.
        /// </summary>
        private StaticPageChrome.SocialCard SocialCardFor(PublishedPage page, string slugPath, Locale locale)
        {
            var title = PageTitle(page, locale);
            return new StaticPageChrome.SocialCard(
                Title: title.Length > 0 ? title : _siteName,
                Description: MetaDescriptionOf(page, locale),
                // Open Graph has no base to resolve a relative reference against, so
                // without a configured origin an absent url beats an unusable one.
                Url: _baseUrl is null ? null : Absolute(DirectoryPath(slugPath, locale)),
                Type: "website",
                SiteName: _siteName,
                // Absolute or absent, for the same reason as the url above. The header logo
                // is deliberately never used as a stand-in: it is the wrong shape, and a
                // platform rejects it rather than cropping, which shows a broken card where
                // a clean text-only one would have done.
                ImageUrl: _baseUrl is null || _socialImageUrl is null ? null : Absolute(_socialImageUrl),
                Locale: OpenGraphLocale(locale));
        }

        /// <summary>
        /// og:locale is language_TERRITORY, not a bare language tag. A locale with no
        /// region cannot be expressed in that form, so it goes unstated rather than wrong.
        /// </summary>
        private static string? OpenGraphLocale(Locale locale) =>
            locale.Value.Split('-') is [{ Length: > 0 } language, { Length: > 0 } region]
                ? $"{language}_{region.ToUpperInvariant()}"
                : null;

        private IReadOnlyList<string> JsonLdFor(PublishedPage page, string slugPath, Locale locale)
        {
            var title = PageTitle(page, locale);
            return
            [
                StructuredData.PageGraph(
                    siteName: _siteName,
                    lang: locale.Value,
                    pageUrl: Absolute(DirectoryPath(slugPath, locale)),
                    homeUrl: Absolute(HomeHref(locale)),
                    title: title.Length > 0 ? title : _siteName,
                    description: MetaDescriptionOf(page, locale),
                    logoUrl: _logoUrl is null ? null : Absolute(_logoUrl),
                    isHome: HomePageId() == page.Id,
                    trail: TrailFor(slugPath, locale)),
            ];
        }

        /// <summary>
        /// The ancestor pages leading to this one, for the breadcrumb. Built from the slug
        /// path, and an ancestor segment that no page occupies is skipped rather than
        /// invented — a breadcrumb that names a page you cannot open is a broken promise.
        /// </summary>
        private IReadOnlyList<(string Name, string Url)> TrailFor(string slugPath, Locale locale)
        {
            if (slugPath.Length == 0)
            {
                return [];
            }

            var trail = new List<(string, string)> { (_siteName, Absolute(HomeHref(locale))) };
            var segments = slugPath.Split('/');
            var prefix = string.Empty;
            for (var i = 0; i < segments.Length - 1; i++)
            {
                prefix = prefix.Length == 0 ? segments[i] : $"{prefix}/{segments[i]}";
                if (PageAt(prefix) is { } ancestor)
                {
                    trail.Add((PageTitle(ancestor, locale), Absolute(DirectoryPath(prefix, locale))));
                }
            }

            return trail;
        }

        private Dictionary<string, PublishedPage>? _pageAtPath;

        // Claimed paths are unique by construction (ClaimPaths resolves collisions), so the
        // inverse of _slugPathOf is a function, not a multimap.
        private PublishedPage? PageAt(string slugPath) =>
            (_pageAtPath ??= _slugPathOf
                .Where(entry => _pageById.ContainsKey(entry.Key))
                .ToDictionary(entry => entry.Value, entry => _pageById[entry.Key]))
            .GetValueOrDefault(slugPath);

        private string? MetaDescriptionOf(PublishedPage page, Locale locale)
        {
            var description = page.MetaDescription.Resolve(locale, _defaultLocale);
            if (description.Length == 0)
            {
                // Fallback per the contract: better a title-shaped description than none.
                description = page.Title.Resolve(locale, _defaultLocale);
            }

            return description.Length > 0 ? description : null;
        }

        private IReadOnlyList<StaticPageChrome.Alternate> AlternatesOf(string slugPath) =>
        [
            .. _locales.Select(locale => new StaticPageChrome.Alternate(locale.Value, Absolute(DirectoryPath(slugPath, locale)))),
            new StaticPageChrome.Alternate("x-default", Absolute(DirectoryPath(slugPath, _defaultLocale))),
        ];

        private IReadOnlyList<StaticPageChrome.NavItem> NavItemsFor(PageId? currentPage, Locale locale)
        {
            var items = new List<StaticPageChrome.NavItem>();
            foreach (var item in _navigation)
            {
                if (item.IsGroup)
                {
                    var children = new List<StaticPageChrome.NavChild>();
                    foreach (var child in item.Children)
                    {
                        if (ResolveNavLink(child.Link, child.Label, locale) is not { } resolved)
                        {
                            continue; // an unpublished/collision-losing page child: absence beats a dead link
                        }

                        var (href, label, page) = resolved;
                        var description = child.Description?.Resolve(locale, _defaultLocale);
                        children.Add(new StaticPageChrome.NavChild(
                            label, href, string.IsNullOrEmpty(description) ? null : description, page == currentPage));
                    }

                    // A group whose every child dropped out is not rendered — an empty
                    // dropdown is worse than no menu.
                    if (children.Count == 0)
                    {
                        continue;
                    }

                    var groupLabel = item.Label?.Resolve(locale, _defaultLocale) ?? string.Empty;
                    items.Add(new StaticPageChrome.NavItem(groupLabel, Href: null, IsCurrent: false, children));
                    continue;
                }

                // Direct link. Unpublished, deleted or collision-losing page targets are
                // skipped: absence beats a dead link, and the editor shows why. (Render-
                // failed pages are NOT skipped — failures surface mid-loop and skipping
                // them would make sibling pages' markup depend on render order.)
                if (ResolveNavLink(item.Link, item.Label, locale) is not { } direct)
                {
                    continue;
                }

                var (directHref, directLabel, directPage) = direct;
                items.Add(new StaticPageChrome.NavItem(
                    directLabel, directHref, directPage == currentPage, Children: []));
            }

            return items;
        }

        /// <summary>
        /// Resolve a navigation/footer <see cref="Link"/> to a concrete href + label for a
        /// locale. A same-site page link yields its published directory path and its title
        /// (label override wins); an unpublished/collision-losing page link yields null so
        /// the caller drops it. An external link passes through verbatim with its label.
        /// </summary>
        private (string Href, string Label, PageId? Page)? ResolveNavLink(Link? link, LocalizedText? labelOverride, Locale locale)
        {
            switch (link)
            {
                case PageLink page:
                    if (!_slugPathOf.TryGetValue(page.PageId, out var slugPath))
                    {
                        return null;
                    }

                    var label = labelOverride?.Resolve(locale, _defaultLocale);
                    if (string.IsNullOrEmpty(label))
                    {
                        label = _pageById[page.PageId].Title.Resolve(locale, _defaultLocale);
                    }

                    // A section link is not the page: on the front page itself, "Independence"
                    // alongside "Home" must not both read as where you are, so only the
                    // whole-page link claims aria-current.
                    return (page.Href(DirectoryPath(slugPath, locale))!, label,
                        page.Fragment is null ? page.PageId : null);

                case ExternalLink external:
                    // The aggregate guarantees an external link carries a label.
                    return (external.Url, labelOverride?.Resolve(locale, _defaultLocale) ?? external.Url, null);

                default:
                    return null;
            }
        }

        private StaticPageChrome.HeaderLink? HeaderLinkFor(HeaderAction? action, Locale locale) =>
            action is null || ResolveNavLink(action.Link, action.Label, locale) is not { } resolved
                ? null
                : new StaticPageChrome.HeaderLink(resolved.Label, resolved.Href);

        private IReadOnlyList<StaticPageChrome.FooterColumn> FooterColumnsFor(Locale locale)
        {
            var columns = new List<StaticPageChrome.FooterColumn>();
            foreach (var group in _footerGroups)
            {
                var links = new List<StaticPageChrome.FooterEntry>();
                foreach (var link in group.Links)
                {
                    if (ResolveNavLink(link.Link, link.Label, locale) is { } resolved)
                    {
                        links.Add(new StaticPageChrome.FooterEntry(resolved.Label, resolved.Href));
                    }
                }

                // A column whose every link dropped out is omitted (all its targets gone).
                if (links.Count > 0)
                {
                    columns.Add(new StaticPageChrome.FooterColumn(group.Heading.Resolve(locale, _defaultLocale), links));
                }
            }

            return columns;
        }

        private string? CopyLineFor(Locale locale)
        {
            var copy = _copyLine?.Text.Resolve(locale, _defaultLocale);
            return string.IsNullOrEmpty(copy) ? null : copy;
        }

        private string HomeHref(Locale locale) =>
            HomePageId() is { } home && _slugPathOf.TryGetValue(home, out var slugPath)
                ? DirectoryPath(slugPath, locale)
                : "/";

        // --------------------------------------------------------------------- paths

        /// <summary>Public directory path of a page in a locale: <c>/</c>, <c>/about/</c>, <c>/da/</c>, <c>/da/about/</c>.</summary>
        private string DirectoryPath(string slugPath, Locale locale) =>
            locale == _defaultLocale
                ? slugPath.Length == 0 ? "/" : $"/{slugPath}/"
                : slugPath.Length == 0
                    ? $"/{locale.UrlSegment}/"
                    : $"/{locale.UrlSegment}/{slugPath}/";

        private IReadOnlyList<string> PathsOf(string slugPath) =>
        [
            DirectoryPath(slugPath, _defaultLocale),
            .. _locales.Where(locale => locale != _defaultLocale).Select(locale => DirectoryPath(slugPath, locale)),
        ];

        private static string IndexFileOf(string directoryPath)
        {
            var trimmed = directoryPath.Trim('/');
            return trimmed.Length == 0 ? "index.html" : $"{trimmed}/index.html";
        }

        private string Absolute(string path) => _baseUrl is null ? path : _baseUrl + path;

        private string FullPath(string relative) => Path.Combine(_outputRoot, relative);

        // ---------------------------------------------------------------------- tree

        /// <summary>All nodes of a page including the content of placed blocks (resolved live, like rendering does).</summary>
        private IEnumerable<Node> NodesOf(PublishedPage page)
        {
            foreach (var node in page.Tree.All())
            {
                yield return node;
                if (node is BlockInstanceNode instance && blockLibrary.Get(instance.DefinitionId) is { } definition)
                {
                    // Overrides only rewrite text, so the definition's own asset and
                    // widget references are exactly what the instance renders.
                    foreach (var inner in PageTree.Flatten(definition.Spec))
                    {
                        yield return inner;
                    }
                }
            }
        }

        // Every asset a node makes the published page depend on. Media nodes carry theirs as
        // a prop; a button or a prose anchor carries it as an asset LINK — and collecting
        // those here is exactly what makes the linked file exist in the deploy output.
        private static IEnumerable<AssetId> AssetReferencesOf(Node node)
        {
            switch (node)
            {
                case ImageNode { AssetId: { } image }:
                    yield return image;
                    break;
                case VideoNode { AssetId: { } video }:
                    yield return video;
                    break;
                case SvgNode { AssetId: { } svg }:
                    yield return svg;
                    break;
                case ButtonNode { LinkTo: AssetLink button }:
                    yield return button.AssetId;
                    break;
                case RichTextNode richText:
                    foreach (var (_, html) in richText.Html.Values)
                    {
                        foreach (var id in RichTextHtml.AssetReferences(html))
                        {
                            yield return id;
                        }
                    }

                    break;
            }
        }

        private static System.Text.Json.JsonSerializerOptions CreateBlockJson()
        {
            var options = new System.Text.Json.JsonSerializerOptions();
            options.Converters.Add(new EventSourcing.GuidIdJsonConverterFactory());
            Authoring.AuthoringJson.Configure(options);
            return options;
        }

        /// <summary>
        /// The cross-aggregate dependency tokens of a page (docs/publishing.md staleness):
        /// the resolved path of every page it links to, and a content hash of every block
        /// definition it instances. A change in either invalidates the page even though
        /// its own version and the chrome version did not move.
        /// </summary>
        private IReadOnlyList<string> DependencyTokensOf(PublishedPage page)
        {
            var tokens = new SortedSet<string>(StringComparer.Ordinal);

            // A syndicated page has no aggregate and therefore no version to compare — it is created with
            // PublishedVersion 0 and stays there, so "old.PublishedVersion < page.PublishedVersion" is
            // 0 < 0 for its whole life. Without this token such a page renders ONCE, when it first appears,
            // and every later push is stored and never re-rendered: the producer reports "955 changed" and
            // the site keeps serving the first version until something site-wide (a chrome edit, a CSS or
            // renderer change) happens to re-render everything. Its content hash IS its version.
            if (_syndicatedHashes.TryGetValue(page.Id, out var contentHash))
            {
                tokens.Add($"syndicated:{contentHash}");
            }

            // Exactly the same problem, for the same reason: a post page is created with
            // PublishedVersion 0 and stays there, so the version comparison is 0 < 0 for its whole
            // life. Without this token a post renders once and every later re-publish is stored and
            // never re-rendered — the editor would say "Published" over a page still showing the
            // first draft. The publish instant is its version.
            if (_postStamps.TryGetValue(page.Id, out var stamp))
            {
                tokens.Add($"post:{stamp}");
            }

            foreach (var node in NodesOf(page))
            {
                switch (node)
                {
                    case ButtonNode { LinkTo: PageLink link }:
                        tokens.Add(PageLinkToken(link.PageId));
                        break;

                    case RichTextNode richText:
                        foreach (var (_, html) in richText.Html.Values)
                        {
                            foreach (var linkedId in PageRefsIn(html))
                            {
                                tokens.Add(PageLinkToken(linkedId));
                            }
                        }

                        break;

                    case BlockInstanceNode instance when blockLibrary.Get(instance.DefinitionId) is { } definition:
                        tokens.Add($"block:{instance.DefinitionId.Compact}={BlockContentHash(instance.DefinitionId, definition.Spec)}");
                        break;
                }
            }

            return [.. tokens];
        }

        // A linked page contributes the path it resolves to (or a marker when it is not
        // a published owner) — so a slug change or an unpublish flips the token.
        private string PageLinkToken(PageId linkedId) =>
            $"page:{linkedId.Compact}={(_slugPathOf.TryGetValue(linkedId, out var path) ? path : "·unpublished")}";

        private string BlockContentHash(BlockDefinitionId id, Node spec)
        {
            if (!_blockHashes.TryGetValue(id, out var hash))
            {
                hash = Hashing.Hash16(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(spec, BlockJson));
                _blockHashes[id] = hash;
            }

            return hash;
        }

        // Canonical rich text carries page references as href="page:{guid}"; the guid may
        // be dashed or compact ("N"). Extracted so a linked page's slug move re-renders
        // the linking page, not just button links.
        // Case-insensitive on both attribute and scheme, and permissive on the guid
        // shape, because CanonicalHtml.IsAllowedHref accepts page: in any case and any
        // Guid.TryParse-able form. A stricter regex here would silently miss references
        // and leave the linking page un-invalidated — the dead-link bug this guards.
        private static readonly System.Text.RegularExpressions.Regex PageRefPattern =
            new("href=\"page:([0-9a-fA-F{}()-]{32,38})\"",
                System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        private static IEnumerable<PageId> PageRefsIn(string html)
        {
            foreach (System.Text.RegularExpressions.Match match in PageRefPattern.Matches(html))
            {
                if (Guid.TryParse(match.Groups[1].Value, out var guid))
                {
                    yield return PageId.From(guid);
                }
            }
        }

        // ------------------------------------------------------------------- outputs

        private async Task LoadWidgetBundles(CancellationToken ct)
        {
            var usedTags = _pageWidgetTags.Values.SelectMany(tags => tags).Distinct().Order(StringComparer.Ordinal);
            foreach (var tag in usedTags)
            {
                if (!_descriptors.TryGetValue(tag, out var descriptor))
                {
                    continue; // unknown widget: the static output simply omits it
                }

                byte[] bytes;
                if (_builtInWidgetTags.Contains(tag))
                {
                    // Built-in: copy the bundle file from the widgets directory, as always.
                    var source = Path.Combine(options.WidgetsDirectory, descriptor.Bundle);
                    if (!File.Exists(source))
                    {
                        logger.LogWarning("Widget '{Tag}' has no bundle at {Path}; pages render its fallback content only.", tag, source);
                        continue;
                    }

                    bytes = await File.ReadAllBytesAsync(source, ct);
                }
                else if (widgetRegistry.BundleOf(tag) is { } approvedSource)
                {
                    // Approved submission: the reviewed source lives in the immutable event
                    // log, not on disk. Write those exact bytes as the bundle — from here on
                    // (_widgetFiles, the manifest widgetBundles, island hydration) it is
                    // indistinguishable from a copied built-in bundle.
                    bytes = Encoding.UTF8.GetBytes(approvedSource);
                }
                else
                {
                    continue; // a descriptor with no source (approval withdrawn mid-pass): omit
                }

                var hash = Hashing.Hash16(bytes);
                _widgetFiles[tag] = ($"widgets/{tag}.{hash}.js", hash, bytes);
            }
        }

        private async Task CopyWidgetBundles(CancellationToken ct)
        {
            foreach (var (_, file) in _widgetFiles)
            {
                if (!File.Exists(FullPath(file.RelativePath)))
                {
                    await WriteIfChanged(file.RelativePath, file.Bytes, ct);
                }
            }
        }

        /// <summary>
        /// The self-hosted marketing fonts, written at their fixed <c>/fonts/*.woff2</c>
        /// paths (the marketing stylesheet references them literally). WriteIfChanged keeps
        /// the zero-rewrite guarantee — an unchanged file is left untouched.
        /// </summary>
        private async Task CopyFonts(CancellationToken ct)
        {
            foreach (var font in FontAssets.All)
            {
                await WriteIfChanged(font.RelativePath, font.Bytes, ct);
            }
        }

        private async Task CopyAssets(CancellationToken ct)
        {
            foreach (var file in _assets.Files.DistinctBy(file => file.RelativePath))
            {
                var full = FullPath(file.RelativePath);
                if (File.Exists(full))
                {
                    // The name embeds the content hash — existence proves content.
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(full)!);
                await using var source = await mediaStore.Open(file.StorageKey, ct);
                long length;
                await using (var target = File.Create(full))
                {
                    await source.CopyToAsync(target, ct);
                    length = target.Length;
                }

                _filesWritten++;
                _bytesWritten += length;
            }
        }

        private string BuildSitemap(List<PagePlan> plans)
        {
            var xml = new StringBuilder(1024);
            xml.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
            xml.Append("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\" xmlns:xhtml=\"http://www.w3.org/1999/xhtml\">\n");
            foreach (var plan in plans)
            {
                if (plan.SlugPath is null || _failed.Contains(plan.Page.Id))
                {
                    continue;
                }

                var alternates = AlternatesOf(plan.SlugPath);
                foreach (var locale in _locales)
                {
                    xml.Append("  <url>\n    <loc>").Append(XmlEscape(Absolute(DirectoryPath(plan.SlugPath, locale)))).Append("</loc>\n");
                    foreach (var alternate in alternates)
                    {
                        xml.Append("    <xhtml:link rel=\"alternate\" hreflang=\"").Append(XmlEscape(alternate.Hreflang))
                            .Append("\" href=\"").Append(XmlEscape(alternate.Href)).Append("\" />\n");
                    }

                    xml.Append("  </url>\n");
                }
            }

            xml.Append("</urlset>\n");
            return xml.ToString();
        }

        /// <summary>
        /// The syndicated pages of this site, as pages the rest of the pass cannot tell apart from
        /// authored ones.
        /// </summary>
        /// <remarks>
        /// The page id is derived from the site and path rather than minted, so it is the SAME id on
        /// every pass. The manifest is keyed by page id, so a fresh id each run would look like the
        /// old page vanishing and a new one appearing — republishing everything, sweeping files, and
        /// churning the output for content that never changed.
        /// <para>
        /// A syndicated page has no publish version to compare, so its content hash takes that role:
        /// it moves when the producer sends something different, and only then.
        /// </para>
        /// </remarks>
        private readonly Dictionary<PageId, string> _syndicatedHashes = [];

        // The three shapes of a blog's public paths now live in BlogPaths, because the EDITOR
        // needs the same answer to offer a working preview link — and got it wrong the moment a
        // blog site's posts moved to its root.
        private string PostPrefix => BlogPaths.PostPrefix(_siteKind);

        private string IndexPath => BlogPaths.IndexPath(_siteKind);

        private string IndexHref => BlogPaths.IndexHref(_siteKind);

        private IReadOnlyList<PublishedPost> _posts = [];
        private readonly Dictionary<PageId, string> _postStamps = [];

        /// <summary>
        /// Published posts, as pages. They join the ordinary page set and are then
        /// indistinguishable to everything downstream — same views, same chrome, same sitemap,
        /// same staleness sweep — for the reason syndicated pages are: there is only one
        /// renderer, so anything given to it learns everything it knows.
        /// <para>Each locale's tree is its own page, because a post's translation is written
        /// independently and has no node-for-node correspondence to hang localized text off
        /// (see <see cref="PublishedPost.RootsByLocale"/>). The default locale keeps the bare
        /// path; the others take their locale prefix, exactly as an authored page's translations
        /// do.</para>
        /// </summary>
        private IEnumerable<PublishedPage> PostPagesOf()
        {
            foreach (var post in _posts)
            {
                _postStamps[PostPageId(post.Id)] = post.UpdatedAt.UtcTicks.ToString(System.Globalization.CultureInfo.InvariantCulture);
                yield return new PublishedPage(
                    PostPageId(post.Id),
                    post.SiteId,
                    default,   // addressed by PublicPath, like a syndicated page
                    post.Title,
                    post.MetaTitle,
                    post.MetaDescription,
                    new PageTree(WithDateline(post, post.RootsFor(_defaultLocale, _defaultLocale))),
                    PublishedVersion: 0)
                {
                    PublicPath = $"{PostPrefix}{post.Slug.Value}",
                };
            }

            // A blog site ALWAYS has an index, empty or not: it is the site's root, and a root that
            // 404s is not an empty blog, it is a broken domain. Inside an ordinary site the old rule
            // still holds — see ListingPage.
            if (_posts.Count > 0 || _siteKind == SiteKind.Blog)
            {
                var listing = ListingPage();
                // The index is derived from every post, so its version is all of theirs: adding,
                // withdrawing or re-publishing any one of them must re-render it. The count is in
                // the stamp too, so going from one post to none re-renders it into its empty state
                // instead of leaving the last post listed on a blog that no longer has it.
                _postStamps[listing.Id] = $"{_posts.Count}:" + string.Join(
                    '|', _posts.Select(post => $"{post.Slug.Value}:{post.UpdatedAt.UtcTicks}"));
                yield return listing;
            }
        }

        /// <summary>
        /// The blog index, built as an ordinary page out of ordinary nodes — a masthead and one
        /// rich-text entry per post. Generating a NODE TREE rather than markup is what keeps it
        /// inside the system: it gets the site's chrome, tokens, dark mode and stylesheet for free,
        /// and there is still exactly one renderer.
        /// <para>Inside an ordinary site it is rendered only when a post exists: an empty
        /// <c>/blog/</c> hanging off a marketing site is a promise of content that isn't there, and
        /// a link in the sitemap to it is worse. A blog SITE is the opposite case — the index is
        /// its root, so it is always rendered and says plainly that nothing has been published
        /// yet. A reader who typed the domain gets an answer either way; the failure mode that
        /// matters is a bare 404 on a domain someone just announced.</para>
        /// </summary>
        private PublishedPage ListingPage()
        {
            // On a blog site the masthead IS the site's name — a heading reading "Blog" on
            // blog.canine.dev tells the reader something they knew before they arrived.
            var isBlogSite = _siteKind == SiteKind.Blog;
            var heading = new HeadingNode
            {
                Id = ListingNodeId(PostId.From(Guid.Empty)),
                Level = 1,
                Text = LocalizedText.Of(_defaultLocale, isBlogSite ? _siteName : "Blog"),
            };

            var body = new List<Node> { heading };

            if (_posts.Count == 0)
            {
                // The empty state. Deliberately not an apology, and with no link to the feed: the
                // feed is only written when there is something to syndicate, so pointing at it
                // here would make the empty state's one link a 404 — a worse first impression than
                // the emptiness it is trying to dress up.
                body.Add(new RichTextNode
                {
                    Id = ListingNodeId(PostId.From(new Guid("00000000-0000-0000-0000-0000000e0000"))),
                    Html = LocalizedText.Of(_defaultLocale, "<p>No posts published yet.</p>"),
                });
            }

            foreach (var post in _posts)
            {
                var title = HtmlText.Encode(post.Title.Resolve(_defaultLocale, _defaultLocale));
                var summary = post.MetaDescription.Resolve(_defaultLocale, _defaultLocale);
                // Absolute() so the href is a scheme the canonical inline grammar accepts. This
                // html is generated rather than stored, so no validator sees it — which is exactly
                // why it must not be the one place that quietly stops obeying the grammar.
                var href = HtmlText.Encode(Absolute($"/{PostPrefix}{post.Slug.Value}/"));
                // The reader's date, matching the dateline on the post itself — the index and the
                // page disagreeing about when something was written is the kind of small
                // contradiction that costs a reader their trust in both.
                var date = HtmlText.Encode(EditorialTime.ForReader(post.PublishedAt));
                var line = $"""<p><a href="{href}">{title}</a></p><p>{date}</p>""";
                if (!string.IsNullOrWhiteSpace(summary))
                {
                    line += $"<p>{HtmlText.Encode(summary)}</p>";
                }

                body.Add(new RichTextNode
                {
                    Id = ListingNodeId(post.Id),
                    Html = LocalizedText.Of(_defaultLocale, line),
                });
            }

            var section = new SectionNode
            {
                Id = ListingNodeId(PostId.From(new Guid("00000000-0000-0000-0000-00000000feed"))),
                Appearance = SectionAppearance.Doc,
                Children = NodeList.Of(body),
            };

            return new PublishedPage(
                PostPageId(PostId.From(new Guid("00000000-0000-0000-0000-0000000000b1"))),
                site.Id,
                default,
                LocalizedText.Of(_defaultLocale, isBlogSite ? _siteName : "Blog"),
                LocalizedText.Empty,
                LocalizedText.Empty,
                new PageTree(NodeList.Of(section)),
                PublishedVersion: 0)
            {
                PublicPath = IndexPath,
            };
        }

        /// <summary>
        /// The post's own date, above its first line. A dated stream of writing that does not say
        /// when anything was written is asking the reader to take its word for how current it is —
        /// and the index has always shown a date, so the page not showing one made the two
        /// disagree. Generated as a NODE, like the index itself, so it gets the site's typography.
        /// </summary>
        private NodeList WithDateline(PublishedPost post, NodeList body)
        {
            var line = HtmlText.Encode(EditorialTime.ForReader(post.PublishedAt));
            var dateline = new RichTextNode
            {
                Id = ListingNodeId(post.Id),
                Html = LocalizedText.Of(_defaultLocale, $"<p>{line}</p>"),
            };

            return NodeList.Of([dateline, .. body]);
        }

        /// <summary>Deterministic node ids for the generated index: the same posts must produce
        /// byte-identical markup on every pass, and a fresh Guid would rewrite the file each time.</summary>
        private static NodeId ListingNodeId(PostId post)
        {
            var seed = System.Security.Cryptography.SHA256.HashData(
                Encoding.UTF8.GetBytes($"blog-index:{post.Compact}"));
            return NodeId.From(new Guid(seed.AsSpan(0, 16)));
        }

        /// <summary>
        /// RSS 2.0 for the published posts. A feed is the one part of a blog a reader takes away
        /// with them, so it carries absolute links and the post's own publication date.
        /// <para>Descriptions only, never the body: an escaped HTML body in a feed would be a
        /// SECOND rendering of the post, drifting from the page — the failure this whole module is
        /// built to avoid. The feed says what a post is and links to the one rendering there is.</para>
        /// </summary>
        private string BuildFeed()
        {
            var xml = new StringBuilder();
            xml.Append("""<?xml version="1.0" encoding="utf-8"?>""").Append('\n');
            xml.Append("<rss version=\"2.0\"><channel>\n");
            xml.Append("<title>").Append(XmlEscape(_siteName)).Append("</title>\n");
            xml.Append("<link>").Append(XmlEscape(Absolute(IndexHref))).Append("</link>\n");
            xml.Append("<description>").Append(XmlEscape(_siteName)).Append("</description>\n");

            foreach (var post in _posts)
            {
                var url = Absolute($"/{PostPrefix}{post.Slug.Value}/");
                xml.Append("<item>\n");
                xml.Append("<title>").Append(XmlEscape(post.Title.Resolve(_defaultLocale, _defaultLocale))).Append("</title>\n");
                xml.Append("<link>").Append(XmlEscape(url)).Append("</link>\n");
                // The URL is the identity: it is stable, unique, and the thing a reader would
                // follow, so a reader that has already seen it recognises it after a re-publish.
                xml.Append("<guid isPermaLink=\"true\">").Append(XmlEscape(url)).Append("</guid>\n");
                xml.Append("<pubDate>").Append(post.PublishedAt.ToUniversalTime().ToString("r", System.Globalization.CultureInfo.InvariantCulture)).Append("</pubDate>\n");
                if (post.MetaDescription.Resolve(_defaultLocale, _defaultLocale) is { Length: > 0 } description)
                {
                    xml.Append("<description>").Append(XmlEscape(description)).Append("</description>\n");
                }

                xml.Append("</item>\n");
            }

            xml.Append("</channel></rss>\n");
            return xml.ToString();
        }

        /// <summary>A stable page id for a post — the same post always names the same page, so the
        /// manifest can see it change rather than treating every pass as a new file.</summary>
        private static PageId PostPageId(PostId id)
        {
            var seed = System.Security.Cryptography.SHA256.HashData(
                Encoding.UTF8.GetBytes($"post:{id.Compact}"));
            return PageId.From(new Guid(seed.AsSpan(0, 16)));
        }

        private IEnumerable<PublishedPage> SyndicatedPagesOf(SiteId siteId) =>
            syndicated.AllForSite(siteId).Select(page => Remember(siteId, page)).Select(page => new PublishedPage(
                SyndicatedPageId(siteId, page.Path),
                siteId,
                default,   // a syndicated page is addressed by PublicPath; it has no editor-typed slug
                page.Title,
                page.MetaTitle,
                page.MetaDescription,
                new PageTree(NodeList.Of([page.Node])),
                PublishedVersion: 0)
            {
                PublicPath = page.Path,
            });

        /// <summary>Records a syndicated page's content hash so staleness can see it change.</summary>
        private SyndicatedPage Remember(SiteId siteId, SyndicatedPage page)
        {
            _syndicatedHashes[SyndicatedPageId(siteId, page.Path)] = page.ContentHash;
            return page;
        }

        /// <summary>A stable id for a page that has no aggregate: the same site and path always name it.</summary>
        private static PageId SyndicatedPageId(SiteId siteId, string path)
        {
            var seed = System.Security.Cryptography.SHA256.HashData(
                Encoding.UTF8.GetBytes($"syndicated:{siteId.Compact}:{path}"));
            return PageId.From(new Guid(seed.AsSpan(0, 16)));
        }

        private string BuildRobots() =>
            $"User-agent: *\nAllow: /\n\nSitemap: {Absolute("/sitemap.xml")}\n";

        /// <summary>
        /// The llms.txt index: what this site is, and every page with the words its own author gave
        /// it. Written for a language model deciding whether it can describe us accurately.
        /// </summary>
        /// <remarks>
        /// Generated, not authored, and that is the whole point. The file this replaces was a loose
        /// text file on the server, edited by hand in July and never again: it still told models the
        /// product was C#/.NET only, long after it measured thirteen languages. Nobody could see it
        /// drift, because it was not in any repository and nothing compared it to the site.
        /// <para>
        /// Deriving it from the same titles and meta descriptions the pages render means it cannot
        /// say something the site does not. It goes stale only if the site does, and then it is
        /// wrong in the same way — which is a problem you can see.
        /// </para>
        /// <para>
        /// Only the default locale is listed. A model asking "what is this" needs one clear answer,
        /// not the same answer in every published language.
        /// </para>
        /// </remarks>
        private string BuildLlmsTxt(List<PagePlan> plans)
        {
            var home = plans.Find(p => p.SlugPath is { Length: 0 } || p.SlugPath == string.Empty)
                       ?? plans.Find(p => p.SlugPath is not null);

            var text = new StringBuilder(2048);

            // A site that has written its own account of itself says something a page list
            // cannot: what it IS, not merely which pages exist. When it has, that stands in
            // for the generated header entirely.
            if (_llmsPreamble is { Length: > 0 } preamble)
            {
                text.Append(preamble.TrimEnd()).Append('\n');
            }
            else
            {
                text.Append("# ").Append(_siteName).Append('\n');
                if (home is not null && MetaDescriptionOf(home.Page, _defaultLocale) is { Length: > 0 } summary)
                {
                    text.Append("\n> ").Append(summary).Append('\n');
                }
            }

            var (listable, excluded) = LlmsPages(plans);

            // The index says which pages exist; a model that wants the words themselves
            // should not have to fetch them one page at a time. Stated here because this
            // is the file it already knows to look for.
            text.Append("\n> The full text of every page is at ")
                .Append(Absolute("/llms-full.txt")).Append(".\n");

            AppendExclusionNote(text, excluded);

            text.Append("\n## Pages\n\n");
            foreach (var plan in listable.Take(MaxLlmsPages))
            {
                text.Append("- [").Append(DocumentTitle(plan.Page, _defaultLocale)).Append("](")
                    .Append(Absolute(DirectoryPath(plan.SlugPath!, _defaultLocale))).Append(')');

                if (MetaDescriptionOf(plan.Page, _defaultLocale) is { Length: > 0 } description)
                {
                    text.Append(": ").Append(description);
                }

                text.Append('\n');
            }

            // llms.txt is read WHOLE by a model, so a site that publishes a large generated
            // corpus would otherwise ship half a megabyte of near-identical lines and get
            // truncated or skipped — which loses the curated part at the top too. The list
            // is bounded, and the omission is stated rather than silent: a file that stops
            // early without saying so reads as "this is everything".
            if (listable.Count > MaxLlmsPages)
            {
                text.Append("\n> ").Append(listable.Count - MaxLlmsPages)
                    .Append(" further pages are not listed here. The complete set is in ")
                    .Append(Absolute("/sitemap.xml")).Append(".\n");
            }

            return text.ToString();
        }

        /// <summary>
        /// The llms-full.txt corpus: every published page's prose, in one file, in the order
        /// the site itself puts them in. What llms.txt promises, delivered.
        /// </summary>
        /// <remarks>
        /// llms.txt describes the site from the outside — titles and meta descriptions, which
        /// are marketing summaries of pages, not the pages. A model handed only that can say
        /// what we claim to be but cannot answer a question the body text answers, so it fetches
        /// fifty URLs or, more often, guesses. One file removes the choice.
        /// <para>
        /// The prose comes from the page TREE, not from the rendered HTML on disk: rendering is
        /// skipped for pages that are not stale, so the HTML is not in memory here, and reading
        /// it back would mean parsing chrome out of every document on every pass. The tree is
        /// already loaded, and it is the same source the renderer reads — including block
        /// instances, which are resolved through <see cref="OverrideApplier"/> exactly as
        /// <c>BlockInstanceView</c> resolves them, so a placed block contributes the words the
        /// page actually shows rather than the definition's originals.
        /// </para>
        /// <para>
        /// Default locale only, for the reason llms.txt lists one locale: a model asking what
        /// this site says needs one answer, not the same answer in every published language.
        /// </para>
        /// </remarks>
        private string BuildLlmsFullTxt(List<PagePlan> plans)
        {
            var text = new StringBuilder(64 * 1024);

            if (_llmsPreamble is { Length: > 0 } preamble)
            {
                text.Append(preamble.TrimEnd()).Append('\n');
            }
            else
            {
                text.Append("# ").Append(_siteName).Append('\n');
            }

            var (listable, excluded) = LlmsPages(plans);
            AppendExclusionNote(text, excluded);

            var included = 0;
            foreach (var plan in listable)
            {
                // Built aside so the budget decides on the finished section: appending first
                // and trimming after would leave a page cut mid-sentence, which reads as if
                // the site said something it did not.
                var title = PageTitle(plan.Page, _defaultLocale);
                var section = new StringBuilder(4096);
                section.Append("\n---\n\n## ").Append(title).Append('\n')
                    .Append('\n').Append(Absolute(DirectoryPath(plan.SlugPath!, _defaultLocale))).Append('\n');

                // MetaDescriptionOf falls back to the title when a page has no description.
                // In the index that fallback is better than an empty line; here it would
                // print the heading again, one line below itself.
                if (MetaDescriptionOf(plan.Page, _defaultLocale) is { Length: > 0 } description
                    && !string.Equals(description, title, StringComparison.Ordinal))
                {
                    section.Append('\n').Append(description).Append('\n');
                }

                AppendProse(section, plan.Page);

                if (text.Length + section.Length > MaxLlmsFullChars)
                {
                    break;
                }

                text.Append(section);
                included++;
            }

            // Same contract as llms.txt: a file that stops early without saying so reads as
            // the whole site, and a model would answer "the site does not mention it".
            if (included < listable.Count)
            {
                text.Append("\n---\n\n> ").Append(listable.Count - included)
                    .Append(" further pages are not included here. The complete set is in ")
                    .Append(Absolute("/sitemap.xml")).Append(".\n");
            }

            return text.ToString();
        }

        /// <summary>
        /// The pages the LLM files speak for, and how many were deliberately left out.
        /// </summary>
        /// <remarks>
        /// A site can publish thousands of generated pages that are entirely legitimate SEO
        /// and pure noise to a model trying to learn what the site is — and nothing about how
        /// a page was produced says which it is. Syndicated pages are not the dividing line:
        /// on the CAI site the rubric catalogues arrive the same way the survey pages do,
        /// and one is the standard while the other is a long tail. So the site declares the
        /// paths (<see cref="Site.SetLlmsExcludedPaths"/>) rather than the publisher guessing.
        /// <para>
        /// This filter is for the LLM files only. sitemap.xml still lists everything: those
        /// pages exist to be indexed, which is exactly what a sitemap is for.
        /// </para>
        /// </remarks>
        private (List<PagePlan> Listed, int Excluded) LlmsPages(List<PagePlan> plans)
        {
            var published = plans
                .Where(plan => plan.SlugPath is not null && !_failed.Contains(plan.Page.Id))
                .ToList();

            if (_llmsExcludedPaths.Count == 0)
            {
                return (published, 0);
            }

            var listed = published.Where(plan => !IsLlmsExcluded(plan.SlugPath!)).ToList();
            return (listed, published.Count - listed.Count);
        }

        // A prefix covers itself and everything nested under it. The "/" guard is what keeps
        // "surveys" from also swallowing a page called "surveys-explained" — unless the site
        // asked for exactly that with a trailing "*", which matches by segment prefix and so
        // covers a family of generated names ("dimensions/rubric*") without naming each one.
        private bool IsLlmsExcluded(string slugPath) =>
            _llmsExcludedPaths.Any(prefix => prefix.EndsWith('*')
                ? slugPath.StartsWith(prefix[..^1], StringComparison.Ordinal)
                : slugPath.Equals(prefix, StringComparison.Ordinal)
                  || slugPath.StartsWith(prefix + "/", StringComparison.Ordinal));

        // Said out loud for the same reason the size caps are: a file that silently omits a
        // section of the site reads as the whole site. Worded as a choice, not a truncation,
        // so a model can tell "deliberately out of scope" from "did not fit".
        private void AppendExclusionNote(StringBuilder text, int excluded)
        {
            if (excluded == 0)
            {
                return;
            }

            text.Append("\n> ").Append(excluded)
                .Append(excluded == 1 ? " page under " : " pages under ")
                .Append(string.Join(", ", _llmsExcludedPaths.Select(
                    prefix => prefix.EndsWith('*') ? $"/{prefix}" : $"/{prefix}/")))
                .Append(excluded == 1 ? " is" : " are")
                .Append(" published for search engines and deliberately left out here. The complete set is in ")
                .Append(Absolute("/sitemap.xml")).Append(".\n");
        }

        /// <summary>The words a page renders, in document order, flattened to plain prose.</summary>
        private void AppendProse(StringBuilder text, PublishedPage page)
        {
            foreach (var node in ProseNodesOf(page))
            {
                switch (node)
                {
                    case HeadingNode heading when Localized(heading.Text) is { Length: > 0 } value:
                        // The page owns "##", so its own headings start one level below it and
                        // stop at Markdown's floor rather than emitting "#######".
                        text.Append('\n').Append(new string('#', Math.Clamp(heading.Level + 1, 3, 6)))
                            .Append(' ').Append(value).Append('\n');
                        break;

                    case RichTextNode richText
                        when CanonicalHtml.ToPlainText(Localized(richText.Html)) is { Length: > 0 } prose:
                        text.Append('\n').Append(prose).Append('\n');
                        break;

                    case ButtonNode button when Localized(button.Label) is { Length: > 0 } label:
                        // A call to action is a link the page makes prominent — worth keeping as
                        // one, so a model can follow it instead of reading a verb with no object.
                        text.Append('\n')
                            .Append(HrefOf(button.LinkTo) is { } href ? $"[{label}]({href})" : label)
                            .Append('\n');
                        break;

                    // Alt text is the only thing an image contributes that survives being read
                    // aloud, which is the same test this file applies to everything in it.
                    case ImageNode image when Localized(image.Alt) is { Length: > 0 } alt:
                        text.Append("\n![").Append(alt).Append("]\n");
                        break;

                    case SvgNode svg when Localized(svg.Alt) is { Length: > 0 } alt:
                        text.Append("\n![").Append(alt).Append("]\n");
                        break;
                }
            }
        }

        /// <summary>
        /// Like <see cref="NodesOf"/>, but block instances carry their overrides. NodesOf is
        /// used to collect asset and widget references, which overrides cannot change, so it
        /// reads the definition directly; text is the one thing overrides DO change.
        /// </summary>
        private IEnumerable<Node> ProseNodesOf(PublishedPage page)
        {
            foreach (var node in page.Tree.All())
            {
                yield return node;
                if (node is BlockInstanceNode instance && blockLibrary.Get(instance.DefinitionId) is { } definition)
                {
                    foreach (var inner in PageTree.Flatten(OverrideApplier.Apply(definition.Spec, instance.Overrides)))
                    {
                        yield return inner;
                    }
                }
            }
        }

        private string Localized(LocalizedText text) => text.Resolve(_defaultLocale, _defaultLocale);

        private string? HrefOf(Link? link) => link switch
        {
            PageLink page when _slugPathOf.TryGetValue(page.PageId, out var target) =>
                page.Href(Absolute(DirectoryPath(target, _defaultLocale))),
            ExternalLink external when CanonicalHtml.IsAllowedHref(external.Url) => external.Url,
            _ => null,
        };

        private static string XmlEscape(string value) => value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal);

        private PublishManifest BuildManifest(List<PagePlan> plans, string cssHash)
        {
            var entries = new SortedDictionary<string, PublishManifest.PageEntry>(StringComparer.Ordinal);
            foreach (var plan in plans)
            {
                entries[plan.Page.Id.Compact] = new PublishManifest.PageEntry
                {
                    PublishedVersion = plan.Page.PublishedVersion,
                    // Sound for un-rendered pages too: staleness already proved their
                    // files equal what a render at this site version would produce.
                    RenderedAtSiteVersion = _siteVersion,
                    Paths = _failed.Contains(plan.Page.Id) ? [] : plan.Paths,
                    AssetHashes = plan.AssetHashes,
                    Dependencies = plan.Dependencies,
                    Error = _errors.GetValueOrDefault(plan.Page.Id),
                };
            }

            return new PublishManifest
            {
                SiteVersion = _siteVersion,
                Pages = entries,
                CssHash = cssHash,
                RendererVersion = RendererVersion,
                WidgetBundles = new SortedDictionary<string, string>(
                    _widgetFiles.ToDictionary(pair => pair.Key, pair => pair.Value.Hash), StringComparer.Ordinal),
            };
        }

        // ----------------------------------------------------- compression and sweep

        private HashSet<string> DesiredFiles(List<PagePlan> plans)
        {
            var desired = new HashSet<string>(StringComparer.Ordinal) { PublishManifest.FileName };

            void Keep(string relative)
            {
                desired.Add(relative);
                if (Precompressor.IsCompressible(relative))
                {
                    desired.Add(relative + ".br");
                    desired.Add(relative + ".gz");
                }
            }

            Keep(_cssFile);
            Keep("404.html");
            Keep("sitemap.xml");
            Keep("robots.txt");
            Keep("llms.txt");
            if (_posts.Count > 0)
            {
                // Written only when posts exist, so it may only be KEPT then — otherwise the
                // first post-free publish would leave yesterday's feed on disk forever.
                Keep("feed.xml");
            }

            Keep("llms-full.txt");
            foreach (var plan in plans)
            {
                if (plan.SlugPath is null || _failed.Contains(plan.Page.Id))
                {
                    continue;
                }

                foreach (var path in plan.Paths)
                {
                    Keep(IndexFileOf(path));
                }
            }

            foreach (var (_, file) in _widgetFiles)
            {
                Keep(file.RelativePath);
            }

            foreach (var file in _assets.Files)
            {
                desired.Add(file.RelativePath);
            }

            // Shipped static assets. woff2 is already compressed and gets no siblings, but the brand
            // SVG is text and Precompress WILL write .br/.gz for it — so those have to be desired
            // too, or Sweep deletes them and the next run rewrites them, breaking the zero-rewrite
            // guarantee with two files every single sync.
            foreach (var asset in FontAssets.All)
            {
                desired.Add(asset.RelativePath);
                if (Precompressor.IsCompressible(asset.RelativePath))
                {
                    desired.Add(asset.RelativePath + ".br");
                    desired.Add(asset.RelativePath + ".gz");
                }
            }

            return desired;
        }

        private async Task Precompress(HashSet<string> desired, CancellationToken ct)
        {
            foreach (var relative in desired.Where(f => Precompressor.IsCompressible(f)).Order(StringComparer.Ordinal))
            {
                var full = FullPath(relative);
                if (!File.Exists(full))
                {
                    continue; // a withdrawn (failed) page never got written
                }

                // Trust existing siblings only when they are at least as new as the
                // source. Mere existence is not enough: a crash between writing the
                // source (a prior pass) and compressing it leaves stale siblings that
                // WriteIfChanged never revisits, since the source isn't rewritten and
                // _written doesn't carry it. The source is always written before its
                // siblings, so in a healthy state sibling mtime >= source mtime.
                var br = new FileInfo(full + ".br");
                var gz = new FileInfo(full + ".gz");
                var sourceTime = File.GetLastWriteTimeUtc(full);
                var siblingsCurrent =
                    !_written.Contains(relative) &&
                    br.Exists && gz.Exists &&
                    br.LastWriteTimeUtc >= sourceTime &&
                    gz.LastWriteTimeUtc >= sourceTime;
                if (siblingsCurrent)
                {
                    continue;
                }

                var content = await File.ReadAllBytesAsync(full, ct);
                await WriteIfChanged(relative + ".br", Precompressor.Brotli(content), ct);
                await WriteIfChanged(relative + ".gz", Precompressor.Gzip(content), ct);
            }
        }

        private void Sweep(HashSet<string> desired)
        {
            foreach (var file in Directory.EnumerateFiles(_outputRoot, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(_outputRoot, file).Replace('\\', '/');
                if (!desired.Contains(relative))
                {
                    File.Delete(file);
                }
            }

            // Longest paths first so nested empty directories collapse bottom-up.
            foreach (var directory in Directory
                         .EnumerateDirectories(_outputRoot, "*", SearchOption.AllDirectories)
                         .OrderByDescending(path => path.Length))
            {
                if (!Directory.EnumerateFileSystemEntries(directory).Any())
                {
                    Directory.Delete(directory);
                }
            }
        }

        private async Task<bool> WriteIfChanged(string relative, byte[] content, CancellationToken ct)
        {
            var full = FullPath(relative);
            if (File.Exists(full))
            {
                var existing = await File.ReadAllBytesAsync(full, ct);
                if (existing.AsSpan().SequenceEqual(content))
                {
                    return false; // byte-identical: the zero-rewrite guarantee in action
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            await File.WriteAllBytesAsync(full, content, ct);
            _written.Add(relative);
            _filesWritten++;
            _bytesWritten += content.Length;
            return true;
        }
    }
}
