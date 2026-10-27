namespace Imprint.Authoring.Domain.Sites;

/// <summary>An old address on this site and where it now lives: a site path (<c>/new/</c>) or an
/// https URL on another origin. See <see cref="Site.SetRedirects"/>.</summary>
public sealed record SiteRedirect(string From, string To);
