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

/// <summary>
/// Syntactic validation for token color values: hex, a bare keyword (named colors,
/// <c>transparent</c>, <c>currentColor</c>), or a color function. Deliberately strict —
/// these values are emitted verbatim into every visitor's <c>site.css</c>, so a value
/// that is not really a color (e.g. one smuggling a <c>url()</c>) would break both the
/// "validated color" claim and the zero-third-party-request guarantee.
/// </summary>
public static partial class CssColor
{
    // Hex or a color function. The function-body class excludes ':' and quotes, so an
    // absolute url() cannot appear; `url(` is then banned outright to also stop
    // relative ones. color-mix's nested color functions/hex are covered by allowing
    // '(', ')' and '#' in the body.
    [GeneratedRegex(
        @"^(#(?:[0-9a-fA-F]{3,4}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})" +
        @"|(?:rgb|rgba|hsl|hsla|hwb|lab|lch|oklab|oklch|color|color-mix)\([0-9a-zA-Z.,%\s/+()#-]{1,120}\))$")]
    private static partial Regex Syntax();

    // Bare keywords are validated against the real set — "validated color" should mean
    // it, so 'reddish' is rejected even though 'red' is accepted. transparent and
    // currentcolor are the two most useful non-hex tokens for a theme.
    private static readonly HashSet<string> NamedColors = new(StringComparer.OrdinalIgnoreCase)
    {
        "transparent", "currentcolor",
        "aliceblue", "antiquewhite", "aqua", "aquamarine", "azure", "beige", "bisque", "black",
        "blanchedalmond", "blue", "blueviolet", "brown", "burlywood", "cadetblue", "chartreuse",
        "chocolate", "coral", "cornflowerblue", "cornsilk", "crimson", "cyan", "darkblue", "darkcyan",
        "darkgoldenrod", "darkgray", "darkgreen", "darkgrey", "darkkhaki", "darkmagenta", "darkolivegreen",
        "darkorange", "darkorchid", "darkred", "darksalmon", "darkseagreen", "darkslateblue",
        "darkslategray", "darkslategrey", "darkturquoise", "darkviolet", "deeppink", "deepskyblue",
        "dimgray", "dimgrey", "dodgerblue", "firebrick", "floralwhite", "forestgreen", "fuchsia",
        "gainsboro", "ghostwhite", "gold", "goldenrod", "gray", "green", "greenyellow", "grey",
        "honeydew", "hotpink", "indianred", "indigo", "ivory", "khaki", "lavender", "lavenderblush",
        "lawngreen", "lemonchiffon", "lightblue", "lightcoral", "lightcyan", "lightgoldenrodyellow",
        "lightgray", "lightgreen", "lightgrey", "lightpink", "lightsalmon", "lightseagreen",
        "lightskyblue", "lightslategray", "lightslategrey", "lightsteelblue", "lightyellow", "lime",
        "limegreen", "linen", "magenta", "maroon", "mediumaquamarine", "mediumblue", "mediumorchid",
        "mediumpurple", "mediumseagreen", "mediumslateblue", "mediumspringgreen", "mediumturquoise",
        "mediumvioletred", "midnightblue", "mintcream", "mistyrose", "moccasin", "navajowhite", "navy",
        "oldlace", "olive", "olivedrab", "orange", "orangered", "orchid", "palegoldenrod", "palegreen",
        "paleturquoise", "palevioletred", "papayawhip", "peachpuff", "peru", "pink", "plum", "powderblue",
        "purple", "rebeccapurple", "red", "rosybrown", "royalblue", "saddlebrown", "salmon", "sandybrown",
        "seagreen", "seashell", "sienna", "silver", "skyblue", "slateblue", "slategray", "slategrey",
        "snow", "springgreen", "steelblue", "tan", "teal", "thistle", "tomato", "turquoise", "violet",
        "wheat", "white", "whitesmoke", "yellow", "yellowgreen",
    };

    public static bool IsValid(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length > 128 || trimmed.Contains("url(", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return NamedColors.Contains(trimmed) || Syntax().IsMatch(trimmed);
    }
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
