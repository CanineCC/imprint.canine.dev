using System.Text;
using System.Text.RegularExpressions;
using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Projections;
using Imprint.EventSourcing;
using Microsoft.Extensions.Logging;

namespace Imprint.Publishing;

/// <summary>
/// Serves a reachable, published-STYLE preview of a site from the running app — the
/// full chrome + marketing CSS + hydrated islands the visitor will get, so the founder
/// can review the real thing at <c>/preview/{site}</c> before anything deploys.
/// </summary>
/// <remarks>
/// The published static output is origin-relative: it references <c>/css</c>, <c>/fonts</c>,
/// <c>/widgets</c> and <c>/media</c> with a leading slash, and each island's
/// <c>data-island</c> is a root-absolute module URL the loader <c>import()</c>s (a
/// <c>&lt;base&gt;</c> tag does not affect it). To serve several sites under one host we
/// render each site into its OWN preview folder and serve it under <c>/preview/{slug}/</c>,
/// rewriting every root-absolute URL in the HTML to that prefix. Assets are streamed from
/// the same folder, so the stylesheet, the fonts, the island bundles and their live
/// <c>api-base</c> fetches all resolve. Rendering is on demand with a short freshness
/// window; a first hit publishes, subsequent hits reuse the folder until it goes stale.
/// </remarks>
public sealed class SitePreview(
    SitePublisher publisher,
    SiteOverview siteOverview,
    ProjectionEngine projections,
    string previewRoot,
    ILogger<SitePreview> logger)
{
    private static readonly TimeSpan Freshness = TimeSpan.FromSeconds(5);
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<string, DateTimeOffset> _renderedAt = new(StringComparer.Ordinal);

    /// <summary>A served preview file: the bytes and the content type to send.</summary>
    public sealed record PreviewFile(byte[] Bytes, string ContentType);

    /// <summary>
    /// The URL-safe key each site is reachable under (<c>/preview/{slug}</c>): the first
    /// token of the brand name lower-cased (watchdog / cai / assay), or the compact id as
    /// a stable fallback. A caller may address a site by either its slug or its compact id.
    /// </summary>
    public IReadOnlyList<(string Slug, string Name)> Sites() =>
        [.. siteOverview.All.Select(site => (SlugOf(site), site.Name))];

    /// <summary>
    /// Resolve and serve one path under a site's preview. <paramref name="site"/> is the
    /// slug or compact id; <paramref name="path"/> is the in-site request path (e.g.
    /// <c>""</c>, <c>"about/"</c>, <c>"css/site.ab12.css"</c>). Returns null when the site
    /// or file is not found. HTML is rewritten to the <c>/preview/{site}/</c> prefix.
    /// </summary>
    public async Task<PreviewFile?> Serve(string site, string path)
    {
        var resolved = Resolve(site);
        if (resolved is not { } r)
        {
            return null;
        }

        var (slug, siteId) = r;
        await EnsureRendered(slug, siteId);

        var outputDir = Path.Combine(previewRoot, slug);
        // Default document: a directory request serves its index.html.
        var relative = string.IsNullOrEmpty(path) || path.EndsWith('/')
            ? path + "index.html"
            : path;
        var full = Path.GetFullPath(Path.Combine(outputDir, relative));

        // Traversal guard: never serve outside the site's own preview folder.
        var rootWithSep = Path.GetFullPath(outputDir) + Path.DirectorySeparatorChar;
        if (!full.StartsWith(rootWithSep, StringComparison.Ordinal))
        {
            return null;
        }

        if (!File.Exists(full))
        {
            // A slugged directory with no trailing slash (e.g. /preview/watchdog/about):
            // retry as a directory index before giving up.
            var asDir = Path.GetFullPath(Path.Combine(outputDir, path, "index.html"));
            if (asDir.StartsWith(rootWithSep, StringComparison.Ordinal) && File.Exists(asDir))
            {
                full = asDir;
                relative = Path.Combine(path, "index.html");
            }
            else
            {
                return null;
            }
        }

        var bytes = await File.ReadAllBytesAsync(full);
        var contentType = ContentTypeOf(full);

        if (contentType.StartsWith("text/html", StringComparison.Ordinal))
        {
            var html = Encoding.UTF8.GetString(bytes);
            bytes = Encoding.UTF8.GetBytes(PreviewRewrite.Html(html, slug));
        }
        else if (contentType.StartsWith("text/css", StringComparison.Ordinal))
        {
            // The stylesheet references the self-hosted fonts as url(/fonts/…) — root
            // absolute, so they must be re-homed under the preview prefix too, or the
            // browser resolves them at the host root and they 404.
            var css = Encoding.UTF8.GetString(bytes);
            bytes = Encoding.UTF8.GetBytes(PreviewRewrite.Css(css, slug));
        }

        return new PreviewFile(bytes, contentType);
    }

    // Render the site into its preview folder if we have never rendered it, or the last
    // render is older than the freshness window (so edits show up on the next hit).
    private async Task EnsureRendered(string slug, SiteId siteId)
    {
        var now = DateTimeOffset.UtcNow;
        if (_renderedAt.TryGetValue(slug, out var at) && now - at < Freshness)
        {
            return;
        }

        await _gate.WaitAsync();
        try
        {
            if (_renderedAt.TryGetValue(slug, out at) && now - at < Freshness)
            {
                return;
            }

            // Preview reflects the latest AUTHORED state — catch the read models up first,
            // then render this one site to its own folder with no BaseUrl (origin-relative
            // output, which the rewrite then re-homes under /preview/{slug}/).
            await projections.CatchUp();
            if (siteOverview.Get(siteId) is { } aggregate)
            {
                var target = new PublishTarget(aggregate, Path.Combine(previewRoot, slug), BaseUrl: null);
                var report = await publisher.Synchronize(target);
                if (report.Errors.Count > 0)
                {
                    logger.LogWarning(
                        "Preview render of '{Slug}' completed with {Count} page error(s).", slug, report.Errors.Count);
                }
            }

            _renderedAt[slug] = DateTimeOffset.UtcNow;
        }
        finally
        {
            _gate.Release();
        }
    }

    private (string Slug, SiteId Id)? Resolve(string site)
    {
        foreach (var s in siteOverview.All)
        {
            if (string.Equals(SlugOf(s), site, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(s.Id.Compact, site, StringComparison.OrdinalIgnoreCase))
            {
                return (SlugOf(s), s.Id);
            }
        }

        return null;
    }

    private static string SlugOf(Site site)
    {
        // The first word of the brand, kept to url-safe chars: "Watchdog" → "watchdog",
        // "CAI" → "cai". Empty (unnamed) sites fall back to their compact id.
        var first = site.Name.Split([' ', '\t', '—', '-', '·'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        var slug = new string((first ?? "").Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
        return slug.Length > 0 ? slug : site.Id.Compact;
    }

    private static string ContentTypeOf(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".html" => "text/html; charset=utf-8",
        ".css" => "text/css; charset=utf-8",
        ".js" => "text/javascript; charset=utf-8",
        ".json" => "application/json; charset=utf-8",
        ".svg" => "image/svg+xml; charset=utf-8",
        ".woff2" => "font/woff2",
        ".woff" => "font/woff",
        ".xml" => "application/xml; charset=utf-8",
        ".txt" => "text/plain; charset=utf-8",
        ".webp" => "image/webp",
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".gif" => "image/gif",
        ".webm" => "video/webm",
        ".ico" => "image/x-icon",
        _ => "application/octet-stream",
    };
}

/// <summary>
/// The pure URL re-homing used to serve a published site's origin-relative output under a
/// <c>/preview/{slug}/</c> prefix. Extracted from <see cref="SitePreview"/> so the rewrite
/// contract (which URLs move, which are left alone) is unit-testable without the app.
/// </summary>
internal static partial class PreviewRewrite
{
    /// <summary>
    /// Re-home every root-absolute URL in a preview page to <c>/preview/{slug}/</c>: the
    /// stylesheet, fonts, island bundles + their <c>data-island</c> module URLs, media, and
    /// the internal nav/canonical/home hrefs. Protocol-relative (<c>//host</c>) and absolute
    /// (<c>https://</c>) URLs, page anchors (<c>#</c>) and <c>mailto:</c>/<c>tel:</c> are left
    /// untouched — the live <c>api-base</c> fetch URL among them.
    /// </summary>
    public static string Html(string html, string slug)
    {
        var prefix = "/preview/" + slug + "/";
        // Match href="/…", src="/…", data-island="/…" (single leading slash only). The
        // leading slash is consumed by the pattern, so `url` is the path without it.
        return AbsoluteUrlAttribute().Replace(html, m =>
        {
            var attr = m.Groups["attr"].Value;
            var quote = m.Groups["q"].Value;
            var url = m.Groups["url"].Value;
            return $"{attr}={quote}{prefix}{url}{quote}";
        });
    }

    /// <summary>
    /// Re-home the stylesheet's root-absolute <c>url(/…)</c> references (the self-hosted
    /// fonts) under <c>/preview/{slug}/</c>. <c>data:</c> URIs and absolute/protocol-relative
    /// urls are skipped by the single-leading-slash pattern, exactly as {@link Html} is.
    /// </summary>
    public static string Css(string css, string slug)
    {
        var prefix = "/preview/" + slug + "/";
        return CssRootUrl().Replace(css, m =>
        {
            var quote = m.Groups["q"].Value;
            var url = m.Groups["url"].Value;
            return $"url({quote}{prefix}{url}{quote})";
        });
    }

    // href / src / data-island whose value is a single-leading-slash path ("/x", never
    // "//host" or "https://"). The value capture stops at the closing quote.
    [GeneratedRegex("""(?<attr>href|src|data-island)=(?<q>["'])/(?!/)(?<url>[^"']*)\k<q>""", RegexOptions.IgnoreCase)]
    private static partial Regex AbsoluteUrlAttribute();

    // url("/x") / url('/x') in CSS — a single-leading-slash path only (data:/http(s):/
    // protocol-relative and bare url(/…) without quotes are all excluded). The published
    // stylesheet always quotes its font urls, so requiring quotes is safe and avoids
    // touching the many data: url()s the theme inlines.
    [GeneratedRegex("""url\((?<q>["'])/(?!/)(?<url>[^"']*)\k<q>\)""", RegexOptions.IgnoreCase)]
    private static partial Regex CssRootUrl();
}
