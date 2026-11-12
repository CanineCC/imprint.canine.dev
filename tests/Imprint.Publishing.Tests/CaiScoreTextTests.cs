using Imprint.Publishing;

namespace Imprint.Publishing.Tests;

/// <summary>
/// ★ A CAI score on the websites is written as the product writes it (owner, 12 November 2026; ADR-0027 in
/// kennel.canine.dev): one decimal, always, rounded half to even as the CAI standard states (§7). The same repository
/// must never read 98 on a site card and 97.6 in the app.
/// </summary>
public sealed class CaiScoreTextTests
{
    [Theory]
    [InlineData(97.6, "97.6")]
    [InlineData(98.0, "98.0")]
    [InlineData(70.0, "70.0")]
    [InlineData(100.0, "100.0")]
    [InlineData(86.81073415417612, "86.8")]
    [InlineData(61.5, "61.5")]
    public void A_score_is_written_with_one_decimal_always(double score, string shown) =>
        Assert.Equal(shown, CaiScoreText.Shown(score));

    [Theory]
    [InlineData(72.25, "72.2")]   // an exact tie in binary: to the even tenth
    [InlineData(72.75, "72.8")]
    public void A_tie_rounds_to_the_even_tenth_as_the_standard_states(double score, string shown) =>
        Assert.Equal(shown, CaiScoreText.Shown(score));

    [Theory]
    [InlineData(-0.04)]
    [InlineData(0.04)]
    public void Negative_zero_is_never_written(double score) =>
        Assert.Equal("0.0", CaiScoreText.Shown(score));

    [Fact]
    public void A_change_is_the_difference_of_the_two_shown_figures()
    {
        Assert.Equal("35.6", CaiScoreText.Change(62, 97.6));
        Assert.Equal("14.7", CaiScoreText.Change(57.46, 72.24));   // 57.5 → 72.2, where the raw difference is 14.78
        Assert.Equal("0.0", CaiScoreText.Change(72.21, 72.24));
    }

    [Fact]
    public void A_line_on_the_scale_keeps_its_own_form() =>
        Assert.Equal("70", TemplateJson.Score(70));
}
