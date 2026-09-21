namespace Imprint.Publishing;

/// <summary>
/// The named server-side templates a widget may select with
/// <c>WidgetDescriptor.PrerenderTemplate</c>. One lookup, so an unknown name fails by rendering
/// nothing rather than by rendering something unexpected.
/// </summary>
public static class PrerenderTemplates
{
    /// <summary>
    /// The markup for <paramref name="name"/> over <paramref name="body"/>, or null when the name is
    /// unknown or the payload did not render — both of which leave the widget's fallback in place
    /// rather than publishing an empty section.
    /// </summary>
    /// <param name="name">The template the widget's descriptor selected.</param>
    /// <param name="body">What the fetch returned.</param>
    /// <param name="url">
    /// The URL the body was fetched FROM. ★ This is how a template resolves the relative links its
    /// payload carries — a report path is `/api/oss/…`, and the origin that serves it is the origin
    /// that served the payload, by construction. It is not passed as a widget prop because the bake is
    /// keyed by (url, template) and cannot see props: a prop that changed the output would produce one
    /// fragment shared by instances that disagreed about it.
    /// </param>
    public static string? Render(string? name, string? body, string? url = null) => name switch
    {
        PricingTemplate.Name => PricingTemplate.Render(body),
        PricingTemplate.OnPremName => PricingTemplate.RenderOnPrem(body),
        ScoreCardTemplate.Name => ScoreCardTemplate.Render(body, OriginOf(url)),
        ScoreCardTemplate.FullName => ScoreCardTemplate.RenderFull(body, OriginOf(url)),
        ScoreCardTemplate.OneName => ScoreCardTemplate.RenderOne(body, OriginOf(url)),
        FindingsTemplate.Name => FindingsTemplate.Render(body, OriginOf(url)),
        CompositionTemplate.Name => CompositionTemplate.Render(body, OriginOf(url)),
        BandScaleTemplate.Name => BandScaleTemplate.Render(body, OriginOf(url)),
        LanguageSupportTemplate.Name => LanguageSupportTemplate.Render(body),
        ArchitectureSvgTemplate.Name => ArchitectureSvgTemplate.Render(body),
        _ => null,
    };

    /// <summary>
    /// The markup for a widget rendered from its OWN props. Same shape of answer as
    /// <see cref="Render"/> — an unknown name renders nothing rather than something unexpected — and
    /// a separate lookup because these templates take no fetched body at all.
    /// </summary>
    public static string? RenderFromProps(string? name, Func<string, string?> props) => name switch
    {
        LinkCardsTemplate.Name => LinkCardsTemplate.Render(props),
        FlowTemplate.Name => FlowTemplate.Render(props),
        _ => null,
    };

    /// <summary>Scheme and host of the fetch URL, or null when there isn't one to trust.</summary>
    private static string? OriginOf(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps
            ? uri.GetLeftPart(UriPartial.Authority)
            : null;
}
