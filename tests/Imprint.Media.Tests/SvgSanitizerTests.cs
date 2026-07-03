using System.Xml;

namespace Imprint.Media.Tests;

public sealed class SvgSanitizerTests
{
    private const string Ns = "xmlns=\"http://www.w3.org/2000/svg\"";

    // -- active content removal ----------------------------------------------------

    [Fact]
    public void Sanitize_script_element_is_removed_and_counted()
    {
        var (svg, removed) = SvgSanitizer.Sanitize(
            $"""<svg {Ns}><script>alert('xss')</script><rect width="4" height="4"/></svg>""");

        Assert.DoesNotContain("script", svg);
        Assert.DoesNotContain("alert", svg);
        Assert.Contains("<rect", svg);
        Assert.Equal(1, removed);
    }

    [Fact]
    public void Sanitize_nested_script_deep_in_the_tree_is_removed()
    {
        var (svg, removed) = SvgSanitizer.Sanitize(
            $"""<svg {Ns}><g><g><script href="#x">alert(1)</script></g></g></svg>""");

        Assert.DoesNotContain("script", svg);
        Assert.Equal(1, removed);
    }

    [Fact]
    public void Sanitize_foreignObject_is_removed()
    {
        var (svg, removed) = SvgSanitizer.Sanitize(
            $"""<svg {Ns}><foreignObject><div xmlns="http://www.w3.org/1999/xhtml">html</div></foreignObject></svg>""");

        Assert.DoesNotContain("foreignObject", svg);
        Assert.Equal(1, removed);
    }

    [Fact]
    public void Sanitize_style_element_and_style_attribute_are_removed()
    {
        var (svg, removed) = SvgSanitizer.Sanitize(
            $$"""<svg {{Ns}}><style>rect { fill: url(http://evil) }</style><rect style="fill:url(javascript:x)" width="4"/></svg>""");

        Assert.DoesNotContain("style", svg);
        Assert.Contains("""<rect width="4" />""", svg);
        Assert.Equal(2, removed);
    }

    [Theory]
    [InlineData("onclick")]
    [InlineData("onload")]
    [InlineData("onmouseover")]
    [InlineData("ONLOAD")]
    [InlineData("onbegin")]
    public void Sanitize_event_handler_attributes_are_removed(string attribute)
    {
        var (svg, removed) = SvgSanitizer.Sanitize(
            $"""<svg {Ns}><circle r="4" {attribute}="steal()"/></svg>""");

        Assert.DoesNotContain("steal", svg);
        Assert.Contains("<circle", svg);
        Assert.Equal(1, removed);
    }

    [Fact]
    public void Sanitize_onload_on_the_svg_root_is_removed()
    {
        var (svg, removed) = SvgSanitizer.Sanitize(
            $"""<svg {Ns} onload="pwn()"><rect width="4"/></svg>""");

        Assert.DoesNotContain("pwn", svg);
        Assert.Equal(1, removed);
    }

    // -- reference policy ------------------------------------------------------------

    [Theory]
    [InlineData("""http://evil.example/x.png""")]
    [InlineData("""https://evil.example/x.png""")]
    [InlineData("""javascript:alert(1)""")]
    [InlineData("""data:image/svg+xml,<svg onload=alert(1)/>""")]
    public void Sanitize_image_with_external_href_is_dropped_entirely(string href)
    {
        var (svg, removed) = SvgSanitizer.Sanitize(
            $"""<svg {Ns} xmlns:xlink="http://www.w3.org/1999/xlink"><image xlink:href="{href.Replace("<", "&lt;")}"/><rect width="4"/></svg>""");

        Assert.DoesNotContain("image", svg);
        Assert.Contains("<rect", svg);
        Assert.Equal(1, removed);
    }

    [Fact]
    public void Sanitize_use_with_external_href_is_dropped_entirely()
    {
        var (svg, removed) = SvgSanitizer.Sanitize(
            $"""<svg {Ns}><use href="https://evil.example/defs.svg#icon"/></svg>""");

        Assert.DoesNotContain("use", svg);
        Assert.Equal(1, removed);
    }

