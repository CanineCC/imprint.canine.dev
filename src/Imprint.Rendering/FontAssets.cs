namespace Imprint.Rendering;

/// <summary>
/// The self-hosted variable fonts the marketing layer references via <c>@font-face</c>
/// (Schibsted Grotesk for UI/headings, JetBrains Mono for code and tabular numerals).
/// Embedded in this assembly and surfaced as (published path, bytes) pairs so the
/// publisher can write them into each site's output without a static-web-asset dependency
/// or an external CDN request — EU-resident, zero third parties. The published path is a
/// FIXED name (no content hash) because <c>imprint-marketing.css</c> references it
/// literally; the file bytes are immutable, so a stable name loses nothing.
/// </summary>
public static class FontAssets
{
    /// <summary>One shipped font file: the path under the site root and its bytes.</summary>
    public sealed record FontFile(string RelativePath, byte[] Bytes);

    private static IReadOnlyList<FontFile>? _all;

    /// <summary>Every font file the marketing stylesheet needs, in a deterministic order.</summary>
    public static IReadOnlyList<FontFile> All => _all ??=
    [
        Load("fonts/schibsted-var.woff2", "Imprint.Rendering.fonts.schibsted-var.woff2"),
        Load("fonts/jetbrains-var.woff2", "Imprint.Rendering.fonts.jetbrains-var.woff2"),
    ];

    private static FontFile Load(string relativePath, string resource)
    {
        using var stream = typeof(FontAssets).Assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"Embedded font '{resource}' is missing from Imprint.Rendering.");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return new FontFile(relativePath, memory.ToArray());
    }
}
