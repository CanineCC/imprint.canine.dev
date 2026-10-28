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
    /// <summary>One shipped static file: the path under the site root and its bytes.</summary>
    public sealed record FontFile(string RelativePath, byte[] Bytes);

    private static IReadOnlyList<FontFile>? _all;

    /// <summary>Every font file the marketing stylesheet needs, in a deterministic order.</summary>
    public static IReadOnlyList<FontFile> All => _all ??=
    [
        Load("fonts/schibsted-var.woff2", "Imprint.Rendering.fonts.schibsted-var.woff2"),
        Load("fonts/jetbrains-var.woff2", "Imprint.Rendering.fonts.jetbrains-var.woff2"),
        // Assure's faces, named only by imprint-site-assure.css, which ships to the Assure site alone.
        Load("fonts/bricolage-var.woff2", "Imprint.Rendering.fonts.bricolage-var.woff2"),
        Load("fonts/inter-var.woff2", "Imprint.Rendering.fonts.inter-var.woff2"),
        // Assert's label face, named only by imprint-site-assert.css, which ships to the Assert site alone.
        Load("fonts/martian-var.woff2", "Imprint.Rendering.fonts.martian-var.woff2"),
        // The brand mark. A FILE rather than a data URI in the stylesheet: the sheet is
        // render-blocking and on a strict size budget, and a mask image is neither — it also
        // caches once for the whole site instead of riding every stylesheet revision.
        Load("brand/canine-badge.svg", "Imprint.Rendering.brand.canine-badge.svg"),
        // ★★ AND THE STANDARD'S OWN MARK, for the same reason and with the same consequence if it is
        //    forgotten: a link card referencing a file no site writes renders a BROKEN IMAGE, and the
        //    markup looks perfectly correct in a diff. This list is the only thing that puts a file
        //    under a published site root — adding the file to wwwroot is not enough.
        Load("brand/cai-mark.svg", "Imprint.Rendering.brand.cai-mark.svg"),
    ];

    private static IReadOnlyList<FontFile>? _resourceCenter;

    /// <summary>
    /// The resource center's reading serif, Source Serif 4 (SIL Open Font License 1.1, Adobe), roman
    /// and italic: written only on a resource-center site, whose sheet is the only one that names it.
    /// Instanced from the Google Fonts variable files with the optical-size axis pinned at 20 (reading
    /// text) and weight kept at 400 to 700, then subset to Latin: 33 KB each instead of 122 and 130.
    /// </summary>
    public static IReadOnlyList<FontFile> ResourceCenter => _resourceCenter ??=
    [
        Load("fonts/sourceserif4-var.woff2", "Imprint.Rendering.fonts.sourceserif4-var.woff2"),
        Load("fonts/sourceserif4-italic-var.woff2", "Imprint.Rendering.fonts.sourceserif4-italic-var.woff2"),
    ];

    private static FontFile Load(string relativePath, string resource)
    {
        using var stream = typeof(FontAssets).Assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"Embedded asset '{resource}' is missing from Imprint.Rendering.");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return new FontFile(relativePath, memory.ToArray());
    }
}
