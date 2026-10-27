using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Imprint.Authoring.Features.LinkedIn;

namespace Imprint.Editor.LinkedIn;

/// <summary>
/// LinkedIn's Posts API (<c>POST /rest/posts</c>), as the organization page the article names. What each answer
/// means for a second attempt is the point of this class: only an answer that proves nothing was posted may be
/// sent again, because the API has no idempotency key.
/// </summary>
public sealed class LinkedInClient(
    HttpClient http, LinkedInOptions options, LinkedInTokenStore tokens, ILogger<LinkedInClient> logger, TimeProvider? clock = null)
    : ILinkedInPoster
{
    private static readonly TimeSpan DefaultRetryAfter = TimeSpan.FromMinutes(15);
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;

    public bool IsConfigured => options.HasApp && tokens.Read() is not null;

    public async Task<LinkedInSendResult> PostAsync(LinkedInPostRequest request, CancellationToken ct)
    {
        if (await AccessToken(forceRefresh: false, ct) is not { } token)
        {
            return Refused("LinkedIn is not connected, or its authorization has expired: connect it again in the editor.");
        }

        var response = await Send(request, token, ct);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            // A 401 proves nothing was posted, so one refresh and one more try is safe.
            response.Dispose();
            if (await AccessToken(forceRefresh: true, ct) is not { } fresh)
            {
                return Refused("LinkedIn refused the authorization and it could not be refreshed: connect it again in the editor.");
            }

            response = await Send(request, fresh, ct);
        }

        using (response)
        {
            return await Interpret(response, ct);
        }
    }

    private async Task<HttpResponseMessage> Send(LinkedInPostRequest request, string token, CancellationToken ct)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, $"{options.ApiBase.TrimEnd('/')}/rest/posts")
        {
            Content = new StringContent(Body(request).ToJsonString(), Encoding.UTF8, "application/json"),
        };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        message.Headers.Add("Linkedin-Version", options.Version);
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

    private async Task<LinkedInSendResult> Interpret(HttpResponseMessage response, CancellationToken ct)
    {
        var status = (int)response.StatusCode;
        if (status == 201)
        {
            var urn = response.Headers.TryGetValues("x-restli-id", out var ids) ? ids.FirstOrDefault() : null;
            return new LinkedInSendResult(LinkedInSendStatus.Posted, Urn: urn);
        }

        var detail = await Detail(response, ct);
        if (status is 429 or 503)
        {
            var wait = response.Headers.RetryAfter is { } after
                ? after.Delta ?? (after.Date is { } date ? date - _clock.GetUtcNow() : null)
                : null;
            return new LinkedInSendResult(LinkedInSendStatus.Refused, Reason: $"{status}: {detail}", Retryable: true,
                RetryAfter: wait is { } w && w > TimeSpan.Zero ? w : DefaultRetryAfter);
        }

        if (status is >= 400 and < 500 and not 408 and not 409)
        {
            return Refused($"{status}: {detail}");
        }

        // A timeout, a conflict, a server error: the post may exist. A person checks the page.
        return new LinkedInSendResult(LinkedInSendStatus.Unknown, Reason: $"{status}: {detail}");
    }

    private static async Task<string> Detail(HttpResponseMessage response, CancellationToken ct)
    {
        var body = await response.Content.ReadAsStringAsync(ct);
        try
        {
            if (JsonNode.Parse(body) is JsonObject json && json["message"]?.GetValue<string>() is { Length: > 0 } message)
            {
                return message.Length > 300 ? message[..300] : message;
            }
        }
        catch (JsonException)
        {
        }

        return response.ReasonPhrase ?? "no detail";
    }

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
