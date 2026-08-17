using Imprint.Authoring.Domain.Sites;

namespace Imprint.Publishing;

/// <summary>
/// Where a post lives in the published output. One decision, in one place, because two things now
/// need the answer and they must not disagree: the publisher, which writes the file, and the
/// editor, which offers a link to it. It disagreed exactly once — the editor's preview link was
/// written when every post lived under <c>/blog/</c>, and a blog site's posts moved to its root a
/// commit later, so the link 404'd on the only site that matters.
/// </summary>
public static class BlogPaths
{
    /// <summary>
    /// The blog's URL prefix inside an ordinary site. A post's slug is unique among POSTS only, so
    /// the prefix is what keeps the two namespaces apart — an author may have both a page and a
    /// post called "notes" without either of them noticing the other.
    /// </summary>
    public const string Prefix = "blog";

    /// <summary>
    /// The prefix posts sit under, relative to the origin. On a <see cref="SiteKind.Blog"/> the
    /// site IS the blog — its origin was chosen to say so (<c>blog.canine.dev</c>) — so the prefix
    /// would only repeat the hostname back at the reader: <c>blog.canine.dev/blog/a-post</c>.
    /// A blog SECTION of an ordinary site still needs it, for the namespace reason above.
    /// </summary>
    public static string PostPrefix(SiteKind kind) => kind == SiteKind.Blog ? "" : Prefix + "/";

    /// <summary>The index's own public path: the root of a blog site, <c>blog</c> inside a site.</summary>
    public static string IndexPath(SiteKind kind) => kind == SiteKind.Blog ? "" : Prefix;

    /// <summary>The index's href, for the feed and the entries that point back at it.</summary>
    public static string IndexHref(SiteKind kind) => kind == SiteKind.Blog ? "/" : $"/{Prefix}/";

    /// <summary>One post's public path, with its trailing slash — what a link to it must say.</summary>
    public static string PostPath(SiteKind kind, string slug) => $"/{PostPrefix(kind)}{slug}/";
}
