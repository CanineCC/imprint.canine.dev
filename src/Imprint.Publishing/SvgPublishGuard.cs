using System.Xml;
using System.Xml.Linq;
using Imprint.Authoring.Domain.Assets;

namespace Imprint.Publishing;

/// <summary>
/// Defense-in-depth re-check of an SVG about to be inlined into published HTML. The
/// content was already sanitized at ingest (Imprint.Media); this cheap parse exists so
/// a bug there — or a hand-edited media directory — cannot ship active content to
/// every visitor. Reject, never fix: an unsafe SVG simply doesn't publish.
/// </summary>
internal static class SvgPublishGuard
{
    // Same allowlist the ingest sanitizer keeps (shared via SvgSafety so the two can't
    // drift). The sanitizer unwraps &lt;a&gt;, so a sanitized SVG contains none — and
    // &lt;a&gt; is not in the allowlist, so this guard rejects it too, which is the
    // correct behaviour for a re-check.
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
                if (!SvgSafety.IsAllowed(element.Name.LocalName) || DepthOf(element) > MaxDepth)
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
