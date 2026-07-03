namespace Imprint.Publishing.Tests;

/// <summary>
/// The publish-time re-check is defense in depth: it must reject the SAME active
/// content the ingest sanitizer strips, so a sanitizer regression or a hand-edited
/// media directory cannot ship script to visitors. These tests pin that equivalence.
/// </summary>
public sealed class SvgPublishGuardTests
{
    private const string Ns = "xmlns=\"http://www.w3.org/2000/svg\"";
    private const string XlinkNs = "xmlns:xlink=\"http://www.w3.org/1999/xlink\"";

    [Fact]
    public void A_clean_svg_is_safe()
    {
        Assert.True(SvgPublishGuard.IsSafe($"""<svg {Ns}><path d="M0 0h4v4H0z" fill="currentColor"/></svg>"""));
    }

    [Fact]
    public void A_same_document_fragment_href_is_safe()
    {
        Assert.True(SvgPublishGuard.IsSafe(
            $"""<svg {XlinkNs} {Ns}><use xlink:href="#icon"/><g id="icon"><rect width="4"/></g></svg>"""));
    }

    [Theory]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("<style>* { color: red }</style>")]
    [InlineData("<foreignObject><div/></foreignObject>")]
    [InlineData("<a href=\"javascript:alert(1)\">x</a>")]
    [InlineData("<set attributeName=\"href\" to=\"javascript:alert(1)\"/>")]
    [InlineData("<animate attributeName=\"x\" to=\"5\"/>")]
    [InlineData("<rect onclick=\"steal()\"/>")]
    [InlineData("<rect style=\"background:url(http://evil)\"/>")]
    [InlineData("<image href=\"https://evil/x.png\"/>")]
    public void Active_or_fetching_content_is_rejected(string inner)
    {
        Assert.False(SvgPublishGuard.IsSafe($"<svg {Ns}>{inner}</svg>"));
    }

    [Fact]
    public void Xlink_href_with_a_remote_scheme_is_rejected()
    {
        Assert.False(SvgPublishGuard.IsSafe(
            $"""<svg {XlinkNs} {Ns}><a xlink:href="javascript:alert(1)"><rect width="4"/></a></svg>"""));
    }

    [Fact]
    public void Non_svg_root_is_rejected()
    {
        Assert.False(SvgPublishGuard.IsSafe("<html><body>x</body></html>"));
    }

    [Fact]
    public void Malformed_xml_is_rejected()
    {
        Assert.False(SvgPublishGuard.IsSafe($"<svg {Ns}><rect"));
    }

    [Fact]
    public void Pathologically_deep_svg_is_rejected()
    {
        var deep = new System.Text.StringBuilder($"<svg {Ns}>");
        for (var i = 0; i < 5_000; i++)
        {
            deep.Append("<g>");
        }

        for (var i = 0; i < 5_000; i++)
        {
            deep.Append("</g>");
        }

        deep.Append("</svg>");
        Assert.False(SvgPublishGuard.IsSafe(deep.ToString()));
    }
}
