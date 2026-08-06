namespace Imprint.Authoring.Domain.Pages;

/// <summary>
/// The one reader of imprint's own <c>page:{guid}</c> href scheme, optionally narrowed to a
/// section with <c>#anchor</c>. Prose stores the reference rather than a path so a link keeps
/// working across a slug rename and resolves into the reader's own locale — and the fragment
/// rides along so "the independence section of the front page" is expressible from another page,
/// which a bare <c>#anchor</c> (same page only) and an absolute URL (default locale only) both
/// fail to say.
/// <para>Shared by the validator and the renderer so the grammar they enforce and the grammar
/// they resolve cannot drift into a parser differential.</para>
/// </summary>
public static class PageHref
{
    public const string Scheme = "page:";

    /// <summary>
    /// True when <paramref name="href"/> is a well-formed page reference; <paramref name="fragment"/>
    /// is the sanitized section, or null when the reference names the whole page.
    /// </summary>
    public static bool TryParse(string href, out PageId pageId, out string? fragment)
    {
        pageId = default;
        fragment = null;
        if (!href.StartsWith(Scheme, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var rest = href[Scheme.Length..];
        var hash = rest.IndexOf('#', StringComparison.Ordinal);
        var raw = hash < 0 ? rest : rest[..hash];
        if (!Guid.TryParse(raw, out var guid))
        {
            return false;
        }

        pageId = PageId.From(guid);
        // An anchor the sanitizer rejects degrades to a link to the page, never to a broken
        // href: the reader still lands somewhere true, one scroll from where the author meant.
        fragment = hash < 0 ? null : SectionAnchor.Sanitize(rest[(hash + 1)..]);
        return true;
    }
}
