using System.Text;
using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Assets;
using Imprint.Authoring.Features.Assets;
using SkiaSharp;

namespace Imprint.Media;

/// <summary>
/// SkiaSharp-backed <see cref="IMediaProcessor"/>: WebP responsive variants for raster
/// images, <see cref="SvgSanitizer"/> for vectors, external ffmpeg for video. Failures
/// throw with human-readable messages — the processing worker records them verbatim as
/// <c>asset.processing-failed</c>, and editors read them in the asset panel.
/// </summary>
public sealed class SkiaMediaProcessor : IMediaProcessor
{
    // The srcset ladder from docs/domain-model.md §3: phone, tablet, laptop, desktop.
    private static readonly int[] TargetWidths = [480, 960, 1440, 1920];

    private readonly IMediaStore _store;
    private readonly MediaOptions _options;
    private readonly FfmpegVideoTranscoder _transcoder;

    public SkiaMediaProcessor(IMediaStore store, MediaOptions options)
    {
        _store = store;
        _options = options;
        _transcoder = new FfmpegVideoTranscoder(options);
    }

    public string? VideoUnavailableReason => _transcoder.UnavailableReason;

    public async Task<IReadOnlyList<ImageVariant>> GenerateImageVariants(AssetId id, string originalKey, bool dark = false, CancellationToken ct = default)
    {
        await using var original = await _store.Open(originalKey, ct);

        using var codec = SKCodec.Create(original, out var decodeResult)
            ?? throw new InvalidOperationException(
                $"The file could not be decoded as an image ({decodeResult}). " +
                "Upload a raster format such as PNG, JPEG, WebP or GIF.");

        // Force a uniform pixel layout so palette PNGs, grayscale JPEGs etc. all
        // resize and WebP-encode the same way.
        var decodeInfo = codec.Info
            .WithColorType(SKColorType.Rgba8888)
            .WithAlphaType(SKAlphaType.Premul);
        using var decoded = SKBitmap.Decode(codec, decodeInfo)
            ?? throw new InvalidOperationException("The image data is corrupt and could not be decoded.");

        // Cameras store sensor-oriented pixels plus an EXIF origin; bake the rotation
        // in now so every variant (and its reported dimensions) is upright.
        using var reoriented = codec.EncodedOrigin == SKEncodedOrigin.TopLeft
            ? null
            : Reorient(decoded, codec.EncodedOrigin);
        var source = reoriented ?? decoded;

        var widths = TargetWidths.Where(width => width <= source.Width).ToArray();
        if (widths.Length == 0)
        {
            // Never upscale: a source narrower than the smallest rung ships as-is
            // (still re-encoded to WebP).
            widths = [source.Width];
        }

        var sampling = new SKSamplingOptions(SKCubicResampler.Mitchell);
        var variants = new List<ImageVariant>(widths.Length);
        foreach (var width in widths)
        {
            ct.ThrowIfCancellationRequested();
            var height = Math.Max(1, (int)Math.Round(source.Height * (double)width / source.Width));
            using var resized = width == source.Width
                ? null
                : source.Resize(new SKImageInfo(width, height, source.ColorType, source.AlphaType), sampling)
                    ?? throw new InvalidOperationException($"The image could not be resized to {width}px.");
            var bitmap = resized ?? source;

            using var encoded = bitmap.Encode(SKEncodedImageFormat.Webp, _options.WebPQuality)
                ?? throw new InvalidOperationException($"The {width}px variant could not be encoded as WebP.");
            var bytes = encoded.ToArray();
            // Dark renditions carry a distinct name so they never collide with (and
            // overwrite) the base variant's key under the same asset's derived/ folder.
            var key = await _store.SaveDerived(id, $"{DerivedPrefix(dark)}{width}.webp", bytes, ct);
            variants.Add(new ImageVariant(bitmap.Width, bitmap.Height, key, bytes.LongLength));
        }

        return variants;
    }

    public async Task<(string StorageKey, int RemovedNodes)> SanitizeSvg(AssetId id, string originalKey, bool dark = false, CancellationToken ct = default)
    {
        var svg = await _store.ReadAllText(originalKey, ct);
        var (sanitized, removedNodes) = SvgSanitizer.Sanitize(svg);
        // Distinct name for the dark rendition — see GenerateImageVariants.
        var key = await _store.SaveDerived(id, $"{DerivedPrefix(dark)}sanitized.svg", Encoding.UTF8.GetBytes(sanitized), ct);
        return (key, removedNodes);
    }

    // The one place light and dark derived names diverge: a "dark-" prefix keeps both
    // renditions under derived/{id}/ with non-colliding keys, so processing the dark
    // variant (which always runs after the base is Ready) can never truncate the base.
    private static string DerivedPrefix(bool dark) => dark ? "dark-" : "";

    public async Task<(string StorageKey, long ByteSize)?> TranscodeToWebM(AssetId id, string originalKey, CancellationToken ct = default)
    {
        var inputPath = _store.PhysicalPathOf(originalKey);
        var webm = await _transcoder.Transcode(inputPath, ct);
        if (webm is null)
        {
            return null;
        }

        var key = await _store.SaveDerived(id, "video.webm", webm, ct);
        return (key, webm.LongLength);
    }

    private static SKBitmap Reorient(SKBitmap source, SKEncodedOrigin origin)
    {
        var swapsAxes = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop
            or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
        var upright = new SKBitmap(new SKImageInfo(
            swapsAxes ? source.Height : source.Width,
            swapsAxes ? source.Width : source.Height,
            source.ColorType,
            source.AlphaType));

        using var canvas = new SKCanvas(upright);
        switch (origin)
        {
            case SKEncodedOrigin.TopRight:
                canvas.Translate(source.Width, 0);
                canvas.Scale(-1, 1);
                break;
            case SKEncodedOrigin.BottomRight:
                canvas.Translate(source.Width, source.Height);
                canvas.RotateDegrees(180);
                break;
            case SKEncodedOrigin.BottomLeft:
                canvas.Translate(0, source.Height);
                canvas.Scale(1, -1);
                break;
            case SKEncodedOrigin.LeftTop:
                canvas.RotateDegrees(90);
                canvas.Scale(1, -1);
                break;
            case SKEncodedOrigin.RightTop:
                canvas.Translate(source.Height, 0);
                canvas.RotateDegrees(90);
                break;
            case SKEncodedOrigin.RightBottom:
                canvas.Translate(source.Height, source.Width);
                canvas.RotateDegrees(90);
                canvas.Scale(-1, 1);
                break;
            case SKEncodedOrigin.LeftBottom:
                canvas.Translate(0, source.Width);
                canvas.RotateDegrees(-90);
                break;
            default:
                break;
        }

        // The transform is a pure 90°/180° pixel permutation — default (nearest)
        // sampling is exact; anything fancier would only blur.
        canvas.DrawBitmap(source, 0, 0, SKSamplingOptions.Default);
        return upright;
    }
}
