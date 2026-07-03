using System.Xml;
using System.Xml.Linq;

namespace Imprint.Publishing;

/// <summary>
/// Defense-in-depth re-check of an SVG about to be inlined into published HTML. The
/// content was already sanitized at ingest (Imprint.Media); this cheap parse exists so
/// a bug there — or a hand-edited media directory — cannot ship active content to
/// every visitor. Reject, never fix: an unsafe SVG simply doesn't publish.
/// </summary>
internal static class SvgPublishGuard
{
    // Must mirror what the ingest sanitizer (Imprint.Media SvgSanitizer) strips: this
    // guard's whole point is that if that sanitizer regresses — or someone hand-edits
    // the media directory — active content still cannot reach a visitor. A shorter
    // list here would be a false sense of safety.
    private static readonly HashSet<string> ForbiddenElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "script", "foreignObject", "style", "a",
        "animate", "animateColor", "animateMotion", "animateTransform", "set", "discard", "mpath",
    };

    // Same depth ceiling as the ingest sanitizer, so a deeply nested SVG can't
    // StackOverflow the publisher via XElement enumeration/serialization either.
    private const int MaxDepth = 100;

    public static bool IsSafe(string svg)
    {
        try
        {
            // XDocument.Parse prohibits DTDs by default, closing the entity-expansion door.
            var document = XDocument.Parse(svg);
            if (document.Root is null ||
                !document.Root.Name.LocalName.Equals("svg", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            foreach (var element in document.Descendants())
            {
                if (ForbiddenElements.Contains(element.Name.LocalName) || DepthOf(element) > MaxDepth)
                {
                    return false;
                }

                foreach (var attribute in element.Attributes())
                {
                    var local = attribute.Name.LocalName;

                    // Any on* attribute is an event handler; there is no legitimate one in SVG.
                    if (local.StartsWith("on", StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }

                    // style can smuggle url()/expression(); href/xlink:href may carry a
                    // javascript:/data: scheme. Only a same-document fragment is safe.
                    if (local.Equals("style", StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }

                    if (IsHref(attribute) && !attribute.Value.TrimStart().StartsWith('#'))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
        catch (XmlException)
        {
            return false;
        }
    }

    private static bool IsHref(XAttribute attribute) =>
        attribute.Name.LocalName.Equals("href", StringComparison.OrdinalIgnoreCase);

    private static int DepthOf(XElement element)
    {
        var depth = 0;
        for (var parent = element.Parent; parent is not null; parent = parent.Parent)
        {
            depth++;
        }

        return depth;
    }
}
