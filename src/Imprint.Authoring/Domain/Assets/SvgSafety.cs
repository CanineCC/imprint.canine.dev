namespace Imprint.Authoring.Domain.Assets;

/// <summary>
/// The allowlist of SVG elements safe to inline into an HTML document. Shared by the
/// ingest sanitizer (Imprint.Media) and the publish-time re-check (Imprint.Publishing)
/// so the two can never drift — a security allowlist maintained in two places is a
/// bypass waiting to happen.
/// </summary>
/// <remarks>
/// An <em>allowlist</em>, deliberately, not a denylist: a denylist has to anticipate
/// every dangerous element and lost twice (uppercase elements; then the
/// <c>&lt;title&gt;</c>/<c>&lt;desc&gt;</c>/<c>&lt;foreignObject&gt;</c> HTML integration
/// points, where an <c>&lt;iframe srcdoc&gt;</c> is HTML-parsed and runs script). Only
/// known-inert presentation, shape, text, paint, gradient, clip/mask and filter
/// elements survive; everything else — including those integration points and any
/// stray HTML element — is dropped whole. Matching is case-insensitive because the
/// output is inlined into case-insensitive HTML.
/// </remarks>
public static class SvgSafety
{
    public static readonly IReadOnlySet<string> AllowedElements = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        // Structure
        "svg", "g", "defs", "symbol", "use", "switch", "view",
        // Shapes
        "path", "rect", "circle", "ellipse", "line", "polyline", "polygon",
        // Text (textPath's href is sanitized to a fragment like every other href)
        "text", "tspan", "textPath",
        // Paint, gradients, patterns
        "linearGradient", "radialGradient", "stop", "pattern",
        // Clipping and masking
        "clipPath", "mask", "marker",
        // Raster embed (href sanitized; external refs drop the element)
        "image",
        // Filters and their primitives
        "filter",
        "feBlend", "feColorMatrix", "feComponentTransfer", "feComposite", "feConvolveMatrix",
        "feDiffuseLighting", "feDisplacementMap", "feDistantLight", "feDropShadow", "feFlood",
        "feFuncA", "feFuncB", "feFuncG", "feFuncR", "feGaussianBlur", "feImage", "feMerge",
        "feMergeNode", "feMorphology", "feOffset", "fePointLight", "feSpecularLighting",
        "feSpotLight", "feTile", "feTurbulence",
    };

    public static bool IsAllowed(string localName) => AllowedElements.Contains(localName);
}
