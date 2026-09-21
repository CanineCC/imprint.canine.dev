namespace Imprint.Publishing;

/// <summary>
/// The named server-side templates a widget may select with
/// <c>WidgetDescriptor.PrerenderTemplate</c>. One lookup, so an unknown name fails by rendering
/// nothing rather than by rendering something unexpected.
/// </summary>
public static class PrerenderTemplates
{
    /// <summary>The markup for <paramref name="name"/> over <paramref name="body"/>, or null when the
    /// name is unknown or the payload did not render — both of which leave the widget's fallback in
    /// place rather than publishing an empty section.</summary>
    public static string? Render(string? name, string? body) => name switch
    {
        PricingTemplate.Name => PricingTemplate.Render(body),
        PricingTemplate.OnPremName => PricingTemplate.RenderOnPrem(body),
        _ => null,
    };
}
