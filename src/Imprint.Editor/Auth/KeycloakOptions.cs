namespace Imprint.Editor.Auth;

/// <summary>
/// Keycloak / OIDC configuration, bound from the <c>Keycloak</c> configuration section
/// (the same shape the rest of this estate uses). Authentication stays <b>off</b> until an
/// <see cref="Authority"/> is supplied: a single operator can run the editor with no login
/// at all — the OS user is recorded as the actor, exactly as before — and Keycloak slots in
/// by providing these values (typically via <c>Keycloak__*</c> environment variables) in a
/// real deployment. Refusing to run unauthenticated in Production is enforced elsewhere
/// (<see cref="ImprintAuthExtensions.AddImprintEditorAuth"/>).
/// </summary>
public sealed class KeycloakOptions
{
    /// <summary>The OIDC issuer, e.g. <c>https://auth.canine.dev/realms/master</c>.</summary>
    public string? Authority { get; set; }

    /// <summary>
    /// Optional explicit discovery-document URL. Only needed when the issuer host the app
    /// must reach differs from the one Keycloak advertises (rare behind a proxy).
    /// </summary>
    public string? MetadataAddress { get; set; }

    /// <summary>The confidential client registered in Keycloak for the editor.</summary>
    public string ClientId { get; set; } = "imprint";

    /// <summary>The confidential client's secret.</summary>
    public string? ClientSecret { get; set; }

    /// <summary>Require HTTPS for metadata/token retrieval. True in real deployments.</summary>
    public bool RequireHttps { get; set; } = true;

    /// <summary>
    /// Optional Keycloak identity-provider alias to jump straight to (sent as
    /// <c>kc_idp_hint</c>), e.g. <c>google</c> — so the user lands on "Sign in with Google"
    /// rather than a Keycloak username/password form. Leave null to show Keycloak's own page.
    /// </summary>
    public string? IdpHint { get; set; }

    /// <summary>Authentication is active only once an <see cref="Authority"/> is configured.</summary>
    public bool Enabled => !string.IsNullOrWhiteSpace(Authority);
}
