using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Features.Sites.SetSiteLinkedInSenders;
using Imprint.Authoring.Projections;
using Imprint.Editor.Auth;
using Imprint.EventSourcing;

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
            IHttpClientFactory clients, TimeProvider clock, SiteOverview sites, ICommandDispatcher dispatcher, CancellationToken ct) =>
        {
            var options = member.Options;
            var expected = http.Request.Cookies[StateCookie];
            var siteId = IsSiteId(http.Request.Cookies[SiteCookie]) ? http.Request.Cookies[SiteCookie] : null;
            var back = siteId is not null ? $"/sites/{siteId}/settings" : "/";
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

            // Connected from a site's settings: the member becomes one of that site's senders, so an article or an
            // update can name them without anyone copying a URN. Only on a site the signed-in person may edit.
            // The same identity EditorActor stamps commands with, so access and attribution agree.
            var me = http.User.Identity?.IsAuthenticated == true
                ? http.User.FindFirstValue(ClaimTypes.Email) ?? http.User.FindFirstValue("email")
                  ?? http.User.FindFirstValue("preferred_username") ?? http.User.Identity.Name
                : null;
            if (siteId is not null && sites.Get(SiteId.From(Guid.ParseExact(siteId, "N"))) is { } site
                && (!requireAuthorization || sites.CanAccess(site.Id, me ?? ""))
                && WithMember(site.LinkedInSenders, who.Urn, who.Name) is { } senders)
            {
                using (EditorActor.BeginScope(me))
                {
                    await dispatcher.Dispatch(new SetSiteLinkedInSenders(site.Id, senders), ct);
                }
            }

            return Results.Redirect(back + "?linkedin=member-connected");
        });

        if (requireAuthorization)
        {
            connect.RequireAuthorization();
            callback.RequireAuthorization();
        }
    }

    /// <summary>The site's senders with this member added under their first name (lower-cased plain letters, made
    /// unique with a number); null when they are a sender already.</summary>
    internal static IReadOnlyList<LinkedInSender>? WithMember(IReadOnlyList<LinkedInSender> senders, string urn, string name)
    {
        if (senders.Any(s => s.OrganizationUrn == urn))
        {
            return null;
        }

        // æ, ø and å are spelled out as Danish does; other accented letters drop to their base letter (by table: the
        // host runs without ICU, so Unicode normalization cannot be relied on).
        var spelled = string.Concat((name.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "")
            .ToLowerInvariant().Select(Plain));
        var first = new string(spelled.Where(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c)).ToArray());
        var stem = first.Length > 0 ? first : "member";
        var key = stem;
        for (var n = 2; senders.Any(s => s.Key == key); n++)
        {
            key = $"{stem}-{n}";
        }

        return [.. senders, new LinkedInSender(key, name, urn)];
    }

    private static string Plain(char c) => c switch
    {
        'æ' => "ae", 'ø' => "oe", 'å' => "aa", 'ß' => "ss",
        'à' or 'á' or 'â' or 'ã' or 'ä' => "a", 'è' or 'é' or 'ê' or 'ë' => "e", 'ì' or 'í' or 'î' or 'ï' => "i",
        'ò' or 'ó' or 'ô' or 'õ' or 'ö' => "o", 'ù' or 'ú' or 'û' or 'ü' => "u", 'ý' or 'ÿ' => "y", 'ñ' => "n", 'ç' => "c",
        _ => c.ToString(),
    };

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
