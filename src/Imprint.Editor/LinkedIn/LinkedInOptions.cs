namespace Imprint.Editor.LinkedIn;

/// <summary>
/// The LinkedIn developer app (configuration <c>Imprint:LinkedIn</c>). Without a client id and secret nothing is
/// sent: the scheduler sees <see cref="Imprint.Authoring.Features.LinkedIn.ILinkedInPoster.IsConfigured"/> false.
/// </summary>
public sealed record LinkedInOptions
{
    public string? ClientId { get; init; }

    public string? ClientSecret { get; init; }

    /// <summary>The <c>Linkedin-Version</c> header (YYYYMM). Versions are retired about a year after release, so
    /// this is configuration: set it to a current one when LinkedIn announces a sunset.</summary>
    public string Version { get; init; } = "202610";

    /// <summary>The editor's public origin, for the OAuth redirect (<c>{PublicOrigin}/linkedin/callback</c>); the
    /// same URL must be registered on the LinkedIn app.</summary>
    public string? PublicOrigin { get; init; }

    public string ApiBase { get; init; } = "https://api.linkedin.com";

    public string OAuthBase { get; init; } = "https://www.linkedin.com/oauth/v2";

    /// <summary>The permission to post as a page the authorizing member administers.</summary>
    public const string Scope = "w_organization_social";

    public bool HasApp => ClientId is { Length: > 0 } && ClientSecret is { Length: > 0 };

    public string RedirectUri => $"{(PublicOrigin ?? "").TrimEnd('/')}/linkedin/callback";
}
