using System.Text;
using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Assets;
using SkiaSharp;

namespace Imprint.Media.Tests;

public sealed class SkiaMediaProcessorImageTests : IDisposable
{
    private readonly TempMediaRoot _root = new();
    private readonly DiskMediaStore _store;
    private readonly SkiaMediaProcessor _processor;
    private readonly AssetId _id = AssetId.New();

    public SkiaMediaProcessorImageTests()
    {
        _store = new DiskMediaStore(_root.Options);
        _processor = new SkiaMediaProcessor(_store, _root.Options);
    }

    public void Dispose() => _root.Dispose();

    private async Task<string> Upload(byte[] bytes, string fileName = "image.png") =>
        await _store.SaveOriginal(_id, fileName, new MemoryStream(bytes));

    private async Task<byte[]> StoredBytes(string storageKey)
    {
        await using var stream = await _store.Open(storageKey);
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer);
        return buffer.ToArray();
    }

    [Fact]
    public async Task GenerateImageVariants_wide_source_emits_every_target_width()
    {
        var key = await Upload(TestImages.GradientPng(2400, 1200));

        var variants = await _processor.GenerateImageVariants(_id, key);

        Assert.Equal([480, 960, 1440, 1920], variants.Select(v => v.Width));
        Assert.Equal([240, 480, 720, 960], variants.Select(v => v.Height));
    }

    [Fact]
    public async Task GenerateImageVariants_never_upscales_past_the_source_width()
    {
        var key = await Upload(TestImages.GradientPng(1000, 500));

        var variants = await _processor.GenerateImageVariants(_id, key);

        Assert.Equal([480, 960], variants.Select(v => v.Width));
        Assert.All(variants, v => Assert.True(v.Width <= 1000));
    }

    [Fact]
    public async Task GenerateImageVariants_narrow_source_emits_single_variant_at_source_width()
    {
        var key = await Upload(TestImages.GradientPng(320, 200));

        var variants = await _processor.GenerateImageVariants(_id, key);

        var variant = Assert.Single(variants);
        Assert.Equal(320, variant.Width);
        Assert.Equal(200, variant.Height);
    }

    [Fact]
    public async Task GenerateImageVariants_source_exactly_on_a_rung_is_reencoded_not_resized_away()
    {
        var key = await Upload(TestImages.GradientPng(480, 240));

        var variants = await _processor.GenerateImageVariants(_id, key);

        var variant = Assert.Single(variants);
        Assert.Equal(480, variant.Width);
        Assert.Equal(240, variant.Height);
    }

    [Fact]
    public async Task GenerateImageVariants_output_is_webp_with_riff_magic_bytes()
    {
        var key = await Upload(TestImages.GradientJpeg(1500, 1000), "photo.jpg");

        var variants = await _processor.GenerateImageVariants(_id, key);

        foreach (var variant in variants)
        {
            var bytes = await StoredBytes(variant.StorageKey);
            Assert.Equal("RIFF", Encoding.ASCII.GetString(bytes, 0, 4));
            Assert.Equal("WEBP", Encoding.ASCII.GetString(bytes, 8, 4));
            Assert.Equal(bytes.LongLength, variant.ByteSize);
        }
    }

    [Fact]
    public async Task GenerateImageVariants_reported_dimensions_match_the_encoded_pixels()
    {
        var key = await Upload(TestImages.GradientPng(1500, 1000));

        var variants = await _processor.GenerateImageVariants(_id, key);

        // 1500:1000 must stay 3:2 on every rung.
        Assert.Equal([480, 960, 1440], variants.Select(v => v.Width));
        Assert.Equal([320, 640, 960], variants.Select(v => v.Height));
        foreach (var variant in variants)
        {
            using var decoded = SKBitmap.Decode(await StoredBytes(variant.StorageKey));
            Assert.NotNull(decoded);
            Assert.Equal(variant.Width, decoded.Width);
            Assert.Equal(variant.Height, decoded.Height);
        }
    }

    [Fact]
    public async Task GenerateImageVariants_honors_exif_orientation_by_swapping_axes()
    {
        // Orientation 6 (RightTop) = camera held portrait: 200x100 stored pixels
        // display as 100x200. The pipeline must emit upright variants.
        var rotated = TestImages.WithExifOrientation(TestImages.GradientJpeg(200, 100), orientation: 6);
        var key = await Upload(rotated, "portrait.jpg");

        var variants = await _processor.GenerateImageVariants(_id, key);

        var variant = Assert.Single(variants);
        Assert.Equal(100, variant.Width);
        Assert.Equal(200, variant.Height);
        using var decoded = SKBitmap.Decode(await StoredBytes(variant.StorageKey));
        Assert.NotNull(decoded);
        Assert.Equal(100, decoded.Width);
        Assert.Equal(200, decoded.Height);
    }

    [Fact]
    public async Task GenerateImageVariants_non_image_input_fails_with_a_clear_message()
    {
        var key = await Upload(Encoding.UTF8.GetBytes("definitely not pixels"), "fake.png");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _processor.GenerateImageVariants(_id, key));

        Assert.Contains("could not be decoded as an image", exception.Message);
    }

    [Fact]
    public async Task GenerateImageVariants_stores_derivatives_under_the_asset_id()
    {
        var key = await Upload(TestImages.GradientPng(960, 480));

        var variants = await _processor.GenerateImageVariants(_id, key);

        Assert.All(variants, v => Assert.StartsWith($"derived/{_id.Compact}/", v.StorageKey, StringComparison.Ordinal));
        Assert.Equal(variants.Count, variants.Select(v => v.StorageKey).Distinct().Count());
    }

    [Fact]
    public async Task SanitizeSvg_stores_a_sanitized_derivative_and_reports_removals()
    {
        const string svg =
            """
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 10 10"><script>alert(1)</script><circle r="4" onclick="pwn()"/></svg>
            """;
        var key = await _store.SaveOriginal(_id, "icon.svg", new MemoryStream(Encoding.UTF8.GetBytes(svg)));

        var (storageKey, removedNodes) = await _processor.SanitizeSvg(_id, key);

        Assert.Equal($"derived/{_id.Compact}/sanitized.svg", storageKey);
        Assert.Equal(2, removedNodes);
        var sanitized = await _store.ReadAllText(storageKey);
        Assert.DoesNotContain("script", sanitized);
        Assert.DoesNotContain("onclick", sanitized);
        Assert.Contains("circle", sanitized);
    }

    [Fact]
    public async Task Dark_image_variants_use_keys_distinct_from_the_base()
    {
        var key = await Upload(TestImages.GradientPng(1200, 800));

        var light = await _processor.GenerateImageVariants(_id, key, dark: false);
        var dark = await _processor.GenerateImageVariants(_id, key, dark: true);

        // Same asset id and widths, but the dark rendition must NEVER share a storage key
        // with the base — the dark run happens after the base and File.WriteAllBytes would
        // otherwise truncate the light files, corrupting the base rendition permanently.
        var lightKeys = light.Select(v => v.StorageKey).ToHashSet(StringComparer.Ordinal);
        Assert.All(dark, v => Assert.DoesNotContain(v.StorageKey, lightKeys));
        Assert.Equal(light.Count, dark.Count);
    }

    [Fact]
    public async Task Dark_sanitized_svg_uses_a_key_distinct_from_the_base()
    {
        const string svg =
            """
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 10 10"><circle r="4"/></svg>
            """;
        var key = await _store.SaveOriginal(_id, "icon.svg", new MemoryStream(Encoding.UTF8.GetBytes(svg)));

        var (lightKey, _) = await _processor.SanitizeSvg(_id, key, dark: false);
        var (darkKey, _) = await _processor.SanitizeSvg(_id, key, dark: true);

        Assert.NotEqual(lightKey, darkKey);
    }
}
