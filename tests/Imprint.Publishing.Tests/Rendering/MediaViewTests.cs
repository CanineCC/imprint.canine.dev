using Imprint.Authoring.Domain.Pages;
using Imprint.Rendering;

namespace Imprint.Publishing.Tests.Rendering;

public sealed class MediaViewTests
{
    private static RenderContext WithMedia(RenderMode mode, string? svgDefaultAlt = null) => RenderHarness.Context(mode) with
    {
        ResolveAsset = id =>
            id == SampleNodes.VideoAssetId ? RenderHarness.VideoAsset()
            : id == SampleNodes.SvgAssetId ? RenderHarness.SvgAsset(svgDefaultAlt)
            : null,
    };

    [Fact]
    public async Task Ambient_video_autoplays_muted_looping_inline_without_controls()
    {
        var html = await RenderHarness.RenderNode(WithMedia(RenderMode.Static), SampleNodes.Video(VideoMode.Ambient));

        Assert.Contains("<video class=\"ip-video\"", html);
        Assert.Contains("autoplay", html);
        Assert.Contains("muted", html);
        Assert.Contains("loop", html);
        Assert.Contains("playsinline", html);
        Assert.Contains("disableremoteplayback", html);
        Assert.DoesNotContain("controls", html);
    }

    [Fact]
    public async Task Player_video_has_controls_and_metadata_preload_without_autoplay()
    {
        var html = await RenderHarness.RenderNode(WithMedia(RenderMode.Static), SampleNodes.Video(VideoMode.Player));

        Assert.Contains("controls", html);
        Assert.Contains("preload=\"metadata\"", html);
        Assert.DoesNotContain("autoplay", html);
        Assert.DoesNotContain("loop", html);
    }

    [Fact]
    public async Task Video_renders_single_webm_source()
    {
        var html = await RenderHarness.RenderNode(WithMedia(RenderMode.Static), SampleNodes.Video());

        Assert.Contains("<source src=\"/assets/clip.webm\" type=\"video/webm\"", html);
    }

    [Fact]
    public async Task Video_without_asset_renders_placeholder_in_editor_and_nothing_in_static()
    {
        var video = SampleNodes.Video() with { AssetId = null };

        var editor = await RenderHarness.RenderNode(WithMedia(RenderMode.Editor), video);
        var published = await RenderHarness.RenderNode(WithMedia(RenderMode.Static), video);

        Assert.Contains("ip-placeholder", editor);
        Assert.Contains("Video", editor);
        Assert.DoesNotContain("<video", published);
        Assert.DoesNotContain("ip-placeholder", published);
    }

    [Fact]
    public async Task Svg_is_inlined_inside_the_wrapper()
    {
        var html = await RenderHarness.RenderNode(WithMedia(RenderMode.Static), SampleNodes.Svg());

        Assert.Contains("class=\"ip-svg\"", html);
        Assert.Contains("<svg viewBox=\"0 0 10 10\"><path d=\"M0 0h10v10z\"/></svg>", html);
    }

    [Fact]
    public async Task Svg_with_alt_gets_img_role_and_label()
    {
        var html = await RenderHarness.RenderNode(WithMedia(RenderMode.Static), SampleNodes.Svg(alt: "Company logo"));

        Assert.Contains("role=\"img\"", html);
        Assert.Contains("aria-label=\"Company logo\"", html);
        Assert.DoesNotContain("aria-hidden", html);
    }

    [Fact]
    public async Task Svg_without_any_alt_is_aria_hidden()
    {
        var html = await RenderHarness.RenderNode(WithMedia(RenderMode.Static), SampleNodes.Svg(alt: null));

        Assert.Contains("aria-hidden=\"true\"", html);
        Assert.DoesNotContain("role=\"img\"", html);
    }

    [Fact]
    public async Task Svg_empty_node_alt_falls_back_to_asset_default_alt()
    {
        var html = await RenderHarness.RenderNode(
            WithMedia(RenderMode.Static, svgDefaultAlt: "Default graphic alt"), SampleNodes.Svg(alt: null));

        Assert.Contains("aria-label=\"Default graphic alt\"", html);
    }

    [Fact]
    public async Task Svg_max_width_emits_inline_style()
    {
        var html = await RenderHarness.RenderNode(WithMedia(RenderMode.Static), SampleNodes.Svg(maxWidthPx: 240));

        Assert.Contains("style=\"max-width: 240px\"", html);
    }

    [Fact]
    public async Task Svg_without_asset_renders_placeholder_in_editor_and_nothing_in_static()
    {
        var svg = SampleNodes.Svg() with { AssetId = null };

        var editor = await RenderHarness.RenderNode(WithMedia(RenderMode.Editor), svg);
        var published = await RenderHarness.RenderNode(WithMedia(RenderMode.Static), svg);

        Assert.Contains("ip-placeholder", editor);
        Assert.Contains("Graphic", editor);
        Assert.DoesNotContain("ip-svg", published);
        Assert.DoesNotContain("ip-placeholder", published);
    }
}
