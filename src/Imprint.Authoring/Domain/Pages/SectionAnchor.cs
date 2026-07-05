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
    public static string? Sanitize(string? anchor)
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

        if (sb.Length == 0 || sb[0] is not (>= 'a' and <= 'z'))
        {
            return null;
        }

        var value = sb.Length <= 80 ? sb.ToString() : sb.ToString(0, 80).TrimEnd('-');
        return value;
    }
}
