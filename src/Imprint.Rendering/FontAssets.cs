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
        // Assure's face, named only by imprint-site-assure.css, which ships to the Assure site alone.
        Load("fonts/figtree-var.woff2", "Imprint.Rendering.fonts.figtree-var.woff2"),
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

    private static IReadOnlyList<FontFile>? _assay;
    private static IReadOnlyList<FontFile>? _canineCodeAnalyzer;

    /// <summary>
    /// The faces only one site's own sheet names, written only on that site: Assay's Source Serif 4 with
    /// its full weight and optical-size axes (the brand sets headlines at 800, past the resource center's
    /// 400 to 700 instance), the resource center's italic for interpretation lines, and Source Code Pro
    /// for the measurement details; CCA's Atkinson Hyperlegible Next and Mono, for headlines, text and
    /// code. Keyed by <see cref="ThemeCss.SiteSlug"/>, as the site's sheet is.
    /// ★ Keyed "assay" since the new site took the Assay name and assay.canine.dev; it was built as
    /// "assaypreview" so the old site's pages could not change before then.
    /// </summary>
    public static IReadOnlyList<FontFile> ForSite(string siteSlug) => siteSlug switch
    {
        "assay" => _assay ??=
        [
            Load("fonts/sourceserif4-opsz-var.woff2", "Imprint.Rendering.fonts.sourceserif4-opsz-var.woff2"),
            Load("fonts/sourceserif4-italic-var.woff2", "Imprint.Rendering.fonts.sourceserif4-italic-var.woff2"),
            Load("fonts/sourcecodepro-var.woff2", "Imprint.Rendering.fonts.sourcecodepro-var.woff2"),
        ],
        "caninecodeanalyzer" => _canineCodeAnalyzer ??=
        [
            Load("fonts/atkinson-next-var.woff2", "Imprint.Rendering.fonts.atkinson-next-var.woff2"),
            Load("fonts/atkinson-mono-var.woff2", "Imprint.Rendering.fonts.atkinson-mono-var.woff2"),
        ],
        _ => [],
    };

    private static FontFile Load(string relativePath, string resource)
    {
        using var stream = typeof(FontAssets).Assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"Embedded asset '{resource}' is missing from Imprint.Rendering.");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return new FontFile(relativePath, memory.ToArray());
    }
}
