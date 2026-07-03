using System.Globalization;
using System.Text;
using Imprint.Authoring.Domain.Sites;

namespace Imprint.Rendering;

/// <summary>
/// Emits a <see cref="Theme"/> as CSS custom properties. <c>light-dark()</c> per color
/// token is the entire dark-mode implementation; <c>color-scheme</c> overrides via
/// <c>[data-theme]</c> serve the explicit toggle. Output is deterministic (properties
/// sorted ordinally) so the publisher's content hash only changes when the theme does.
/// </summary>
public static class ThemeCss
{
    private static string? _structuralCss;

    public static string Emit(Theme theme) => EmitFor(theme, ":root");

    /// <summary>
    /// Same variables under an arbitrary selector — the editor canvas scopes the site
    /// theme to the canvas root so site tokens and editor chrome never bleed into each
    /// other (docs/editor-ux.md §1).
    /// </summary>
    public static string EmitScoped(Theme theme, string selector) => EmitFor(theme, selector);

    /// <summary>
    /// The structural stylesheet (imprint-base.css) as a string. Embedded so the
    /// publisher can compose <c>site.css = Emit(theme) + StructuralCss</c> without
    /// depending on static-web-asset paths; the wwwroot copy of the same file serves
    /// the editor canvas.
    /// </summary>
    public static string StructuralCss => _structuralCss ??= LoadStructuralCss();

    private static string EmitFor(Theme theme, string selector)
    {
        var props = new SortedDictionary<string, string>(StringComparer.Ordinal);

        foreach (var (name, token) in theme.Tokens)
        {
            props[$"--ip-{name}"] = $"light-dark({token.Light}, {token.Dark})";
        }

        var typography = theme.Typography;
        props["--ip-font-heading"] = FontStackCss(typography.Heading);
        props["--ip-font-body"] = FontStackCss(typography.Body);
        props["--ip-radius"] = $"{typography.RadiusPx}px";

        var spacing = typography.Spacing switch
        {
            SpacingScale.Compact => 0.8,
            SpacingScale.Spacious => 1.25,
            _ => 1.0,
        };
        foreach (var step in (int[])[1, 2, 3, 4, 6, 8])
        {
            // 4px per step before scaling — the familiar quarter-rem grid.
            props[$"--ip-space-{step}"] = Rem(step * 4 * spacing);
        }

        foreach (var (name, step) in (ReadOnlySpan<(string, int)>)[("sm", -1), ("base", 0), ("lg", 1), ("xl", 2), ("2xl", 3), ("3xl", 4), ("4xl", 5)])
        {
            props[$"--ip-text-{name}"] = FluidClamp(typography.BaseSizePx, typography.ScaleRatio, step);
        }

        var css = new StringBuilder(1024);
        css.Append(selector).Append(" {\n");
        css.Append("  color-scheme: light dark;\n");
        foreach (var (name, value) in props)
        {
            css.Append("  ").Append(name).Append(": ").Append(value).Append(";\n");
        }

        css.Append("}\n");
        css.Append(selector).Append("[data-theme=light] { color-scheme: light; }\n");
        css.Append(selector).Append("[data-theme=dark] { color-scheme: dark; }\n");
        return css.ToString();
    }

    /// <summary>
    /// Modular fluid type: size n = base × ratio^n at a 320px viewport, growing ~12%
    /// by 1280px. Linear interpolation between the two endpoints gives the clamp
    /// midterm; rem everywhere so browser font-size preferences still scale the site.
    /// </summary>
    private static string FluidClamp(int baseSizePx, double scaleRatio, int step)
    {
        const double growth = 1.12;
        const double fromVw = 320;
        const double toVw = 1280;

        var minPx = baseSizePx * Math.Pow(scaleRatio, step);
        var maxPx = minPx * growth;
        var slopeVw = (maxPx - minPx) / (toVw - fromVw) * 100;
        var interceptPx = minPx - (slopeVw / 100 * fromVw);
        return $"clamp({Rem(minPx)}, {Rem(interceptPx)} + {Number(slopeVw)}vw, {Rem(maxPx)})";
    }

    // rem is computed against the 16px root default; users who raise their browser
    // font size scale the whole site proportionally, which is the accessible behavior.
    private static string Rem(double px) => $"{Number(px / 16)}rem";

    private static string Number(double value) =>
        Math.Round(value, 4).ToString("0.####", CultureInfo.InvariantCulture);

    /// <summary>
    /// Curated system stacks (informed by modernfontstacks.com): zero requests, zero
    /// FOUT, present on every mainstream OS. Quoted names contain spaces; generic
    /// family always last.
    /// </summary>
    private static string FontStackCss(FontStack stack) => stack switch
    {
        FontStack.Sans => "system-ui, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif",
        FontStack.Humanist => "Seravek, 'Gill Sans Nova', Ubuntu, Calibri, 'DejaVu Sans', source-sans-pro, sans-serif",
        FontStack.Geometric => "'Avenir Next', Avenir, Montserrat, Corbel, 'Century Gothic', Futura, 'URW Gothic', system-ui, sans-serif",
        FontStack.Serif => "Charter, 'Bitstream Charter', 'Sitka Text', Cambria, Georgia, serif",
        FontStack.Slab => "Rockwell, 'Rockwell Nova', 'Roboto Slab', 'DejaVu Serif', 'Sitka Small', serif",
        FontStack.Mono => "ui-monospace, 'Cascadia Code', Menlo, Consolas, 'Source Code Pro', monospace",
        _ => "system-ui, sans-serif",
    };

    private static string LoadStructuralCss()
    {
        using var stream = typeof(ThemeCss).Assembly.GetManifestResourceStream("Imprint.Rendering.styles.imprint-base.css")
            ?? throw new InvalidOperationException("Embedded resource 'imprint-base.css' is missing from Imprint.Rendering.");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
