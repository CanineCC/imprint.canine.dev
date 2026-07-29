using Imprint.Authoring.Domain.Pages;

namespace Imprint.Authoring.Syndication;

/// <summary>
/// The address of a syndicated page: one or more slug-shaped segments joined by <c>/</c>.
/// </summary>
/// <remarks>
/// A path arrives from another system and becomes a DIRECTORY in the published output, so it is
/// validated rather than trusted. Each segment must satisfy the same rule a slug does, which rules
/// out <c>..</c>, absolute paths, backslashes, spaces and anything else that could place a file
/// outside the site — the containment is a property of the alphabet, not of a traversal check
/// somebody has to remember to write.
/// <para>
/// The first segment additionally may not collide with the published site's own files (assets, css,
/// widgets, sitemap…), which <see cref="Slug"/> already refuses for the same reason.
/// </para>
/// </remarks>
public static class SyndicatedPath
{
    /// <summary>The maximum number of segments — deep enough for host/owner/name, shallow enough to stay a path.</summary>
    private const int MaxSegments = 6;

    /// <summary>
    /// The canonical form of <paramref name="path"/>, or null when it is not one this site can serve.
    /// </summary>
    public static string? Sanitize(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var segments = path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length is 0 or > MaxSegments)
        {
            return null;
        }

        var clean = new string[segments.Length];
        for (var i = 0; i < segments.Length; i++)
        {
            // Every segment has to be a valid slug in its own right. Slug.TryCreate lower-cases,
            // enforces the alphabet, and refuses the names the published output reserves — so a
            // path can never reach outside the site or shadow sitemap.xml.
            if (!Slug.TryCreate(segments[i], out var slug, out _))
            {
                return null;
            }

            clean[i] = slug.Value;
        }

        return string.Join('/', clean);
    }
}
