using System.Collections.Immutable;

namespace Imprint.Authoring.Domain;

/// <summary>
/// A text value per locale — the reason side-by-side translation is a projection
/// instead of a feature: content fields are <em>born</em> multilingual. Immutable,
/// with structural equality (so events carrying localized text compare by value).
/// </summary>
public sealed class LocalizedText : IEquatable<LocalizedText>
{
    public static readonly LocalizedText Empty = new(ImmutableSortedDictionary<Locale, string>.Empty);

    private readonly ImmutableSortedDictionary<Locale, string> _values;

    private LocalizedText(ImmutableSortedDictionary<Locale, string> values) => _values = values;

    public static LocalizedText Of(Locale locale, string value) =>
        Empty.With(locale, value);

    public IReadOnlyCollection<Locale> Locales => _values.Keys.ToImmutableArray();
    public IEnumerable<KeyValuePair<Locale, string>> Values => _values;
    public bool IsEmpty => _values.IsEmpty;

    public LocalizedText With(Locale locale, string value) =>
        new(string.IsNullOrEmpty(value) ? _values.Remove(locale) : _values.SetItem(locale, value));

    public string? Get(Locale locale) => _values.GetValueOrDefault(locale);

    public bool Has(Locale locale) => _values.ContainsKey(locale);

    /// <summary>Locale value, falling back to the default locale, then to any value, then empty.</summary>
    public string Resolve(Locale locale, Locale defaultLocale) =>
        _values.GetValueOrDefault(locale)
        ?? _values.GetValueOrDefault(defaultLocale)
        ?? (_values.IsEmpty ? string.Empty : _values.First().Value);

    public bool Equals(LocalizedText? other) =>
        other is not null && _values.SequenceEqual(other._values);

    public override bool Equals(object? obj) => Equals(obj as LocalizedText);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var (locale, value) in _values)
        {
            hash.Add(locale);
            hash.Add(value);
        }

        return hash.ToHashCode();
    }

    public override string ToString() =>
        string.Join(", ", _values.Select(kv => $"{kv.Key}: \"{kv.Value}\""));
}
