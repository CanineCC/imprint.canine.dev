using System.Text;
using System.Text.RegularExpressions;

namespace Imprint.Authoring.Domain.Pages;

/// <summary>
/// A URL path segment: lower-case kebab, 1–80 chars. Reserved names (published asset
/// folders) and anything that parses as a locale tag (locale URL prefixes) are
/// rejected so a page can never shadow <c>/assets/</c> or <c>/da/</c>.
/// </summary>
public readonly partial record struct Slug
{
    [GeneratedRegex("^[a-z0-9](?:[a-z0-9-]{0,78}[a-z0-9])?$")]
    private static partial Regex Shape();

    private static readonly string[] Reserved = ["assets", "css", "js", "widgets", "sitemap", "robots"];

    public string Value { get; }

    private Slug(string value) => Value = value;

    public static bool TryCreate(string? input, out Slug slug, out string? error)
    {
        slug = default;
        error = null;
        var candidate = input?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!Shape().IsMatch(candidate))
        {
            error = "Slugs are 1–80 characters of lower-case letters, digits and hyphens.";
            return false;
        }

        if (Reserved.Contains(candidate))
        {
            error = $"'{candidate}' is reserved for published site files.";
            return false;
        }

        if (Locale.TryCreate(candidate, out _))
        {
            error = $"'{candidate}' looks like a language code, which is reserved for locale URLs.";
            return false;
        }

        slug = new Slug(candidate);
        return true;
    }

    /// <summary>Best-effort slug suggestion from a title (Danish-friendly: æ→ae ø→oe å→aa).</summary>
    public static string Suggest(string title)
    {
        var folded = title.ToLowerInvariant()
            .Replace("æ", "ae").Replace("ø", "oe").Replace("å", "aa")
            .Replace("ä", "ae").Replace("ö", "oe").Replace("ü", "ue").Replace("ß", "ss");

        var builder = new StringBuilder(folded.Length);
        var lastWasHyphen = true; // suppress leading hyphen
        foreach (var normalized in folded.Normalize(NormalizationForm.FormD))
        {
            var category = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(normalized);
            if (category == System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                continue; // strip diacritics
            }

            if (normalized is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                builder.Append(normalized);
                lastWasHyphen = false;
            }
            else if (!lastWasHyphen)
            {
                builder.Append('-');
                lastWasHyphen = true;
            }
        }

        var suggestion = builder.ToString().Trim('-');
        if (suggestion.Length > 80)
        {
            suggestion = suggestion[..80].Trim('-');
        }

        return TryCreate(suggestion, out _, out _) ? suggestion : $"page-{suggestion}".Trim('-');
    }

    public override string ToString() => Value;
}