    [Fact]
    public void Sanitize_use_with_fragment_href_is_kept()
    {
        var (svg, removed) = SvgSanitizer.Sanitize(
            $"""<svg {Ns}><defs><circle id="dot" r="2"/></defs><use href="#dot"/></svg>""");

        Assert.Contains("""<use href="#dot" />""", svg);
        Assert.Equal(0, removed);
    }

    [Fact]
    public void Sanitize_javascript_href_on_other_elements_removes_only_the_attribute()
    {
        var (svg, removed) = SvgSanitizer.Sanitize(
            $"""<svg {Ns}><textPath href="javascript:alert(1)">bend</textPath></svg>""");

        Assert.DoesNotContain("javascript", svg);
        Assert.Contains("bend", svg);
        Assert.Equal(1, removed);
    }

    [Fact]
    public void Sanitize_gradient_fragment_reference_is_preserved()
    {
        var (svg, removed) = SvgSanitizer.Sanitize(
            $"""<svg {Ns} xmlns:xlink="http://www.w3.org/1999/xlink"><linearGradient id="b" xlink:href="#a"/><linearGradient id="a"><stop offset="0" stop-color="red"/></linearGradient></svg>""");

        Assert.Contains("xlink:href=\"#a\"", svg);
        Assert.Equal(0, removed);
    }

    // -- <a> unwrapping ----------------------------------------------------------------

    [Fact]
    public void Sanitize_anchor_is_unwrapped_keeping_children()
    {
        var (svg, removed) = SvgSanitizer.Sanitize(
            $"""<svg {Ns}><a href="https://example.com"><circle r="4"/><text>hi</text></a></svg>""");

        Assert.DoesNotContain("<a", svg);
        Assert.DoesNotContain("example.com", svg);
        Assert.Contains("<circle", svg);
        Assert.Contains("hi", svg);
        Assert.Equal(1, removed);
    }

    [Fact]
    public void Sanitize_nested_anchors_are_both_unwrapped()
    {
        var (svg, removed) = SvgSanitizer.Sanitize(
            $"""<svg {Ns}><a href="javascript:x"><a href="https://e"><circle r="4" onclick="p()"/></a></a></svg>""");

        Assert.DoesNotContain("<a", svg);
        Assert.DoesNotContain("onclick", svg);
        Assert.Contains("<circle", svg);
        Assert.Equal(3, removed);
    }

    [Fact]
    public void Sanitize_empty_anchor_is_removed()
    {
        var (svg, removed) = SvgSanitizer.Sanitize($"""<svg {Ns}><a href="https://e"></a></svg>""");

        Assert.DoesNotContain("<a", svg);
        Assert.Equal(1, removed);
    }

    // -- parser hardening -----------------------------------------------------------

    [Fact]
    public void Sanitize_doctype_with_entity_is_rejected_by_the_reader()
    {
        const string billionLaughs =
            """
            <!DOCTYPE svg [<!ENTITY x "boom">]>
            <svg xmlns="http://www.w3.org/2000/svg"><text>&x;</text></svg>
            """;

        Assert.Throws<XmlException>(() => SvgSanitizer.Sanitize(billionLaughs));
    }

    [Fact]
    public void Sanitize_external_entity_doctype_is_rejected_by_the_reader()
    {
        const string xxe =
            """
            <!DOCTYPE svg [<!ENTITY xxe SYSTEM "file:///etc/passwd">]>
            <svg xmlns="http://www.w3.org/2000/svg"><text>&xxe;</text></svg>
            """;

        Assert.Throws<XmlException>(() => SvgSanitizer.Sanitize(xxe));
    }

