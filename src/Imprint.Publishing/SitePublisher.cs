using System.Diagnostics;
using System.Text;
using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Features.Assets;
using Imprint.Authoring.Projections;
using Imprint.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RenderMode = Imprint.Rendering.RenderMode;

namespace Imprint.Publishing;

/// <summary>One site rendered to one output folder, addressed by one base URL — the unit a publish pass acts on.</summary>
public sealed record PublishTarget(Site Site, string OutputPath, string? BaseUrl);

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
                options, target.Site, target.OutputPath, target.BaseUrl, publishedContent,
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
        PublishedContent publishedContent,
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
        private readonly HashSet<PageId> _failed = [];
        private readonly HashSet<string> _written = new(StringComparer.Ordinal);
        private int _filesWritten;
        private long _bytesWritten;

        // Snapshot state, filled by Run before any file is touched.
        private long _siteVersion;
        private string _siteName = "";
        private Locale _defaultLocale;
        private IReadOnlyList<Locale> _locales = [];
        private IReadOnlyList<NavigationItem> _navigation = [];
        private IReadOnlyList<FooterLinkGroup> _footerGroups = [];
        private HeaderAction? _headerCta;
        private HeaderAction? _headerQuiet;
        private CopyLine? _copyLine;
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
            var theme = site.Theme;
            _defaultLocale = site.DefaultLocale;
            _locales = [.. site.Locales];
            _navigation = [.. site.Navigation];
            _footerGroups = [.. site.FooterGroups];
            _headerCta = site.HeaderCta;
            _headerQuiet = site.HeaderQuiet;
            _copyLine = site.CopyLine;
            // Only THIS site's published pages — a target folder holds exactly one site.
            _pages = [.. publishedContent.AllForSite(site.Id)];
            _pageById = _pages.ToDictionary(page => page.Id);

            var oldManifest =
                PublishManifest.Load(Path.Combine(_outputRoot, PublishManifest.FileName)) ?? new PublishManifest();

            // ---- stylesheet: tokens + structural styles, one hashed file.
            var cssText = ThemeCss.Emit(theme) + "\n" + ThemeCss.StructuralCss;
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
                    [.. NodesOf(page).Select(AssetReferenceOf).Where(id => id.HasValue).Select(id => id!.Value).Distinct()]);
            _pageWidgetTags = ordered.ToDictionary(
                page => page.Id,
                page => (IReadOnlyList<string>)
                    [.. NodesOf(page).OfType<WidgetNode>().Select(widget => widget.Tag).Distinct().Order(StringComparer.Ordinal)]);

            _assets = await PublishedAssetCatalog.Build(
                pageAssetIds.Values.SelectMany(ids => ids), assetLibrary, mediaStore, logger, ct);
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
                    .ThenBy(page => page.Slug.Value, StringComparer.Ordinal)
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
            foreach (var group in ordered.GroupBy(page => homeId is { } home && page.Id == home ? "" : page.Slug.Value))
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
                if (item.PageId is { } topLevel)
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
                StylesheetHref = $"/{_cssFile}",
                SiteName = _siteName,
                HomeHref = HomeHref(locale),
                Nav = NavItemsFor(page.Id, locale),
                HeaderCta = HeaderLinkFor(_headerCta, locale),
                HeaderQuiet = HeaderLinkFor(_headerQuiet, locale),
                FooterGroups = FooterColumnsFor(locale),
                CopyLine = CopyLineFor(locale),
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
                StylesheetHref = $"/{_cssFile}",
                SiteName = _siteName,
                HomeHref = "/",
                Nav = NavItemsFor(currentPage: null, _defaultLocale),
                HeaderCta = HeaderLinkFor(_headerCta, _defaultLocale),
                HeaderQuiet = HeaderLinkFor(_headerQuiet, _defaultLocale),
                FooterGroups = FooterColumnsFor(_defaultLocale),
                CopyLine = CopyLineFor(_defaultLocale),
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

        private string DocumentTitle(PublishedPage page, Locale locale)
        {
            var title = page.MetaTitle.Resolve(locale, _defaultLocale);
            if (title.Length == 0)
            {
                title = page.Title.Resolve(locale, _defaultLocale);
            }

            return title.Length > 0 ? $"{title} · {_siteName}" : _siteName;
        }

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

                    return (DirectoryPath(slugPath, locale), label, page.PageId);

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

        private static AssetId? AssetReferenceOf(Node node) => node switch
        {
            ImageNode image => image.AssetId,
            VideoNode video => video.AssetId,
            SvgNode svg => svg.AssetId,
            _ => null,
        };

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

        private string BuildRobots() =>
            $"User-agent: *\nAllow: /\n\nSitemap: {Absolute("/sitemap.xml")}\n";

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
