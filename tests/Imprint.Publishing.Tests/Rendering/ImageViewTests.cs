using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Rendering;

namespace Imprint.Publishing.Tests.Rendering;

public sealed class ImageViewTests
{
    private static RenderContext WithImage(RenderMode mode) => RenderHarness.Context(mode) with
    {
        ResolveAsset = id => id == SampleNodes.ImageAssetId ? RenderHarness.ImageAsset() : null,
    };

    [Fact]
    public async Task Static_image_renders_middle_src_full_srcset_and_lazy_defaults()
    {
        var html = await RenderHarness.RenderNode(WithImage(RenderMode.Static), SampleNodes.Image());

        Assert.Contains("src=\"/assets/img-960.webp\"", html);
        Assert.Contains("srcset=\"/assets/img-480.webp 480w, /assets/img-960.webp 960w, /assets/img-1440.webp 1440w\"", html);
        Assert.Contains("loading=\"lazy\"", html);
        Assert.Contains("decoding=\"async\"", html);
        Assert.DoesNotContain("fetchpriority", html);
    }

    [Fact]
    public async Task Width_and_height_come_from_the_largest_variant()
    {
        var html = await RenderHarness.RenderNode(WithImage(RenderMode.Static), SampleNodes.Image());

        Assert.Contains("width=\"1440\"", html);
        Assert.Contains("height=\"960\"", html);
    }

    [Fact]
    public async Task First_image_on_a_page_is_eager_with_high_fetchpriority_and_later_images_stay_lazy()
    {
        var page = SampleNodes.Section(SampleNodes.Stack(SampleNodes.Image("first"), SampleNodes.Image("second")));

        var html = await RenderHarness.RenderPage(WithImage(RenderMode.Static), page);

        var first = html[html.IndexOf("<img", StringComparison.Ordinal)..];
        var second = first[(first.IndexOf("<img", 1, StringComparison.Ordinal))..];
        first = first[..first.IndexOf('>')];
        second = second[..second.IndexOf('>')];

        Assert.Contains("loading=\"eager\"", first);
        Assert.Contains("fetchpriority=\"high\"", first);
        Assert.Contains("loading=\"lazy\"", second);
        Assert.DoesNotContain("fetchpriority", second);
    }

    [Fact]
    public async Task Sizes_defaults_to_full_viewport_without_layout_context()
    {
        var html = await RenderHarness.RenderNode(WithImage(RenderMode.Static), SampleNodes.Image());

        Assert.Contains("sizes=\"100vw\"", html);
    }

    [Fact]
    public async Task Sizes_caps_at_the_section_track()
    {
        var page = SampleNodes.Section(SampleNodes.Image());

        var html = await RenderHarness.RenderPage(WithImage(RenderMode.Static), page);

        Assert.Contains("sizes=\"(min-width: 1152px) 1152px, 100vw\"", html);
    }

    [Fact]
    public async Task Sizes_multiplies_by_the_column_ratio_fraction()
    {
        var page = SampleNodes.Section(
            SampleNodes.Columns([2, 1], SampleNodes.Stack(SampleNodes.Image()), SampleNodes.Stack()));

        var html = await RenderHarness.RenderPage(WithImage(RenderMode.Static), page);

        Assert.Contains("sizes=\"(min-width: 1152px) 768px, 67vw\"", html);
    }

    [Fact]
    public async Task Sizes_inside_a_grid_uses_the_min_item_width()
    {
        var page = SampleNodes.Section(SampleNodes.Grid(280, SampleNodes.Image()));

        var html = await RenderHarness.RenderPage(WithImage(RenderMode.Static), page);

        Assert.Contains("sizes=\"(min-width: 280px) 280px, 100vw\"", html);
    }

    [Fact]
    public async Task Node_alt_wins_over_the_asset_default()
    {
        var html = await RenderHarness.RenderNode(WithImage(RenderMode.Static), SampleNodes.Image(alt: "Placement alt"));

        Assert.Contains("alt=\"Placement alt\"", html);
    }

    [Fact]
    public async Task Empty_node_alt_falls_back_to_the_asset_default_alt()
    {
        var html = await RenderHarness.RenderNode(WithImage(RenderMode.Static), SampleNodes.Image(alt: null));

        Assert.Contains("alt=\"Library alt\"", html);
    }

    [Fact]
    public async Task Aspect_and_rounded_variants_emit_classes()
    {
        var image = SampleNodes.Image() with { Aspect = ImageAspect.Wide16x9, Rounded = true };

        var html = await RenderHarness.RenderNode(WithImage(RenderMode.Static), image);

        Assert.Contains("ip-img-wide16x9", html);
        Assert.Contains("ip-img-rounded", html);
    }

    [Fact]
    public async Task Editor_mode_missing_asset_renders_labelled_placeholder()
    {
        var image = SampleNodes.Image() with { AssetId = null };

        var html = await RenderHarness.RenderNode(WithImage(RenderMode.Editor), image);

        Assert.Contains("ip-placeholder", html);
        Assert.Contains("Image", html);
        Assert.Contains($"data-node-id=\"{image.Id.Compact}\"", html);
    }

    [Fact]
    public async Task Static_mode_missing_asset_renders_nothing()
    {
        var image = SampleNodes.Image() with { AssetId = null };

        var html = await RenderHarness.RenderNode(WithImage(RenderMode.Static), image);

        Assert.DoesNotContain("<img", html);
        Assert.DoesNotContain("ip-placeholder", html);
    }

    [Fact]
    public async Task Unresolvable_asset_renders_placeholder_in_editor_only()
    {
        var image = SampleNodes.Image() with { AssetId = AssetId.New() };

        var editor = await RenderHarness.RenderNode(WithImage(RenderMode.Editor), image);
        var published = await RenderHarness.RenderNode(WithImage(RenderMode.Static), image);

        Assert.Contains("ip-placeholder", editor);
        Assert.DoesNotContain("<img", published);
    }
}
