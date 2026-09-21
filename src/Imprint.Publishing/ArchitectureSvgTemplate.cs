using System.Xml;
using System.Xml.Linq;

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
/// the LAYOUT to drift — the same reason the frame was defensible in the first place. What changes is
/// that the result lands IN the page instead of in a document beside it, and that this site supplies
/// the paint: the <c>.wd-c4</c> rules live in imprint-marketing.css in the site's own tokens, so the
/// figure themes with the page in both light and dark instead of carrying a second palette.</para>
/// <para>★ Two elements are REMOVED and everything else is then re-checked with
/// <see cref="SvgPublishGuard"/> unchanged — see the comment on the removal, which is the one piece of
/// reasoning in this file worth reading before changing it. This markup crosses a trust boundary: an
/// SVG is an active-content format, and inlining one from another service into every visitor's page
/// is the same risk as inlining a script from it. The guard is the reason that is acceptable; without
/// it this file would be a stored-XSS sink with a nice comment on top.</para>
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

        // ★★ STRIP <style> AND <title> BEFORE THE GUARD, DO NOT RELAX THE GUARD.
        //   The drawing arrives with a <style> block, and the guard refuses it — correctly. A <style>
        //   inside an inlined SVG is NOT scoped to the drawing: it is ordinary CSS in the document,
        //   so a figure fetched from another service could restyle, hide or impersonate anything on
        //   the page. "Every selector in THIS one happens to start with .wd-c4" is a fact about
        //   today's payload, not a property a guard can rely on, and teaching the guard to verify it
        //   means parsing CSS inside a security check.
        //   Removing the element is the safe move, and it costs nothing here because this site owns
        //   presentation anyway: the .wd-c4 rules live in imprint-marketing.css, in the site's own
        //   tokens, so the figure themes with the page instead of carrying a second palette.
        //   <title> goes for the same reason — it is outside the allowlist, and `aria-label` on the
        //   root already carries the accessible name.
        //   ★ The guard then runs UNCHANGED over the result. Nothing unsafe survives; it is simply
        //   asked about a document that no longer contains the part it would have refused.
        if (Strip(trimmed, "style", "title") is not { } stripped || !SvgPublishGuard.IsSafe(stripped))
        {
            return null;
        }

        trimmed = stripped;

        // ip-svg is the site's existing figure wrapper: it scrolls a wide drawing inside its own box on
        // a phone rather than shrinking the labels below reading size.
        return $"<div class=\"ip-svg ip-architecture\">{trimmed}</div>";
    }

    /// <summary>
    /// The drawing with every element of the named kinds removed, or null when it will not parse.
    /// Matching is on the LOCAL name so a namespace prefix cannot smuggle one past.
    /// </summary>
    private static string? Strip(string svg, params string[] localNames)
    {
        try
        {
            var document = XDocument.Parse(svg);
            document.Descendants()
                .Where(e => localNames.Contains(e.Name.LocalName, StringComparer.OrdinalIgnoreCase))
                .ToList()
                .ForEach(e => e.Remove());

            return document.ToString(SaveOptions.DisableFormatting);
        }
        catch (XmlException)
        {
            return null;
        }
    }
}
