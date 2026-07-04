using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Imprint.Editor.Auth;

/// <summary>
/// Optional Keycloak (OIDC) protection for the editor, in the idiom the rest of this estate
/// already uses: TLS terminates on the reverse proxy, the app trusts its <c>X-Forwarded-*</c>
/// headers, and login is a standard authorization-code + PKCE flow against Keycloak. The
/// delivery plane is unaffected — this guards only the authoring app.
///
/// With no <c>Keycloak:Authority</c> configured the editor runs exactly as before (open, OS
/// user as the actor) <b>except</b> in Production, where running unauthenticated is refused
/// unless the operator explicitly sets <c>Imprint:AllowUnauthenticated=true</c> (e.g. a
/// LAN-only install behind a firewall). This keeps the public default safe by construction.
/// </summary>
public static class ImprintAuthExtensions
{
    /// <summary>Registers auth services and returns the bound options so callers can gate the pipeline.</summary>
    public static KeycloakOptions AddImprintEditorAuth(this WebApplicationBuilder builder)
    {
        var options = new KeycloakOptions();
        builder.Configuration.GetSection("Keycloak").Bind(options);
        builder.Services.AddSingleton(options);

        // The actor accessor exists in every mode; it only resolves a real identity when an
        // AuthenticationStateProvider is present (i.e. auth is enabled below).
        builder.Services.AddScoped(sp => new EditorActor(sp.GetService<AuthenticationStateProvider>()));

        if (!options.Enabled)
        {
            var allowAnonymous = builder.Configuration.GetValue("Imprint:AllowUnauthenticated", false);
            if (builder.Environment.IsProduction() && !allowAnonymous)
            {
                throw new InvalidOperationException(
                    "Keycloak:Authority is not configured but ASPNETCORE_ENVIRONMENT=Production. The Imprint " +
                    "editor refuses to start unauthenticated in Production — anyone reaching it could edit and " +
                    "publish. Set Keycloak__Authority / Keycloak__ClientId / Keycloak__ClientSecret (see " +
                    "docs/deployment.md), or set Imprint__AllowUnauthenticated=true if this install is only " +
                    "reachable from a trusted network.");
            }

            // Dev / test / deliberately-open install: no login, no cascading auth state. Every
            // command is stamped with the OS user, exactly as the editor has always behaved.
            return options;
        }

        // TLS terminates on the proxy (dgx1 nginx): honour its X-Forwarded-* so the app builds
        // https redirect URIs that match the client's registration in Keycloak. Only our own
        // nginx fronts the LAN-bound port, so trust it.
        builder.Services.Configure<ForwardedHeadersOptions>(o =>
        {
            o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
            o.KnownIPNetworks.Clear();
            o.KnownProxies.Clear();
        });

        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddAuthorization();

        builder.Services.AddAuthentication(o =>
            {
                o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                o.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(o =>
            {
                o.Cookie.Name = "imprint.sid.v1";
                o.Cookie.HttpOnly = true;
                o.Cookie.SameSite = SameSiteMode.Lax;
                o.ExpireTimeSpan = TimeSpan.FromHours(12);
                o.SlidingExpiration = true;
            })
            .AddOpenIdConnect(o =>
            {
                o.Authority = options.Authority;
                if (!string.IsNullOrWhiteSpace(options.MetadataAddress))
                {
                    o.MetadataAddress = options.MetadataAddress;
                }

                o.ClientId = options.ClientId;
                o.ClientSecret = options.ClientSecret;
                o.ResponseType = OpenIdConnectResponseType.Code;
                o.UsePkce = true;
                o.RequireHttpsMetadata = options.RequireHttps;
                o.SaveTokens = true; // retain the id_token for a clean RP-initiated logout
                o.GetClaimsFromUserInfoEndpoint = true;
                o.CallbackPath = "/signin-oidc";
                o.SignedOutCallbackPath = "/signout-callback-oidc";
                o.SignedOutRedirectUri = "/";
                o.Scope.Clear();
                o.Scope.Add("openid");
                o.Scope.Add("profile");
                o.Scope.Add("email");
                o.TokenValidationParameters.NameClaimType = "preferred_username";
                o.TokenValidationParameters.RoleClaimType = ClaimTypes.Role;
                // Use a plain front-channel authorization redirect, not Pushed Authorization
                // Requests. .NET 10 opts into PAR when the provider advertises it, but the rest
                // of this estate runs this Keycloak with PAR disabled (see remoteclaude); match
                // that known-good configuration so the login flow is identical across apps.
                o.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Disable;
                o.Events = new OpenIdConnectEvents
                {
                    OnTokenValidated = ctx =>
                    {
                        MapRealmRoles(ctx.Principal);
                        return Task.CompletedTask;
                    },
                    // The default challenge scheme is OIDC, so RequireAuthorization on the
                    // Blazor endpoint funnels every anonymous request through here. A full-page
                    // navigation should redirect to Keycloak, but the SignalR negotiate and the
                    // media route can't follow a 302 — hand them a 401 so a stale cookie surfaces
                    // as a clean reconnect / broken image, never a redirect written into a hub frame.
                    OnRedirectToIdentityProvider = ctx =>
                    {
                        if (ctx.Request.Path.StartsWithSegments("/_blazor") ||
                            ctx.Request.Path.StartsWithSegments("/media"))
                        {
                            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            ctx.HandleResponse();
                            return Task.CompletedTask;
                        }

                        // Skip Keycloak's own login page and go straight to the hinted broker
                        // (e.g. Google), matching how the estate's other apps land on their IdP.
                        if (!string.IsNullOrWhiteSpace(options.IdpHint))
                        {
                            ctx.ProtocolMessage.SetParameter("kc_idp_hint", options.IdpHint);
                        }

                        return Task.CompletedTask;
                    },
                };
            });

        return options;
    }

    /// <summary>
    /// Maps the always-anonymous health probe plus, when auth is enabled, the login/logout
    /// endpoints. The pipeline middleware (ForwardedHeaders/Authentication/Authorization) is
    /// wired in <c>Program.cs</c> so its ordering stays explicit and visible.
    /// </summary>
    public static void MapImprintAuthEndpoints(this WebApplication app, KeycloakOptions options)
    {
        app.MapGet("/healthz", () => Results.Ok("ok")).AllowAnonymous();

        if (!options.Enabled)
        {
            return;
        }

        app.MapGet("/auth/login", (string? returnUrl) =>
            Results.Challenge(
                new AuthenticationProperties { RedirectUri = SafeLocalReturn(returnUrl) },
                [OpenIdConnectDefaults.AuthenticationScheme])).AllowAnonymous();

        app.MapPost("/auth/logout", () =>
            Results.SignOut(
                new AuthenticationProperties { RedirectUri = "/" },
                [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]));

        // Clear a stale local cookie and re-challenge — the escape hatch from a broken session.
        app.MapGet("/auth/relogin", () =>
            Results.SignOut(
                new AuthenticationProperties { RedirectUri = "/" },
                [CookieAuthenticationDefaults.AuthenticationScheme])).AllowAnonymous();
    }

    // Never redirect back to an attacker-supplied absolute URL — only local paths.
    private static string SafeLocalReturn(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && Uri.IsWellFormedUriString(returnUrl, UriKind.Relative) ? returnUrl : "/";

    /// <summary>
    /// Keycloak puts realm roles in the <c>realm_access.roles</c> claim; promote them to
    /// standard role claims so <c>[Authorize(Roles = …)]</c> works if the editor ever gates
    /// an admin surface by role.
    /// </summary>
    private static void MapRealmRoles(ClaimsPrincipal? principal)
    {
        if (principal?.Identity is not ClaimsIdentity id)
        {
            return;
        }

        var realmAccess = principal.FindFirst("realm_access")?.Value;
        if (string.IsNullOrWhiteSpace(realmAccess))
        {
            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(realmAccess);
            if (doc.RootElement.TryGetProperty("roles", out var roles) && roles.ValueKind == JsonValueKind.Array)
            {
                foreach (var r in roles.EnumerateArray())
                {
                    if (r.GetString() is { Length: > 0 } role && !id.HasClaim(ClaimTypes.Role, role))
                    {
                        id.AddClaim(new Claim(ClaimTypes.Role, role));
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Malformed claim — ignore; absence of a role simply means no elevated access.
        }
    }
}
