using System.Text;

namespace Imprint.Authoring.Domain.Pages;

/// <summary>
/// The single sanitizer for <see cref="SectionNode.Anchor"/>: lowercased, runs of
/// non-alphanumerics collapsed to single hyphens, must start with a letter, at most
/// 80 chars. Null when nothing valid remains — the renderer then emits no <c>id</c>
/// at all, so a stored anchor can never become a broken or unsafe attribute. Shared
/// by the content seeder (authoring time) and the section view (render time) so the
/// two can never disagree on what an anchor looks like.
/// </summary>
public static class SectionAnchor
{
    public static string? Sanitize(string? anchor) =>
        Slug(anchor) is { } slug && slug[0] is >= 'a' and <= 'z' ? slug : null;

    /// <summary>
    /// The same slug, for an id derived from a heading's own words rather than an authored
    /// anchor. It differs in one way: a leading run of digits is dropped instead of
    /// rejecting the whole value, because a numbered legal heading ("6. Subprocessors")
    /// must still yield a usable id — <c>#subprocessors</c>. The number stays visible in
    /// the heading, so nothing is lost for someone citing a clause.
    /// </summary>
    public static string? ForHeading(string? text)
    {
        if (Slug(text) is not { } slug)
        {
            return null;
        }

        var start = 0;
        while (start < slug.Length && slug[start] is not (>= 'a' and <= 'z'))
        {
            start++;
        }

        return start < slug.Length ? slug[start..] : null;
    }

    private static string? Slug(string? anchor)
    {
        if (string.IsNullOrWhiteSpace(anchor))
        {
            return null;
        }

        var sb = new StringBuilder(anchor.Length);
        var pendingHyphen = false;
        foreach (var c in anchor.Trim().ToLowerInvariant())
        {
            if (c is (>= 'a' and <= 'z') or (>= '0' and <= '9'))
            {
                if (pendingHyphen && sb.Length > 0)
                {
                    sb.Append('-');
                }

                pendingHyphen = false;
                sb.Append(c);
            }
            else
            {
                pendingHyphen = true;
            }
        }

        if (sb.Length == 0)
        {
            return null;
        }

        return sb.Length <= 80 ? sb.ToString() : sb.ToString(0, 80).TrimEnd('-');
    }
}
