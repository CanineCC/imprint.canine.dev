using Imprint.Authoring.Domain.Sites;

namespace Imprint.Authoring.Tests.Domain.Sites;

/// <summary>
/// Theme token values are emitted verbatim into every visitor's <c>site.css</c>, so
/// "validated CSS color" has to actually mean it — no <c>url()</c> smuggling (which
/// would break the zero-third-party-request promise) and no non-colors.
/// </summary>
public sealed class CssColorTests
{
    [Theory]
    [InlineData("#fff")]
    [InlineData("#ffff")]
    [InlineData("#ffffff")]
    [InlineData("#ff00ff80")]
    [InlineData("rgb(0, 0, 0)")]
    [InlineData("rgb(0 0 0 / 50%)")]
    [InlineData("rgba(10, 20, 30, 0.5)")]
    [InlineData("hsl(210 50% 40%)")]
    [InlineData("oklch(70% 0.15 265)")]
    [InlineData("color-mix(in srgb, #000 40%, rgb(255 255 255))")]
    [InlineData("red")]
    [InlineData("rebeccapurple")]
    [InlineData("transparent")]
    [InlineData("currentColor")]
    public void Accepts_real_colors(string value) => Assert.True(CssColor.IsValid(value));

    [Theory]
    [InlineData("rgb(0,0,0) url(http://tracker/x)")]  // the smuggled tracker the audit flagged
    [InlineData("url(http://evil)")]
    [InlineData("rgb(0,0,0) url(/relative.gif)")]      // relative url, no scheme
    [InlineData("reddish")]                            // looks like a keyword, isn't a color
    [InlineData("bananas")]
    [InlineData("red; background: url(x)")]            // declaration break attempt
    [InlineData("</style><script>alert(1)</script>")]  // markup break attempt
    [InlineData("expression(alert(1))")]
    [InlineData("")]
    public void Rejects_non_colors_and_smuggling(string value) => Assert.False(CssColor.IsValid(value));

    [Fact]
    public void Rejects_absurdly_long_values()
    {
        Assert.False(CssColor.IsValid("#" + new string('f', 200)));
    }
}
