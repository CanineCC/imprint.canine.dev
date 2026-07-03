using Imprint.Rendering;

namespace Imprint.Publishing.Tests.Rendering;

/// <summary>
/// The zero-JS light/dark technique at the view layer: an asset with a dark rendition
/// emits BOTH renditions carrying the <c>ip-img-light</c>/<c>ip-img-dark</c> class pair
/// (CSS reveals one), the dark copy is hidden from assistive tech, and a neutral asset
/// is untouched (docs/proposals/theme-media-and-widget-approval.md §Part 1).
/// </summary>
public sealed class DarkVariantViewTests
{
    private static RenderContext WithDarkImage(RenderMode mode) => RenderHarness.Context(mode) with
    {
        ResolveAsset = id => id == SampleNodes.ImageAssetId ? RenderHarness.ImageAssetWithDark() : null,
    };

    private static RenderContext WithNeutralImage(RenderMode mode) => RenderHarness.Context(mode) with
    {
        ResolveAsset = id => id == SampleNodes.ImageAssetId ? RenderHarness.ImageAsset() : null,
    };

    private static RenderContext WithDarkSvg(RenderMode mode) => RenderHarness.Context(mode) with
    {
        ResolveAsset = id => id == SampleNodes.SvgAssetId ? RenderHarness.SvgAssetWithDark() : null,
    };

    // -------------------------------------------------------------------- image

    [Fact]
    public async Task Dark_image_emits_both_renditions_with_the_light_dark_class_pair()
    {
        var html = await RenderHarness.RenderNode(WithDarkImage(RenderMode.Static), SampleNodes.Image());

        Assert.Contains("class=\"ip-img ip-img-light\"", html);
        Assert.Contains("class=\"ip-img ip-img-dark\"", html);
        Assert.Equal(2, CountImgs(html));
    }

    [Fact]
    public async Task Dark_image_carries_its_own_srcset_src_and_dimensions()
    {
        var html = await RenderHarness.RenderNode(WithDarkImage(RenderMode.Static), SampleNodes.Image());

        // Light rendition keeps the base URLs; dark rendition mirrors the shape from the Dark* fields.
        Assert.Contains("src=\"/assets/img-960.webp\"", html);
        Assert.Contains("src=\"/assets/img-dark-960.webp\"", html);
        Assert.Contains(
            "srcset=\"/assets/img-dark-480.webp 480w, /assets/img-dark-960.webp 960w, /assets/img-dark-1440.webp 1440w\"",
            html);
        // Both renditions reserve the same box (zero CLS): width/height appear twice.
        Assert.Equal(2, Occurrences(html, "width=\"1440\""));
        Assert.Equal(2, Occurrences(html, "height=\"960\""));
    }

    [Fact]
    public async Task Dark_image_is_aria_hidden_and_the_light_one_keeps_the_alt()
    {
        var html = await RenderHarness.RenderNode(WithDarkImage(RenderMode.Static), SampleNodes.Image(alt: "A dog"));

        Assert.Contains("alt=\"A dog\"", html);
        // Exactly one aria-hidden (the dark copy) and exactly one alt (the light copy).
        Assert.Equal(1, Occurrences(html, "aria-hidden=\"true\""));
        Assert.Equal(1, Occurrences(html, "alt=\""));
    }

    [Fact]
    public async Task Only_the_light_rendition_claims_the_eager_first_image_slot()
    {
        var page = SampleNodes.Section(SampleNodes.Stack(SampleNodes.Image()));

        var html = await RenderHarness.RenderPage(WithDarkImage(RenderMode.Static), page);

        // One node, one claim: the light copy is the LCP candidate, the dark copy stays lazy
        // so both renditions are never eagerly downloaded at once.
        Assert.Equal(1, Occurrences(html, "loading=\"eager\""));
        Assert.Equal(1, Occurrences(html, "fetchpriority=\"high\""));
        Assert.Equal(1, Occurrences(html, "loading=\"lazy\""));
    }

    [Fact]
    public async Task Neutral_image_is_a_single_unchanged_img_with_no_variant_classes()
    {
        var html = await RenderHarness.RenderNode(WithNeutralImage(RenderMode.Static), SampleNodes.Image());

        Assert.Equal(1, CountImgs(html));
        Assert.DoesNotContain("ip-img-light", html);
        Assert.DoesNotContain("ip-img-dark", html);
    }

    [Fact]
    public async Task Editor_mode_puts_the_node_id_on_the_light_rendition_only()
    {
        var image = SampleNodes.Image();

        var html = await RenderHarness.RenderNode(WithDarkImage(RenderMode.Editor), image);

        // One hit-target per node: the data-node-id rides the light (primary) copy alone.
        Assert.Equal(1, Occurrences(html, $"data-node-id=\"{image.Id.Compact}\""));
    }

    // ---------------------------------------------------------------------- svg

    [Fact]
    public async Task Dark_svg_emits_both_inline_wrappers_with_the_class_pair()
    {
        var html = await RenderHarness.RenderNode(WithDarkSvg(RenderMode.Static), SampleNodes.Svg(alt: "Company logo"));

        Assert.Contains("class=\"ip-svg ip-img-light\"", html);
        Assert.Contains("class=\"ip-svg ip-img-dark\"", html);
        // Both SVGs are inlined (light path, dark circle).
        Assert.Contains("<path d=\"M0 0h10v10z\"/>", html);
        Assert.Contains("<circle cx=\"5\" cy=\"5\" r=\"4\"/>", html);
    }

    [Fact]
    public async Task Dark_svg_wrapper_is_aria_hidden_and_the_light_one_keeps_the_name()
    {
        var html = await RenderHarness.RenderNode(WithDarkSvg(RenderMode.Static), SampleNodes.Svg(alt: "Company logo"));

        Assert.Contains("aria-label=\"Company logo\"", html);
        Assert.Contains("role=\"img\"", html);
        // The dark mirror is hidden so assistive tech announces the graphic once.
        Assert.Equal(1, Occurrences(html, "aria-hidden=\"true\""));
    }

    [Fact]
    public async Task Editor_mode_puts_the_node_id_on_the_light_svg_wrapper_only()
    {
        var svg = SampleNodes.Svg(alt: "Company logo");

        var html = await RenderHarness.RenderNode(WithDarkSvg(RenderMode.Editor), svg);

        Assert.Equal(1, Occurrences(html, $"data-node-id=\"{svg.Id.Compact}\""));
    }

    [Fact]
    public async Task Neutral_svg_is_a_single_unchanged_wrapper_with_no_variant_classes()
    {
        var ctx = RenderHarness.Context(RenderMode.Static) with
        {
            ResolveAsset = id => id == SampleNodes.SvgAssetId ? RenderHarness.SvgAsset() : null,
        };

        var html = await RenderHarness.RenderNode(ctx, SampleNodes.Svg(alt: "Company logo"));

        Assert.Contains("class=\"ip-svg\"", html);
        Assert.DoesNotContain("ip-img-light", html);
        Assert.DoesNotContain("ip-img-dark", html);
    }

    private static int CountImgs(string html) => Occurrences(html, "<img");

    private static int Occurrences(string haystack, string needle)
    {
        var count = 0;
        for (var i = haystack.IndexOf(needle, StringComparison.Ordinal); i >= 0;
             i = haystack.IndexOf(needle, i + needle.Length, StringComparison.Ordinal))
        {
            count++;
        }

        return count;
    }
}
