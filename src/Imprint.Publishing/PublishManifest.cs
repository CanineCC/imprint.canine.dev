using System.Text.Json;
using System.Text.Json.Serialization;

namespace Imprint.Publishing;

/// <summary>
/// The projection's durable checkpoint (docs/publishing.md §Publish manifest): per
/// page, the publish version and site (chrome) version it rendered, the public paths
/// it occupies and the content hashes of the assets it references. Staleness on the
/// next pass is "manifest vs. current read models"; deleting it (or the whole output
/// directory) is a full republish by construction. Serialization is deterministic —
/// sorted keys, stable formatting — because an unchanged site must produce an
/// unchanged manifest byte-for-byte (the zero-rewrite guarantee covers this file too).
/// </summary>
public sealed record PublishManifest
{
    public const string FileName = "publish-manifest.json";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public int SchemaVersion { get; init; } = 1;

    /// <summary>The Site stream version the whole output reflects — the chrome version.</summary>
    public long SiteVersion { get; init; }

    /// <summary>Keyed by the page id's compact ("N") form.</summary>
    public IReadOnlyDictionary<string, PageEntry> Pages { get; init; } =
        new SortedDictionary<string, PageEntry>(StringComparer.Ordinal);

    /// <summary>hash16 of the published stylesheet's content (also part of its file name).</summary>
    public string CssHash { get; init; } = "";

    /// <summary>Tag → bundle content hash16, for widgets actually used on published pages.</summary>
    public IReadOnlyDictionary<string, string> WidgetBundles { get; init; } =
        new SortedDictionary<string, string>(StringComparer.Ordinal);

    public sealed record PageEntry
    {
        public long PublishedVersion { get; init; }

        public long RenderedAtSiteVersion { get; init; }

        /// <summary>Public directory paths this page occupies, default locale first (<c>/</c>, <c>/da/</c>).</summary>
        public IReadOnlyList<string> Paths { get; init; } = [];

        /// <summary>Sorted content hashes of every referenced publishable asset — the asset-staleness key.</summary>
        public IReadOnlyList<string> AssetHashes { get; init; } = [];

        /// <summary>A render failure surfaced to the editor; null when the page published cleanly.</summary>
        public string? Error { get; init; }
    }

    public byte[] ToUtf8Json() => JsonSerializer.SerializeToUtf8Bytes(this, JsonOptions);

    /// <summary>
    /// Loads the checkpoint, tolerating absence and corruption: a manifest that cannot
    /// be trusted is treated as no manifest, which degrades to a full republish — the
    /// safe direction for a projection whose store is a directory.
    /// </summary>
    public static PublishManifest? Load(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return null;
            }

            var manifest = JsonSerializer.Deserialize<PublishManifest>(File.ReadAllBytes(path), JsonOptions);
            return manifest is { SchemaVersion: 1 } ? manifest : null;
        }
        catch (Exception e) when (e is IOException or JsonException or NotSupportedException)
        {
            return null;
        }
    }
}
