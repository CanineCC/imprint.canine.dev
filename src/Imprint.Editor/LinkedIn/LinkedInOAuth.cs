using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Imprint.Editor.LinkedIn;

/// <summary>
/// Connecting LinkedIn: a member with a role on the organization pages authorizes the app once (3-legged OAuth),
/// and the tokens go to <see cref="LinkedInTokenStore"/>. The OAuth <c>state</c> is bound to the browser that
/// started the flow by an HttpOnly cookie, so a callback carrying someone else's code is refused.
/// </summary>
public static class LinkedInOAuth
{
    private const string StateCookie = "imprint-linkedin-state";
    private const string SiteCookie = "imprint-linkedin-site";

    public static void MapLinkedInConnect(this WebApplication app, bool requireAuthorization)
    {
        var connect = app.MapGet("/linkedin/connect", (HttpContext http, string? site, LinkedInOptions options) =>
        {
            if (!options.HasApp || options.PublicOrigin is not { Length: > 0 })
            {
                return Results.Problem("LinkedIn is not set up on this install (Imprint:LinkedIn:ClientId, ClientSecret, PublicOrigin).", statusCode: 503);
            }

            var state = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            var cookie = new CookieOptions
            {
                HttpOnly = true,
                Secure = http.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Path = "/linkedin",
                MaxAge = TimeSpan.FromMinutes(10),
            };
            http.Response.Cookies.Append(StateCookie, state, cookie);
            if (IsSiteId(site))
            {
                http.Response.Cookies.Append(SiteCookie, site!, cookie);
            }

            return Results.Redirect(AuthorizeUrl(options, state));
        });

        var callback = app.MapGet("/linkedin/callback", async (
            HttpContext http, string? code, string? state, string? error, LinkedInOptions options, LinkedInTokenStore tokens,
            IHttpClientFactory clients, TimeProvider clock, CancellationToken ct) =>
        {
            var expected = http.Request.Cookies[StateCookie];
            var back = IsSiteId(http.Request.Cookies[SiteCookie]) ? $"/sites/{http.Request.Cookies[SiteCookie]}/settings" : "/";
            http.Response.Cookies.Delete(StateCookie, new CookieOptions { Path = "/linkedin" });
            http.Response.Cookies.Delete(SiteCookie, new CookieOptions { Path = "/linkedin" });
            if (!StateMatches(expected, state))
            {
                return Results.BadRequest("This LinkedIn answer does not belong to a connect started from this browser.");
            }

            if (error is { Length: > 0 } || code is not { Length: > 0 })
            {
                return Results.Redirect(back + "?linkedin=declined");
            }

            var now = clock.GetUtcNow();
            var issued = await Exchange(clients.CreateClient("linkedin"), options, new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = options.RedirectUri,
            }, now, now, ct);
            if (issued is null)
            {
                return Results.Redirect(back + "?linkedin=failed");
            }

            tokens.Write(issued);
            return Results.Redirect(back + "?linkedin=connected");
        });

        if (requireAuthorization)
        {
            connect.RequireAuthorization();
            callback.RequireAuthorization();
        }
    }

    // Only a site id goes into the redirect, so the cookie cannot send the browser anywhere else.
    private static bool IsSiteId(string? value) =>
        value is { Length: 32 } && value.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');

    internal static string AuthorizeUrl(LinkedInOptions options, string state) =>
        $"{options.OAuthBase.TrimEnd('/')}/authorization?response_type=code" +
        $"&client_id={Uri.EscapeDataString(options.ClientId!)}" +
        $"&redirect_uri={Uri.EscapeDataString(options.RedirectUri)}" +
        $"&state={state}" +
        $"&scope={Uri.EscapeDataString(options.Scopes)}";

    internal static bool StateMatches(string? expected, string? given) =>
        expected is { Length: > 0 } && given is { Length: > 0 }
        && CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.ASCII.GetBytes(expected), System.Text.Encoding.ASCII.GetBytes(given));

    /// <summary>POSTs to the token endpoint and reads the tokens; null when LinkedIn refuses.</summary>
    internal static async Task<LinkedInTokens?> Exchange(
        HttpClient http, LinkedInOptions options, Dictionary<string, string> form, DateTimeOffset connectedAt,
        DateTimeOffset now, CancellationToken ct)
    {
        form["client_id"] = options.ClientId!;
        form["client_secret"] = options.ClientSecret!;
        using var response = await http.PostAsync(
            $"{options.OAuthBase.TrimEnd('/')}/accessToken", new FormUrlEncodedContent(form), ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        try
        {
            if (JsonNode.Parse(await response.Content.ReadAsStringAsync(ct)) is not JsonObject json
                || json["access_token"]?.GetValue<string>() is not { Length: > 0 } access)
            {
                return null;
            }

            var expiresIn = json["expires_in"]?.GetValue<long>() ?? 0;
            var refresh = json["refresh_token"]?.GetValue<string>();
            var refreshExpiresIn = json["refresh_token_expires_in"]?.GetValue<long>();
            return new LinkedInTokens(
                access, now.AddSeconds(expiresIn), refresh is { Length: > 0 } ? refresh : null,
                refreshExpiresIn is { } r ? now.AddSeconds(r) : null, connectedAt);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
