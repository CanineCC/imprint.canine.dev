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

    /// <summary>A member posting under their own name: their id from OpenID Connect, and the right to post.</summary>
    public const string MemberScope = "openid profile w_member_social";

    /// <summary>What the authorization asks for: <see cref="Scope"/> for the page app, <see cref="MemberScope"/> for
    /// the member app (<see cref="ForMembers"/>).</summary>
    public string Scopes { get; init; } = Scope;

    /// <summary>Where LinkedIn sends the browser back; each app registers its own.</summary>
    public string CallbackPath { get; init; } = "/linkedin/callback";

    public bool HasApp => ClientId is { Length: > 0 } && ClientSecret is { Length: > 0 };

    public string RedirectUri => $"{(PublicOrigin ?? "").TrimEnd('/')}{CallbackPath}";

    /// <summary>The member app (configuration <c>Imprint:LinkedInMember</c>): its own client, because LinkedIn may
    /// not allow the page product (Community Management) on the same app as member sharing.</summary>
    public static LinkedInOptions ForMembers(LinkedInOptions app) =>
        app with { Scopes = MemberScope, CallbackPath = "/linkedin/member-callback" };
}
