using System.Xml;
using System.Xml.Linq;
using Imprint.Authoring.Domain.Assets;

namespace Imprint.Media;

/// <summary>
/// Strips everything but a known-safe subset from an SVG so it is safe to inline into
/// published pages (inlining is what lets icons inherit <c>currentColor</c>). Shapes,
/// defs, gradients, viewBox and presentation attributes are preserved verbatim.
/// </summary>
public static class SvgSanitizer
{
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";
    private static readonly XNamespace Xlink = "http://www.w3.org/1999/xlink";

    // Real SVG icons are a handful of levels deep; these caps are far above any
    // legitimate graphic yet cut off the pathological inputs that would otherwise blow
    // the stack. The depth cap in particular is load-bearing: the sanitize walk (and
    // XElement.ToString) recurse per nesting level, and a StackOverflowException is
    // uncatchable — it would take the whole process down in a restart loop, since the
    // asset stays Pending and the worker re-enqueues it on every boot.
    private const int MaxDepth = 100;
    private const int MaxElements = 40_000;

    public static (string Svg, int RemovedNodes) Sanitize(string svg)
    {
        // XXE is non-negotiable: no DTDs, no resolver — a doctype (even a benign one)
        // fails the parse before any entity could be expanded.
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
        };

        // Bound the input before building or walking a tree: this streaming scan is
        // iterative (constant stack), so it cannot itself overflow, and it guarantees
        // the recursive sanitize/serialize below stay within MaxDepth frames.
        EnforceStructuralLimits(svg, settings);

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

    private static void EnforceStructuralLimits(string svg, XmlReaderSettings settings)
    {
        using var reader = XmlReader.Create(new StringReader(svg), settings);
        var elements = 0;
        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            if (reader.Depth > MaxDepth)
            {
                throw new InvalidOperationException(
                    $"The SVG is nested more than {MaxDepth} levels deep, which no real graphic needs.");
            }

            if (++elements > MaxElements)
            {
                throw new InvalidOperationException(
                    $"The SVG contains more than {MaxElements:N0} elements, which no real graphic needs.");
            }
        }
    }

    private static void SanitizeNode(XElement element, ref int removed)
    {
        var localName = element.Name.LocalName;

        // Links make no sense in an inlined decorative graphic and are a phishing
        // surface; keep the visuals, drop the wrapper. Handled before the allowlist so
        // an <a>'s children are preserved rather than dropped with it. Case-insensitive
        // because the output is inlined into case-insensitive HTML (<A> is still a link).
        if (localName.Equals("a", StringComparison.OrdinalIgnoreCase))
        {
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
        }

        // Allowlist, not denylist: anything that is not a known-inert SVG element goes
        // whole — including <script>/<style>/SMIL, and crucially the <title>/<desc>/
        // <foreignObject> HTML integration points, where an <iframe srcdoc> would be
        // HTML-parsed and run script once inlined.
        if (!SvgSafety.IsAllowed(localName))
        {
            element.Remove();
            removed++;
            return;
        }

        SanitizeElement(element, ref removed);
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
            // (Case-insensitive: the output is inlined into case-insensitive HTML.)
            if (local.Equals("style", StringComparison.OrdinalIgnoreCase) &&
                attribute.Name.Namespace == XNamespace.None)
            {
                attribute.Remove();
                removed++;
                continue;
            }

            if (IsHref(attribute) && !attribute.Value.TrimStart().StartsWith('#'))
            {
                // javascript:, data:, http(s): — none may survive. An <image> or
                // <use> existed only to follow that reference, so it goes wholesale.
                if (element.Name.LocalName.Equals("image", StringComparison.OrdinalIgnoreCase) ||
                    element.Name.LocalName.Equals("use", StringComparison.OrdinalIgnoreCase))
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
        attribute.Name.LocalName.Equals("href", StringComparison.OrdinalIgnoreCase) &&
        (attribute.Name.Namespace == XNamespace.None || attribute.Name.Namespace == Xlink);
}
