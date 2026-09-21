namespace Imprint.Publishing;

/// <summary>
/// An architecture map inlined as SVG — the product draws it from a published survey, this site
/// prints it as part of the page.
/// </summary>
/// <remarks>
/// <para>★ SVG IS THE WHOLE POINT, and the reason this is not a PNG. The owner's first proposal for
/// the bespoke-looking embeds was to render them nightly into images. An image is exactly as
/// unreadable to a crawler and a language model as the iframe it would replace, so it does not solve
/// the problem this work exists for — and <b>WCAG 2.2 §1.4.5 Images of Text</b> is a criterion we
/// charge customers to measure, so publishing a diagram full of component names as a flat picture
/// fails, on our own site, a check we sell. Inline SVG keeps the bespoke drawing AND ships every
/// label as real <c>&lt;text&gt;</c>: selectable, translatable, zoomable, searchable.</para>
/// <para>The product's own renderer draws it, against a published run, so there is no second copy of
/// the layout to drift — the same reason the frame was defensible in the first place. What changes is
/// that the result lands IN the page instead of in a document beside it. The figure themes with the
/// page because the drawing already refers to <c>var(--heading)</c> and <c>var(--muted)</c>, which
/// resolve against whatever surrounds it.</para>
/// <para>★ It is re-checked with <see cref="SvgPublishGuard"/> before it is inlined, and REFUSED
/// rather than repaired. This markup crosses a trust boundary: an SVG is an active-content format,
/// and inlining one from another service into every visitor's page is the same risk as inlining a
/// script from it. The guard is the reason that is acceptable; without it this file would be a
/// stored-XSS sink with a nice comment on top.</para>
/// </remarks>
public static class ArchitectureSvgTemplate
{
    public const string Name = "architecture-svg";

    /// <summary>The figure, or null — an unsafe or unparseable drawing publishes the widget's
    /// fallback link, which is a way to go and see it rather than a broken picture.</summary>
    public static string? Render(string? svg)
    {
        if (string.IsNullOrWhiteSpace(svg))
        {
            return null;
        }

        var trimmed = svg.Trim();

        // A payload that is not a drawing at all — an error page, an HTML 404, a JSON envelope — must
        // not reach the guard's parser as if it might pass. Cheap shape check first.
        if (!trimmed.StartsWith("<svg", StringComparison.OrdinalIgnoreCase)
            && !trimmed.StartsWith("<?xml", StringComparison.Ordinal))
        {
            return null;
        }

        if (!SvgPublishGuard.IsSafe(trimmed))
        {
            return null;
        }

        // ip-svg is the site's existing figure wrapper: it scrolls a wide drawing inside its own box on
        // a phone rather than shrinking the labels below reading size.
        return $"<div class=\"ip-svg ip-architecture\">{trimmed}</div>";
    }
}
