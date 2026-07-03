using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Imprint.Authoring.Domain.Sites;

/// <summary>
/// The design system of a site: semantic color tokens (each with a light and a dark
/// value) plus typography choices. Everything a published page looks like flows from
/// this value object — the publisher emits it as CSS custom properties with
/// <c>light-dark()</c>, which is the whole dark-mode implementation.
/// </summary>
public sealed record Theme(TokenSet Tokens, Typography Typography)
{
    /// <summary>
    /// A tasteful, WCAG-AA baseline (contrast pairs are asserted by tests so nobody
    /// "improves" the default palette into an inaccessible one).
    /// </summary>
    public static readonly Theme Default = new(
        TokenSet.Of(new Dictionary<string, ThemeToken>
        {
            ["background"] = new("#ffffff", "#0f1115"),
            ["surface"] = new("#f6f7f9", "#171a21"),
            ["surface-alt"] = new("#eceef2", "#1f242e"),
            ["text"] = new("#16181d", "#e8eaf0"),
            ["text-muted"] = new("#5c6370", "#9aa3b2"),
            ["primary"] = new("#3b5bdb", "#748ffc"),
            ["on-primary"] = new("#ffffff", "#0f1115"),
            ["accent"] = new("#0ca678", "#38d9a9"),
            ["border"] = new("#dee2e6", "#2c3340"),
        }),
        new Typography(
            Heading: FontStack.Sans,
            Body: FontStack.Sans,
            BaseSizePx: 16,
            ScaleRatio: 1.25,
            RadiusPx: 8,
            Spacing: SpacingScale.Comfortable));
}

/// <summary>A color token: one semantic role, two values. Values are validated CSS colors.</summary>
public sealed record ThemeToken(string Light, string Dark);

public sealed record Typography(
    FontStack Heading,
    FontStack Body,
    int BaseSizePx,
    double ScaleRatio,
    int RadiusPx,
    SpacingScale Spacing)
{
    public const int MinBaseSizePx = 14;
    public const int MaxBaseSizePx = 20;
    public const double MinScaleRatio = 1.125;
    public const double MaxScaleRatio = 1.5;
    public const int MaxRadiusPx = 24;

    public bool IsValid =>
        BaseSizePx is >= MinBaseSizePx and <= MaxBaseSizePx &&
        ScaleRatio is >= MinScaleRatio and <= MaxScaleRatio &&
        RadiusPx is >= 0 and <= MaxRadiusPx;
}

/// <summary>
/// Curated system font stacks — zero requests, zero layout shift, zero third parties.
/// The CSS for each lives in the rendering layer; the domain only knows the choice.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FontStack { Sans, Humanist, Geometric, Serif, Slab, Mono }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SpacingScale { Compact, Comfortable, Spacious }

/// <summary>The closed set of semantic token names (docs/domain-model.md §1).</summary>
public static class ThemeTokens
{
    public static readonly ImmutableArray<string> All =
    [
        "background", "surface", "surface-alt", "text", "text-muted",
        "primary", "on-primary", "accent", "border",
    ];

    public static bool IsKnown(string name) => All.Contains(name);
}

/// <summary>Syntactic validation for token color values (hex or a modern functional form).</summary>
public static partial class CssColor
{
    [GeneratedRegex(@"^(#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})|(?:rgb|hsl|oklch|color-mix)\([^;{}<>]{1,100}\))$")]
    private static partial Regex Syntax();

    public static bool IsValid(string value) => Syntax().IsMatch(value.Trim());
}

/// <summary>Token name → token map with structural equality. JSON form: an object.</summary>
[JsonConverter(typeof(TokenSetJsonConverter))]
public sealed class TokenSet : IEquatable<TokenSet>, IEnumerable<KeyValuePair<string, ThemeToken>>
{
    private readonly ImmutableSortedDictionary<string, ThemeToken> _tokens;

    private TokenSet(ImmutableSortedDictionary<string, ThemeToken> tokens) => _tokens = tokens;

    public static TokenSet Of(IEnumerable<KeyValuePair<string, ThemeToken>> tokens) =>
        new(ImmutableSortedDictionary.CreateRange(StringComparer.Ordinal, tokens));

    public ThemeToken? Get(string name) => _tokens.GetValueOrDefault(name);
    public TokenSet With(string name, ThemeToken token) => new(_tokens.SetItem(name, token));

    public IEnumerator<KeyValuePair<string, ThemeToken>> GetEnumerator() => _tokens.GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    public bool Equals(TokenSet? other) => other is not null && _tokens.SequenceEqual(other._tokens);
    public override bool Equals(object? obj) => Equals(obj as TokenSet);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var (name, token) in _tokens)
        {
            hash.Add(name, StringComparer.Ordinal);
            hash.Add(token);
        }

        return hash.ToHashCode();
    }

    internal sealed class TokenSetJsonConverter : JsonConverter<TokenSet>
    {
        public override TokenSet Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new(JsonSerializer.Deserialize<ImmutableSortedDictionary<string, ThemeToken>>(ref reader, options)!);

        public override void Write(Utf8JsonWriter writer, TokenSet value, JsonSerializerOptions options) =>
            JsonSerializer.Serialize(writer, value._tokens, options);
    }
}
