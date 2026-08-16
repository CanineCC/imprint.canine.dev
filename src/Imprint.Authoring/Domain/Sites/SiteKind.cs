using System.Text.Json.Serialization;

namespace Imprint.Authoring.Domain.Sites;

/// <summary>
/// What a site publishes, and therefore how an author enters it.
///
/// Both kinds are sites in every structural sense — one origin, one settings surface,
/// one publish pipeline — because the thing a subdomain addresses is a site whatever is
/// inside it. What differs is the shape of the content: a <see cref="Site"/> is a page
/// tree an author navigates, a <see cref="Blog"/> is a dated stream of posts with an
/// index and a feed. Modelling that as a kind rather than as a section hanging off some
/// other site is what lets a blog have its own origin: <c>blog.canine.dev</c> is a peer
/// of <c>watchdog.canine.dev</c>, not a folder inside it.
///
/// Serialized by NAME, not by ordinal: this lands in an append-only event log, where a
/// stored <c>1</c> is a value nobody can read back in two years, and reordering the
/// members would silently rewrite history.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<SiteKind>))]
public enum SiteKind
{
    /// <summary>A page tree. The default, and what every site created before blogs existed is.</summary>
    Site = 0,

    /// <summary>A dated stream of posts, published with an index and a feed.</summary>
    Blog = 1,
}
