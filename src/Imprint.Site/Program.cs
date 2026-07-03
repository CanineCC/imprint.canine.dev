// Imprint.Site — the whole delivery story in one readable file.
//
// Serves the publisher's output directory with the headers a static site deserves:
//   - content-hashed assets (name.{16 hex}.ext)  → Cache-Control: immutable, 1 year
//   - HTML and everything else                   → no-cache + ETag revalidation
//   - precompressed .br/.gz siblings             → served when the client accepts
//   - directory URLs                             → index.html, missing pages → 404.html
// A CDN or nginx in front of the same directory is an equally valid production setup;
// this host exists so the delivery contract is executable and testable.

using System.Text.RegularExpressions;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var publishRoot = ResolvePublishRoot(app.Configuration, app.Environment);
app.Logger.LogInformation("Serving published site from {Root}", publishRoot);

var contentTypes = new PrecompressedContentTypeProvider();
var fileProvider = new PhysicalFileProvider(publishRoot);
var hashedName = new Regex(@"\.[0-9a-f]{16}\.[a-z0-9]+$", RegexOptions.Compiled);

// Rewrite: directory-style URLs to index.html, and pick a precompressed sibling when
// the client accepts it (the publisher writes .br/.gz next to every text file).
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "/";
    if (path.EndsWith('/'))
    {
        path += "index.html";
    }
    else if (!Path.HasExtension(path))
    {
        path += "/index.html";
    }

    if (fileProvider.GetFileInfo(path) is { Exists: false })
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        path = "/404.html";
        if (!fileProvider.GetFileInfo(path).Exists)
        {
            await context.Response.WriteAsync("Not found.");
            return;
        }
    }

    var acceptEncoding = context.Request.Headers.AcceptEncoding.ToString();
    foreach (var (encoding, extension) in new[] { ("br", ".br"), ("gzip", ".gz") })
    {
        if (acceptEncoding.Contains(encoding, StringComparison.OrdinalIgnoreCase) &&
            fileProvider.GetFileInfo(path + extension).Exists)
        {
            context.Response.Headers.ContentEncoding = encoding;
            context.Response.Headers.Vary = "Accept-Encoding";

            // The content-type provider below maps ".html.br" by its inner extension,
            // so the static middleware serves the sibling with the real type.
            path += extension;
            break;
        }
    }

    context.Request.Path = path;
    await next();
});

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = fileProvider,
    ContentTypeProvider = contentTypes,
    ServeUnknownFileTypes = false,
    OnPrepareResponse = context =>
    {
        // ETag + Last-Modified come from the static-files middleware itself; we only
        // decide the caching policy: hashed names never change, everything else
        // revalidates. (The .br/.gz suffix hides the hash — strip it before testing.)
        var name = context.File.Name;
        if (name.EndsWith(".br", StringComparison.Ordinal) || name.EndsWith(".gz", StringComparison.Ordinal))
        {
            name = name[..^3];
        }

        context.Context.Response.Headers.CacheControl =
            hashedName.IsMatch(name) ? "public, max-age=31536000, immutable" : "no-cache";
    },
});

app.Run();

static string ResolvePublishRoot(IConfiguration configuration, IWebHostEnvironment environment)
{
    var configured = configuration["ImprintPublish"];
    var candidates = configured is not null
        ? new[] { Path.GetFullPath(configured) }
        : new[]
        {
            // Default developer flow: the editor sibling project's data directory.
            Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", "Imprint.Editor", "data", "publish")),
            Path.GetFullPath(Path.Combine(environment.ContentRootPath, "publish")),
        };

    return candidates.FirstOrDefault(Directory.Exists)
        ?? throw new InvalidOperationException(
            $"No published site found (looked at: {string.Join(", ", candidates)}). " +
            "Publish from the editor first, or pass --ImprintPublish=<path>.");
}

/// <summary>
/// Maps precompressed siblings (<c>page.html.br</c>) by their inner extension. Without
/// this the static middleware refuses ".br" as an unknown type and an
/// Accept-Encoding: br request for an existing page turns into a 404.
/// </summary>
file sealed class PrecompressedContentTypeProvider : IContentTypeProvider
{
    private readonly FileExtensionContentTypeProvider _inner = new();

    public bool TryGetContentType(string subpath, out string contentType)
    {
        var probe = subpath.EndsWith(".br", StringComparison.OrdinalIgnoreCase) ||
                    subpath.EndsWith(".gz", StringComparison.OrdinalIgnoreCase)
            ? subpath[..^3]
            : subpath;
        return _inner.TryGetContentType(probe, out contentType!);
    }
}
