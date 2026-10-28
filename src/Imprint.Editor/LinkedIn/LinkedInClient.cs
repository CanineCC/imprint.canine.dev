using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Imprint.Authoring.Features.LinkedIn;

namespace Imprint.Editor.LinkedIn;

/// <summary>
/// LinkedIn's Posts API (<c>POST /rest/posts</c>), as the author the article names: an organization page through the
/// page app and its connection, or a person (urn:li:person:) through their own member connection. What each answer
/// means for a second attempt is the point of this class: only an answer that proves nothing was posted may be
/// sent again, because the API has no idempotency key.
/// </summary>
public sealed class LinkedInClient(
    HttpClient http, LinkedInOptions options, LinkedInTokenStore tokens, ILogger<LinkedInClient> logger, TimeProvider? clock = null,
    LinkedInOptions? memberApp = null, LinkedInMemberTokenStore? members = null)
    : ILinkedInPoster
{
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;

    public bool IsConfigured => options.HasApp && tokens.Read() is not null;

    public bool IsConfiguredFor(string authorUrn) => IsPerson(authorUrn) ? MemberToken(authorUrn) is not null : IsConfigured;

    public async Task<LinkedInSendResult> PostAsync(LinkedInPostRequest request, CancellationToken ct)
    {
        if (IsPerson(request.AuthorUrn))
        {
            // A member's connection has no refresh token: an expired one is connected again by the person.
            if (MemberToken(request.AuthorUrn) is not { } memberToken)
            {
                return Refused("This member is not connected to LinkedIn, or the connection has expired: connect again in the editor.");
            }

            using var answer = await Send(request, memberToken, memberApp!, ct);
            return await Interpret(answer, ct);
        }

        if (await AccessToken(forceRefresh: false, ct) is not { } token)
        {
            return Refused("LinkedIn is not connected, or its authorization has expired: connect it again in the editor.");
        }

        var response = await Send(request, token, options, ct);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            // A 401 proves nothing was posted, so one refresh and one more try is safe.
            response.Dispose();
            if (await AccessToken(forceRefresh: true, ct) is not { } fresh)
            {
                return Refused("LinkedIn refused the authorization and it could not be refreshed: connect it again in the editor.");
            }

            response = await Send(request, fresh, options, ct);
        }

        using (response)
        {
            return await Interpret(response, ct);
        }
    }

    private static bool IsPerson(string authorUrn) => authorUrn.StartsWith("urn:li:person:", StringComparison.Ordinal);

    /// <summary>The member's access token while it has a minute left; null when the member app or the person's
    /// connection is missing or expired.</summary>
    private string? MemberToken(string authorUrn) =>
        memberApp is { HasApp: true } && members?.Read(authorUrn) is { } member
        && member.Tokens.AccessExpiresAt > _clock.GetUtcNow().AddMinutes(1)
            ? member.Tokens.AccessToken
            : null;

    private async Task<HttpResponseMessage> Send(LinkedInPostRequest request, string token, LinkedInOptions app, CancellationToken ct)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, $"{app.ApiBase.TrimEnd('/')}/rest/posts")
        {
            Content = new StringContent(Body(request).ToJsonString(), Encoding.UTF8, "application/json"),
        };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        message.Headers.Add("Linkedin-Version", app.Version);
        message.Headers.Add("X-Restli-Protocol-Version", "2.0.0");
        return await http.SendAsync(message, ct);
    }

    internal static JsonObject Body(LinkedInPostRequest request)
    {
        var article = new JsonObject { ["source"] = request.ArticleUrl, ["title"] = request.Title };
        if (request.Description is { Length: > 0 } description)
        {
            article["description"] = description;
        }

        return new JsonObject
        {
            ["author"] = request.AuthorUrn,
            ["commentary"] = EscapeLittleText(request.Commentary),
            ["visibility"] = "PUBLIC",
            ["distribution"] = new JsonObject
            {
                ["feedDistribution"] = "MAIN_FEED",
                ["targetEntities"] = new JsonArray(),
                ["thirdPartyDistributionChannels"] = new JsonArray(),
            },
            ["content"] = new JsonObject { ["article"] = article },
            ["lifecycleState"] = "PUBLISHED",
            ["isReshareDisabledByAuthor"] = false,
        };
    }

    /// <summary>LinkedIn reads commentary as its "little" text format, where these characters are markup (a
    /// mention, a hashtag, a template): escaped, the text appears exactly as the reviewer signed it.</summary>
    internal static string EscapeLittleText(string text)
    {
        var builder = new StringBuilder(text.Length + 16);
        foreach (var c in text)
        {
            if (c is '\\' or '|' or '{' or '}' or '@' or '[' or ']' or '(' or ')' or '<' or '>' or '#' or '*' or '_' or '~')
            {
                builder.Append('\\');
            }

            builder.Append(c);
        }

        return builder.ToString();
    }

    private Task<LinkedInSendResult> Interpret(HttpResponseMessage response, CancellationToken ct) =>
        LinkedInAnswers.Interpret(response, _clock.GetUtcNow(), ct);

    private static LinkedInSendResult Refused(string reason) => new(LinkedInSendStatus.Refused, Reason: reason);

    /// <summary>A usable access token: the stored one while it has a minute left, else a refreshed one.</summary>
    private async Task<string?> AccessToken(bool forceRefresh, CancellationToken ct)
    {
        if (tokens.Read() is not { } stored)
        {
            return null;
        }

        var now = _clock.GetUtcNow();
        if (!forceRefresh && stored.AccessExpiresAt > now.AddMinutes(1))
        {
            return stored.AccessToken;
        }

        if (stored.RefreshToken is not { Length: > 0 } refresh || stored.RefreshExpiresAt is { } dies && dies <= now)
        {
            return null;
        }

        var exchanged = await LinkedInOAuth.Exchange(http, options, new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refresh,
        }, stored.ConnectedAt, now, ct);
        if (exchanged is null)
        {
            logger.LogWarning("LinkedIn token refresh was refused.");
            return null;
        }

        // LinkedIn may not return a new refresh token on refresh: the old one stays valid until it expires.
        var next = exchanged.RefreshToken is null
            ? exchanged with { RefreshToken = stored.RefreshToken, RefreshExpiresAt = stored.RefreshExpiresAt }
            : exchanged;
        tokens.Write(next);
        return next.AccessToken;
    }
}
