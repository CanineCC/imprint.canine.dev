using System.Xml;
using System.Xml.Linq;

namespace Imprint.Media;

/// <summary>
/// Strips active content from an SVG so it is safe to inline into published pages
/// (inlining is what lets icons inherit <c>currentColor</c>). Everything not removed
/// is preserved verbatim: shapes, defs, gradients, viewBox, presentation attributes.
/// </summary>
public static class SvgSanitizer
{
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";
    private static readonly XNamespace Xlink = "http://www.w3.org/1999/xlink";

    public static (string Svg, int RemovedNodes) Sanitize(string svg)
    {
        // XXE is non-negotiable: no DTDs, no resolver — a doctype (even a benign one)
        // fails the parse before any entity could be expanded.
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
        };

        XDocument document;
        using (var reader = XmlReader.Create(new StringReader(svg), settings))
        {
            document = XDocument.Load(reader, LoadOptions.PreserveWhitespace);
        }

        if (document.Root is not { } root || root.Name != Svg + "svg")
        {
            throw new InvalidOperationException(
                "The file is not an SVG image: the root element must be <svg> in the SVG namespace.");
        }

        var removed = 0;
        SanitizeElement(root, ref removed);

        // ToString() on the element (not the document) emits without an XML
        // declaration, which is what inline embedding needs.
        return (root.ToString(SaveOptions.DisableFormatting), removed);
    }

    private static void SanitizeNode(XElement element, ref int removed)
    {
        switch (element.Name.LocalName)
        {
            // Local-name matching is deliberate: a <script> smuggled in under the
            // XHTML namespace executes just the same once inlined.
            case "script" or "foreignObject" or "style":
                element.Remove();
                removed++;
                return;

            // Links make no sense in an inlined decorative graphic and are a
            // phishing surface; keep the visuals, drop the wrapper.
            case "a":
                var kept = element.Nodes().ToList();
                element.RemoveNodes();
                if (kept.Count == 0)
                {
                    element.Remove();
                }
                else
                {
                    element.ReplaceWith([.. kept]);
                }

                removed++;
                foreach (var promoted in kept.OfType<XElement>())
                {
                    SanitizeNode(promoted, ref removed);
                }

                return;

            default:
                SanitizeElement(element, ref removed);
                return;
        }
    }

    private static void SanitizeElement(XElement element, ref int removed)
    {
        foreach (var attribute in element.Attributes().ToList())
        {
            if (attribute.IsNamespaceDeclaration)
            {
                continue;
            }

            var local = attribute.Name.LocalName;

            // No SVG attribute legitimately starts with "on" — they are all event
            // handlers (onclick, onload, onbegin, …).
            if (local.StartsWith("on", StringComparison.OrdinalIgnoreCase))
            {
                attribute.Remove();
                removed++;
                continue;
            }

            // style can carry url(...) loads and (in legacy engines) expressions;
            // presentation attributes cover every styling need we preserve.
            if (local == "style" && attribute.Name.Namespace == XNamespace.None)
            {
                attribute.Remove();
                removed++;
                continue;
            }

            if (IsHref(attribute) && !attribute.Value.StartsWith('#'))
            {
                // javascript:, data:, http(s): — none may survive. An <image> or
                // <use> existed only to follow that reference, so it goes wholesale.
                if (element.Name.LocalName is "image" or "use")
                {
                    element.Remove();
                    removed++;
                    return;
                }

                attribute.Remove();
                removed++;
            }
        }

        foreach (var child in element.Elements().ToList())
        {
            SanitizeNode(child, ref removed);
        }
    }

    private static bool IsHref(XAttribute attribute) =>
        attribute.Name.LocalName == "href" &&
        (attribute.Name.Namespace == XNamespace.None || attribute.Name.Namespace == Xlink);
}
