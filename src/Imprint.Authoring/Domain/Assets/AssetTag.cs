namespace Imprint.Authoring.Domain.Assets;

/// <summary>
/// The shape of a media tag, in one place because two parts of the system must agree on
/// it: the aggregate deciding whether a tag is already on an asset, and the library
/// deciding whether two assets are filed under the same tag.
///
/// <para>A tag is the author's own filing system ("Blog-entry-20"), so the casing they
/// typed is what is stored and shown — but "Blog-entry-20" and "blog-entry-20" are one
/// tag, not two, because nobody types a tag twice on purpose.</para>
/// </summary>
public static class AssetTag
{
    public const int MaxLength = 60;

    /// <summary>Case-insensitive: the comparison that decides tag identity everywhere.</summary>
    public static StringComparer Comparer => StringComparer.OrdinalIgnoreCase;

    /// <summary>
    /// Trims and collapses whitespace. A tag pasted with a trailing space, or typed with a
    /// double space, is the same tag — normalizing here rather than at each call site is
    /// what keeps the filter and the stored value from drifting apart.
    /// </summary>
    public static string Normalize(string tag) =>
        string.Join(' ', tag.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
