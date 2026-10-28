using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Imprint.Editor.LinkedIn;

/// <summary>The member app as its own service, so it cannot be confused with the page app's options.</summary>
public sealed record LinkedInMemberApp(LinkedInOptions Options);

/// <summary>
/// Connecting a member who posts under their own name: OpenID Connect gives their person id, and the token goes to
/// <see cref="LinkedInMemberTokenStore"/> under it. The state is bound to the browser by an HttpOnly cookie, as for
/// the page connection.
/// </summary>
public static class LinkedInMemberOAuth
{
    private const string StateCookie = "imprint-linkedin-member-state";
    private const string SiteCookie = "imprint-linkedin-member-site";

    public static void MapLinkedInMemberConnect(this WebApplication app, bool requireAuthorization)
    {
        var connect = app.MapGet("/linkedin/connect-member", (HttpContext http, string? site, LinkedInMemberApp member) =>
        {
            var options = member.Options;
            if (!options.HasApp || options.PublicOrigin is not { Length: > 0 })
            {
                return Results.Problem("LinkedIn for members is not set up on this install (Imprint:LinkedInMember:ClientId, ClientSecret, PublicOrigin).", statusCode: 503);
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

            return Results.Redirect(LinkedInOAuth.AuthorizeUrl(options, state));
        });

        var callback = app.MapGet("/linkedin/member-callback", async (
            HttpContext http, string? code, string? state, string? error, LinkedInMemberApp member, LinkedInMemberTokenStore members,
            IHttpClientFactory clients, TimeProvider clock, CancellationToken ct) =>
        {
            var options = member.Options;
            var expected = http.Request.Cookies[StateCookie];
            var back = IsSiteId(http.Request.Cookies[SiteCookie]) ? $"/sites/{http.Request.Cookies[SiteCookie]}/settings" : "/";
            http.Response.Cookies.Delete(StateCookie, new CookieOptions { Path = "/linkedin" });
            http.Response.Cookies.Delete(SiteCookie, new CookieOptions { Path = "/linkedin" });
            if (!LinkedInOAuth.StateMatches(expected, state))
            {
                return Results.BadRequest("This LinkedIn answer does not belong to a connect started from this browser.");
            }

            if (error is { Length: > 0 } || code is not { Length: > 0 })
            {
                return Results.Redirect(back + "?linkedin=declined");
            }

            var linkedin = clients.CreateClient("linkedin");
            var now = clock.GetUtcNow();
            var issued = await LinkedInOAuth.Exchange(linkedin, options, new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = options.RedirectUri,
            }, now, now, ct);
            if (issued is null || await Who(linkedin, options, issued.AccessToken, ct) is not { } who)
            {
                return Results.Redirect(back + "?linkedin=failed");
            }

            members.Write(new LinkedInMember(who.Urn, who.Name, issued));
            return Results.Redirect(back + "?linkedin=member-connected");
        });

        if (requireAuthorization)
        {
            connect.RequireAuthorization();
            callback.RequireAuthorization();
        }
    }

    /// <summary>The member's person URN and name, from OpenID Connect's userinfo; null when LinkedIn will not say.</summary>
    internal static async Task<(string Urn, string Name)?> Who(HttpClient http, LinkedInOptions options, string token, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{options.ApiBase.TrimEnd('/')}/v2/userinfo");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        try
        {
            var json = JsonNode.Parse(await response.Content.ReadAsStringAsync(ct));
            var sub = json?["sub"]?.GetValue<string>();
            if (sub is not { Length: > 0 } || !sub.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_'))
            {
                return null;
            }

            return ($"urn:li:person:{sub}", json?["name"]?.GetValue<string>() ?? sub);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool IsSiteId(string? value) =>
        value is { Length: 32 } && value.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');
}
