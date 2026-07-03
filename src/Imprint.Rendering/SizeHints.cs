using Imprint.Authoring.Domain.Pages;

namespace Imprint.Rendering;

/// <summary>
/// A cascading fraction model that turns layout context into an <c>img sizes</c>
/// attribute: sections cap the available width at their track, columns multiply by
/// their ratio fraction, grids pin items near their minimum width. The <c>sizes</c>
/// grammar only understands viewport media queries while the real layout is
/// container-query driven, so this is a deliberate approximation — the cost of being
/// off is a slightly larger (never broken) image variant.
/// </summary>
public sealed record SizeHints
{
    public static readonly SizeHints Root = new();

    // Must match the .ip-section tracks in imprint-base.css (72rem / 96rem at 16px root).
    private const int NormalTrackPx = 1152;
    private const int WideTrackPx = 1536;

    /// <summary>Widest the current slot can ever get, in px. Null = full-bleed (viewport-bound).</summary>
    public int? CapPx { get; init; }

    /// <summary>Share of the cap (or viewport) the current slot occupies.</summary>
    public double Fraction { get; init; } = 1.0;

    public SizeHints InSection(SectionWidth width) => width switch
    {
        SectionWidth.Normal => this with { CapPx = Narrowest(NormalTrackPx) },
        SectionWidth.Wide => this with { CapPx = Narrowest(WideTrackPx) },
        // Full sections keep the viewport as the upper bound.
        _ => this,
    };

    public SizeHints InColumn(int ratio, int ratioSum) =>
        this with { Fraction = Fraction * ratio / Math.Max(1, ratioSum) };

    /// <summary>
    /// Grid items sit between min-item and ~2× min-item wide regardless of what wraps
    /// the grid, so the min-item becomes the cap and the fraction resets; below the
    /// min-item the grid collapses to one full-width column.
    /// </summary>
    public SizeHints InGrid(int minItemPx) => this with { CapPx = minItemPx, Fraction = 1.0 };

    public string ToSizesAttribute()
    {
        var percent = Math.Clamp((int)Math.Round(Fraction * 100), 1, 100);
        if (CapPx is not int cap)
        {
            return $"{percent}vw";
        }

        var px = Math.Max(1, (int)Math.Round(cap * Fraction));
        return $"(min-width: {cap}px) {px}px, {percent}vw";
    }

    private int Narrowest(int trackPx) => Math.Min(CapPx ?? int.MaxValue, trackPx);
}
