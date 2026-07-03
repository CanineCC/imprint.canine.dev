using Imprint.Authoring.Domain.Sites;

namespace Imprint.Publishing.Tests.Rendering;

/// <summary>
/// WCAG 2.x AA contrast (≥ 4.5:1 for normal text) for the default theme, in both
/// modes. Relative luminance is computed here from first principles so the assertion
/// cannot drift with any production helper — nobody gets to "improve" the default
/// palette into an inaccessible one without this test objecting.
/// </summary>
public sealed class ThemeContrastTests
{
    [Theory]
    [InlineData("text", "background")]
    [InlineData("text-muted", "background")]
    [InlineData("on-primary", "primary")]
    public void Default_theme_pairs_meet_aa_in_light_mode(string foreground, string background)
    {
        var ratio = Contrast(Light(foreground), Light(background));
        Assert.True(ratio >= 4.5, $"{foreground} on {background} (light) is {ratio:F2}:1 — below 4.5:1.");
    }

    [Theory]
    [InlineData("text", "background")]
    [InlineData("text-muted", "background")]
    [InlineData("on-primary", "primary")]
    public void Default_theme_pairs_meet_aa_in_dark_mode(string foreground, string background)
    {
        var ratio = Contrast(Dark(foreground), Dark(background));
        Assert.True(ratio >= 4.5, $"{foreground} on {background} (dark) is {ratio:F2}:1 — below 4.5:1.");
    }

    private static string Light(string token) => Token(token).Light;

    private static string Dark(string token) => Token(token).Dark;

    private static ThemeToken Token(string name) =>
        Theme.Default.Tokens.Get(name) ?? throw new InvalidOperationException($"Default theme lacks token '{name}'.");

    private static double Contrast(string hexA, string hexB)
    {
        var (lighter, darker) = Luminance(hexA) >= Luminance(hexB)
            ? (Luminance(hexA), Luminance(hexB))
            : (Luminance(hexB), Luminance(hexA));
        return (lighter + 0.05) / (darker + 0.05);
    }

    private static double Luminance(string hex)
    {
        Assert.Matches("^#[0-9a-fA-F]{6}$", hex);
        var r = Linear(Convert.ToInt32(hex.Substring(1, 2), 16));
        var g = Linear(Convert.ToInt32(hex.Substring(3, 2), 16));
        var b = Linear(Convert.ToInt32(hex.Substring(5, 2), 16));
        return 0.2126 * r + 0.7152 * g + 0.0722 * b;
    }

    private static double Linear(int channel)
    {
        var c = channel / 255.0;
        return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
    }
}
