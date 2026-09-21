using System.Globalization;
using System.Net;
using System.Text.Json;

namespace Imprint.Publishing;

/// <summary>
/// The reading helpers every prerender template shares: another service's JSON, read defensively and
/// escaped on the way into our HTML.
/// </summary>
/// <remarks>
/// <para>★ EVERY ACCESSOR RETURNS A DEFAULT RATHER THAN THROWING, and that is the design. A template
/// runs at publish time inside the publisher; an exception there does not produce a broken card, it
/// takes out the publish of a page — or of a site. A missing field must cost one missing line.</para>
/// <para>★ <c>TryGetInt64</c>/<c>TryGetDouble</c> THROW on a JSON null rather than returning false,
/// so the <c>ValueKind</c> check in <see cref="Number"/> is load-bearing, not defensive noise. A null
/// where a number was expected is normal in these payloads — it means "not measured".</para>
/// </remarks>
internal static class TemplateJson
{
    /// <summary>The parsed root, or null when the body is absent or not JSON — never an exception.</summary>
    public static JsonElement? Root(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonDocument.Parse(json).RootElement;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static string Str(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object
        && e.TryGetProperty(name, out var v)
        && v.ValueKind == JsonValueKind.String
            ? v.GetString() ?? ""
            : "";

    public static double? Number(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object
        && e.TryGetProperty(name, out var v)
        && v.ValueKind == JsonValueKind.Number
        && v.TryGetDouble(out var n)
            ? n
            : null;

    public static bool Flag(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object
        && e.TryGetProperty(name, out var v)
        && v.ValueKind == JsonValueKind.True;

    /// <summary>The named array's elements, or empty — a missing list is an empty list.</summary>
    public static IReadOnlyList<JsonElement> Array(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object
        && e.TryGetProperty(name, out var v)
        && v.ValueKind == JsonValueKind.Array
            ? [.. v.EnumerateArray()]
            : [];

    /// <summary>The named array's string elements, blanks dropped.</summary>
    public static IReadOnlyList<string> Strings(JsonElement e, string name) =>
        [.. Array(e, name).Where(x => x.ValueKind == JsonValueKind.String)
            .Select(x => x.GetString() ?? "").Where(s => s.Length > 0)];

    /// <summary>A 0–100 figure with at most one decimal, and no trailing ".0" on a whole number.</summary>
    public static string Score(double value) =>
        value.ToString(Math.Abs(value - Math.Round(value)) < 0.05 ? "0" : "0.0", CultureInfo.InvariantCulture);

    public static string Group(double value) => value.ToString("#,##0", CultureInfo.InvariantCulture);

    /// <summary>
    /// ★ A COLOUR FROM A PAYLOAD IS STILL UNTRUSTED INPUT. It lands in a <c>style</c> attribute, which
    /// is a CSS injection sink: <c>WebUtility.HtmlEncode</c> would not stop <c>;background:url(…)</c>
    /// because none of those characters need escaping in HTML. Only a strict shape passes.
    /// </summary>
    public static string? HexColour(string? value) =>
        value is { Length: 7 } hex && hex[0] == '#' && hex[1..].All(Uri.IsHexDigit) ? hex : null;

    public static string Esc(string text) => WebUtility.HtmlEncode(text);

    /// <summary>
    /// An absolute https URL built from a base and a path the payload supplied, or null.
    /// ★ The scheme check is the point: a payload-supplied <c>javascript:</c> in an <c>href</c> is a
    /// script the page runs, and HTML-escaping does not touch it.
    /// </summary>
    public static string? Link(string? origin, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        // ★ A RELATIVE PATH MUST LOOK LIKE ONE. Concatenating an origin onto anything that merely
        //   fails to start with "http" turned `javascript:alert(1)` into
        //   `https://app…/javascript:alert(1)` — an https URL, so it survived the scheme check below,
        //   and shipped as a live link to a path that does not exist. A payload-supplied scheme is
        //   never a path: a colon before the first slash means the payload is naming a protocol.
        var scheme = Scheme(path);
        if (scheme is not null && !scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
        {
            // `javascript:`, `data:`, `http:` — a payload naming a protocol is never a path.
            return null;
        }

        var candidate = scheme is null && !path.StartsWith("//", StringComparison.Ordinal)
            ? $"{(origin ?? "").TrimEnd('/')}/{path.TrimStart('/')}"
            : path;

        return Uri.TryCreate(candidate, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps
            ? uri.ToString()
            : null;
    }

    /// <summary>The scheme a string names before its first slash, or null when it names none.</summary>
    private static string? Scheme(string value)
    {
        var colon = value.IndexOf(':', StringComparison.Ordinal);
        if (colon <= 0)
        {
            return null;
        }

        var slash = value.IndexOf('/', StringComparison.Ordinal);
        return slash >= 0 && slash < colon ? null : value[..colon];
    }
}
