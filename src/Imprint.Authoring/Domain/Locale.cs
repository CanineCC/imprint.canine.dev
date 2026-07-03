using System.Text.RegularExpressions;

namespace Imprint.Authoring.Domain;

/// <summary>
/// A normalized IETF language tag (<c>en</c>, <c>da</c>, <c>de-AT</c>): lower-case
/// language, upper-case region. Construction validates; equality is ordinal on the
/// canonical string, so <c>DA-dk</c> and <c>da-DK</c> are the same locale.
/// </summary>
public readonly partial record struct Locale : IComparable<Locale>
{
    [GeneratedRegex("^[a-z]{2,3}(-[A-Z]{2})?$")]
    private static partial Regex CanonicalForm();

    public string Value { get; }

    public Locale(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var parts = value.Trim().Split('-');
        var canonical = parts.Length switch
        {
            1 => parts[0].ToLowerInvariant(),
            2 => $"{parts[0].ToLowerInvariant()}-{parts[1].ToUpperInvariant()}",
            _ => value, // fails the regex below
        };
        if (!CanonicalForm().IsMatch(canonical))
        {
            throw new ArgumentException($"'{value}' is not a valid locale tag (expected e.g. 'en' or 'de-AT').", nameof(value));
        }

        Value = canonical;
    }

    /// <summary>The language part, used for <c>&lt;html lang&gt;</c> and URL prefixes (<c>da-DK</c> → <c>da-dk</c>).</summary>
    public string UrlSegment => Value.ToLowerInvariant();

    public int CompareTo(Locale other) => string.CompareOrdinal(Value, other.Value);
    public override string ToString() => Value;

    public static bool TryCreate(string? value, out Locale locale)
    {
        try
        {
            locale = new Locale(value!);
            return true;
        }
        catch (ArgumentException)
        {
            locale = default;
            return false;
        }
    }
}