    [Fact]
    public void Sanitize_non_svg_root_is_rejected()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => SvgSanitizer.Sanitize("""<html xmlns="http://www.w3.org/1999/xhtml"><body/></html>"""));

        Assert.Contains("svg", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sanitize_svg_root_without_the_svg_namespace_is_rejected()
    {
        Assert.Throws<InvalidOperationException>(() => SvgSanitizer.Sanitize("<svg><rect/></svg>"));
    }

    [Fact]
    public void Sanitize_malformed_xml_is_rejected()
    {
        Assert.Throws<XmlException>(() => SvgSanitizer.Sanitize("<svg xmlns='http://www.w3.org/2000/svg'><rect>"));
    }

    // -- preservation -----------------------------------------------------------------

    [Fact]
    public void Sanitize_benign_svg_round_trips_with_structure_intact()
    {
        const string benign =
            """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor"><defs><linearGradient id="g"><stop offset="0%" stop-color="#ff0000" /><stop offset="100%" stop-color="#0000ff" /></linearGradient></defs><path d="M12 2 L22 22 L2 22 Z" fill="url(#g)" stroke-width="1.5" /><circle cx="12" cy="12" r="3" /></svg>""";

        var (svg, removed) = SvgSanitizer.Sanitize(benign);

        Assert.Equal(0, removed);
        Assert.Contains("viewBox=\"0 0 24 24\"", svg);
        Assert.Contains("fill=\"currentColor\"", svg);
        Assert.Contains("""<path d="M12 2 L22 22 L2 22 Z" fill="url(#g)" stroke-width="1.5" />""", svg);
        Assert.Contains("<linearGradient", svg);
        Assert.Contains("""<circle cx="12" cy="12" r="3" />""", svg);
    }

    [Fact]
    public void Sanitize_output_has_no_xml_declaration()
    {
        var (svg, _) = SvgSanitizer.Sanitize(
            $"""<?xml version="1.0" encoding="UTF-8"?><svg {Ns}><rect width="4"/></svg>""");

        Assert.StartsWith("<svg", svg, StringComparison.Ordinal);
    }

    [Fact]
    public void Sanitize_counts_every_removal_across_categories()
    {
        // script + style element + style attribute + onclick + external image = 5.
        var (_, removed) = SvgSanitizer.Sanitize(
            $"""<svg {Ns}><script>x</script><style>y</style><rect style="fill:red" onclick="z()"/><image href="https://evil/x"/></svg>""");

        Assert.Equal(5, removed);
    }

    // -- SMIL animation (audit finding: <set> can retarget href at runtime) ---------

    [Theory]
    [InlineData("animate")]
    [InlineData("animateColor")]
    [InlineData("animateMotion")]
    [InlineData("animateTransform")]
    [InlineData("set")]
    [InlineData("discard")]
    public void Sanitize_smil_animation_elements_are_removed(string element)
    {
        var (svg, removed) = SvgSanitizer.Sanitize(
            $"""<svg {Ns}><circle r="4"><{element} attributeName="href" to="javascript:alert(1)"/></circle></svg>""");

        Assert.DoesNotContain(element, svg, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("javascript", svg, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, removed);
    }

    // -- depth bound (audit finding: unbounded recursion → StackOverflow crash loop) -

    [Fact]
    public void Sanitize_pathologically_deep_svg_is_rejected_not_crashed()
    {
        // Far beyond any real graphic; must fail fast with a clear message rather than
        // recurse the stack to death (a StackOverflowException is uncatchable and would
        // take the whole worker process down in a restart loop).
        var deep = new System.Text.StringBuilder($"<svg {Ns}>");
        const int levels = 5_000;
        for (var i = 0; i < levels; i++)
        {
            deep.Append("<g>");
        }

        for (var i = 0; i < levels; i++)
        {
            deep.Append("</g>");
        }

        deep.Append("</svg>");

        var error = Assert.Throws<InvalidOperationException>(() => SvgSanitizer.Sanitize(deep.ToString()));
        Assert.Contains("deep", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sanitize_a_normally_nested_icon_still_passes()
    {
        // A dozen levels is plenty for a real icon — the cap must not touch it.
        var (svg, _) = SvgSanitizer.Sanitize(
            $"""<svg {Ns}><g><g><g><g><g><path d="M0 0h4v4H0z"/></g></g></g></g></g></svg>""");

        Assert.Contains("<path", svg, StringComparison.Ordinal);
    }
}
