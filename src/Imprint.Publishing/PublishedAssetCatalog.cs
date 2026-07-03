using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Assets;
using Imprint.Authoring.Features.Assets;
using Imprint.Authoring.Projections;
using Imprint.Rendering;
using Microsoft.Extensions.Logging;

namespace Imprint.Publishing;

/// <summary>
/// Resolves every asset referenced by published pages into its delivery form: the
/// files to place under <c>assets/</c>, the render info the node views consume, and
/// the content hashes the manifest uses as the asset-staleness key. Built once per
/// synchronize pass (hashing is I/O and <see cref="RenderContext.ResolveAsset"/> must
/// be synchronous), so a render never blocks on the media store.
/// </summary>
internal sealed class PublishedAssetCatalog
{
    /// <summary>A file the published site needs: where it goes and where its bytes live.</summary>
    public sealed record AssetFile(string RelativePath, string StorageKey);

    public sealed record Entry(AssetRenderInfo? Info, IReadOnlyList<AssetFile> Files, IReadOnlyList<string> Hashes)
    {
        public static readonly Entry Unpublishable = new(null, [], []);
    }

    private readonly Dictionary<AssetId, Entry> _entries = [];

    private PublishedAssetCatalog()
    {
    }

    public AssetRenderInfo? Resolve(AssetId id) => _entries.GetValueOrDefault(id)?.Info;

    public IEnumerable<AssetFile> Files => _entries.Values.SelectMany(entry => entry.Files);

    /// <summary>Sorted, distinct content hashes for a set of referenced assets — a page's staleness key.</summary>
    public IReadOnlyList<string> HashesOf(IEnumerable<AssetId> assetIds) =>
        [.. assetIds.SelectMany(id => _entries.GetValueOrDefault(id)?.Hashes ?? []).Distinct().Order(StringComparer.Ordinal)];

    public static async Task<PublishedAssetCatalog> Build(
        IEnumerable<AssetId> referenced,
        AssetLibrary library,
        IMediaStore media,
        ILogger logger,
        CancellationToken ct)
    {
        var catalog = new PublishedAssetCatalog();
        foreach (var id in referenced.Distinct())
        {
            try
            {
                catalog._entries[id] = await ResolveOne(id, library, media, logger, ct);
            }
            catch (Exception e) when (e is IOException or FileNotFoundException or UnauthorizedAccessException)
            {
                // A Ready asset whose bytes are unreadable is media-store corruption:
                // degrade to absence (the views render nothing) instead of failing the
                // page — a visitor page with a hole beats no visitor page at all.
                logger.LogWarning(e, "Asset {AssetId} could not be read from the media store; publishing without it.", id);
                catalog._entries[id] = Entry.Unpublishable;
            }
        }

        return catalog;
    }

    private static async Task<Entry> ResolveOne(
        AssetId id, AssetLibrary library, IMediaStore media, ILogger logger, CancellationToken ct)
    {
        var asset = library.Get(id);
        if (asset is null)
        {
            return Entry.Unpublishable;
        }

        return (asset.Kind, asset.Status) switch
        {
            (AssetKind.Image, AssetStatus.Ready) => await ResolveImage(asset, media, ct),
            (AssetKind.Video, AssetStatus.Ready) when asset.DerivedStorageKey is { } webm =>
                await ResolveSingleFile(asset, webm, ".webm", media, ct),
            // ReadyDegraded video (no ffmpeg): the original file ships as-is — the
            // documented graceful floor, with the editor showing why.
            (AssetKind.Video, AssetStatus.ReadyDegraded) =>
                await ResolveSingleFile(asset, asset.OriginalStorageKey, OriginalExtension(asset), media, ct),
            (AssetKind.Vector, AssetStatus.Ready) when asset.DerivedStorageKey is { } sanitized =>
                await ResolveInlineSvg(asset, sanitized, media, logger, ct),
            // File-kind assets are publishable the moment they are uploaded.
            (AssetKind.File, AssetStatus.Ready) =>
                await ResolveSingleFile(asset, asset.OriginalStorageKey, OriginalExtension(asset), media, ct),
            // Pending/Failed never publish; a degraded (unsanitized!) SVG must not be
            // inlined, and a degraded image has no variants to render.
            _ => Entry.Unpublishable,
        };
    }

    private static async Task<Entry> ResolveImage(Asset asset, IMediaStore media, CancellationToken ct)
    {
        var files = new List<AssetFile>();
        var hashes = new List<string>();
        var sources = new List<ImageSource>();
        foreach (var variant in asset.Variants.OrderBy(v => v.Width))
        {
            var hash = await HashOf(media, variant.StorageKey, ct);
            var relative = $"assets/{asset.Id.Compact}-{variant.Width}.{hash}.webp";
            files.Add(new AssetFile(relative, variant.StorageKey));
            hashes.Add(hash);
            sources.Add(new ImageSource($"/{relative}", variant.Width, variant.Height));
        }

        if (sources.Count == 0)
        {
            return Entry.Unpublishable;
        }

        var largest = sources[^1];
        // Url mirrors ImageView's own src choice (middle variant) for any consumer
        // that wants a single representative URL.
        var info = new AssetRenderInfo(
            AssetKind.Image, AssetStatus.Ready,
            sources[(sources.Count - 1) / 2].Url,
            sources, largest.Width, largest.Height,
            InlineSvg: null, asset.DefaultAlt);
        return new Entry(info, files, hashes);
    }

    private static async Task<Entry> ResolveSingleFile(
        Asset asset, string storageKey, string extension, IMediaStore media, CancellationToken ct)
    {
        var hash = await HashOf(media, storageKey, ct);
        var relative = $"assets/{asset.Id.Compact}.{hash}{extension}";
        var info = new AssetRenderInfo(
            asset.Kind, asset.Status, $"/{relative}", [], null, null, InlineSvg: null, asset.DefaultAlt);
        return new Entry(info, [new AssetFile(relative, storageKey)], [hash]);
    }

    private static async Task<Entry> ResolveInlineSvg(
        Asset asset, string sanitizedKey, IMediaStore media, ILogger logger, CancellationToken ct)
    {
        var svg = await media.ReadAllText(sanitizedKey, ct);
        if (!SvgPublishGuard.IsSafe(svg))
        {
            logger.LogWarning(
                "Sanitized SVG for asset {AssetId} failed the publish-time safety re-check; publishing without it.",
                asset.Id);
            return Entry.Unpublishable;
        }

        // SVGs are only ever inlined (SvgNode is the sole consumer, and inlining is
        // what lets strokes inherit currentColor) — no file lands in assets/, but the
        // content hash still participates in page staleness: a re-sanitized SVG must
        // re-render the pages that embed it.
        var hash = Hashing.Hash16(System.Text.Encoding.UTF8.GetBytes(svg));
        var info = new AssetRenderInfo(
            AssetKind.Vector, AssetStatus.Ready, "", [], null, null, svg, asset.DefaultAlt);
        return new Entry(info, [], [hash]);
    }

    private static async Task<string> HashOf(IMediaStore media, string storageKey, CancellationToken ct)
    {
        await using var stream = await media.Open(storageKey, ct);
        return await Hashing.Hash16(stream, ct);
    }

    // Extensions become part of a public URL: keep them boring, fall back to .bin.
    private static string OriginalExtension(Asset asset)
    {
        var extension = Path.GetExtension(asset.FileName).ToLowerInvariant();
        return extension.Length > 1 && extension[1..].All(char.IsAsciiLetterOrDigit) ? extension : ".bin";
    }
}
