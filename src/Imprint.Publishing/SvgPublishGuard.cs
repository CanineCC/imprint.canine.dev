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
                var name = element.Name.LocalName;
                if (name.Equals("script", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("foreignObject", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                foreach (var attribute in element.Attributes())
                {
                    // Any on* attribute is an event handler; there is no legitimate one in SVG.
                    if (attribute.Name.LocalName.StartsWith("on", StringComparison.OrdinalIgnoreCase))
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
}
