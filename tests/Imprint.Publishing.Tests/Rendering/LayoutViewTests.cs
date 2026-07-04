using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Rendering;

namespace Imprint.Publishing.Tests.Rendering;

public sealed class LayoutViewTests
{
    private static readonly RenderContext Static = RenderHarness.Context(RenderMode.Static);

    [Fact]
    public async Task Columns_render_ratio_template_and_collapse_class()
    {
        var columns = SampleNodes.Columns([2, 1], SampleNodes.Stack(), SampleNodes.Stack()) with
        {
            CollapseBelow = CollapseBreakpoint.Px480,
        };

        var html = await RenderHarness.RenderNode(Static, columns);

        Assert.Contains("--ip-cols: 2fr 1fr", html);
        Assert.Contains("ip-collapse-480", html);
        Assert.Contains("class=\"ip-columns", html);
    }

    [Fact]
    public async Task Columns_default_collapse_breakpoint_is_640()
    {
        var html = await RenderHarness.RenderNode(Static, SampleNodes.Columns([1, 1, 1], SampleNodes.Stack(), SampleNodes.Stack(), SampleNodes.Stack()));

        Assert.Contains("ip-collapse-640", html);
        Assert.Contains("--ip-cols: 1fr 1fr 1fr", html);
    }

    [Fact]
    public async Task Grid_renders_min_item_custom_property()
    {
        var html = await RenderHarness.RenderNode(Static, SampleNodes.Grid(320));

        Assert.Contains("class=\"ip-grid\"", html);
        Assert.Contains("--ip-min-item: 320px", html);
    }

    [Fact]
    public async Task Section_default_variants_emit_no_extra_classes()
    {
        var html = await RenderHarness.RenderNode(Static, SampleNodes.Section());

        Assert.Contains("<section class=\"ip-section\"", html);
    }

    [Fact]
    public async Task Section_non_default_variants_emit_their_classes()
    {
        var section = SampleNodes.Section() with
        {
            Width = SectionWidth.Wide,
            Background = SectionBackground.Primary,
            Padding = SectionPadding.Large,
        };

        var html = await RenderHarness.RenderNode(Static, section);

        Assert.Contains("ip-section-wide", html);
        Assert.Contains("ip-bg-primary", html);
        Assert.Contains("ip-pad-large", html);
    }

    [Fact]
    public async Task Section_plain_appearance_emits_no_appearance_class()
    {
        var html = await RenderHarness.RenderNode(Static, SampleNodes.Section());

        Assert.Contains("<section class=\"ip-section\"", html);
        Assert.DoesNotContain("ip-ap-", html);
    }

    [Fact]
    public async Task Section_named_appearance_emits_its_ip_ap_class_alongside_structural_ones()
    {
        var section = SampleNodes.Section() with
        {
            Background = SectionBackground.SurfaceAlt,
            Appearance = SectionAppearance.FeatureGrid,
        };

        var html = await RenderHarness.RenderNode(Static, section);

        // The appearance class rides alongside the structural background class.
        Assert.Contains("ip-section", html);
        Assert.Contains("ip-bg-surface-alt", html);
        Assert.Contains("ip-ap-feature-grid", html);
    }

    [Fact]
    public async Task Stack_gap_and_align_variants_emit_their_classes()
    {
        var stack = SampleNodes.Stack() with { Gap = Gap.Tight, Align = StackAlign.Center };

        var html = await RenderHarness.RenderNode(Static, stack);

        Assert.Contains("ip-gap-tight", html);
        Assert.Contains("ip-align-center", html);
    }

    [Fact]
    public async Task Divider_renders_hr_with_class()
    {
        var html = await RenderHarness.RenderNode(Static, new DividerNode { Id = NodeId.New() });

        Assert.Contains("<hr class=\"ip-divider\"", html);
    }

    [Fact]
    public async Task Spacer_renders_size_class_and_is_aria_hidden()
    {
        var html = await RenderHarness.RenderNode(Static, new SpacerNode { Id = NodeId.New(), Size = SpacerSize.Large });

        Assert.Contains("ip-spacer-l", html);
        Assert.Contains("aria-hidden=\"true\"", html);
    }

    [Fact]
    public async Task Heading_renders_level_tag_and_encodes_text()
    {
        var heading = SampleNodes.Heading("Fish & Chips", level: 3);

        var html = await RenderHarness.RenderNode(Static, heading);

        Assert.Contains("<h3>", html);
        Assert.Contains("Fish &amp; Chips", html);
        Assert.Contains("</h3>", html);
    }

    [Fact]
    public async Task Heading_falls_back_to_default_locale_text()
    {
        var heading = SampleNodes.Heading("English only");
        var daCtx = Static with { Locale = RenderHarness.Da };

        var html = await RenderHarness.RenderNode(daCtx, heading);

        Assert.Contains("English only", html);
    }

    [Fact]
    public async Task Page_view_wraps_roots_in_ip_page()
    {
        var html = await RenderHarness.RenderPage(Static, SampleNodes.Section(SampleNodes.Heading()));

        Assert.Contains("<div class=\"ip-page\">", html);
        Assert.Contains("<section class=\"ip-section\"", html);
    }
}
