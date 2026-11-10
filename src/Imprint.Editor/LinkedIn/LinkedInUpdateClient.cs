using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Imprint.Authoring.Features.LinkedIn;
using Imprint.Authoring.Features.LinkedInUpdates;

namespace Imprint.Editor.LinkedIn;

/// <summary>
/// A standalone update through LinkedIn's Images and Posts APIs, as its sender: a member through their own
/// connection, or a page (<c>urn:li:organization:</c>) through the page connection. Each image goes up first, owned
/// by the sender, then one post carries them. An image that cannot go up means nothing was posted, so that refusal is
/// safe to repeat; the post itself is read like an article's (<see cref="LinkedInAnswers"/>). A first comment goes
/// through the Comments API as the same sender, once the post is out.
/// </summary>
public sealed partial class LinkedInUpdateClient(
    HttpClient http, LinkedInOptions memberApp, LinkedInMemberTokenStore members, ILogger<LinkedInUpdateClient> logger,
    TimeProvider? clock = null, LinkedInPageConnection? page = null) : ILinkedInUpdatePoster
{
    private const string PersonPrefix = "urn:li:person:";
    private const string OrganizationPrefix = "urn:li:organization:";

    private readonly TimeProvider _clock = clock ?? TimeProvider.System;

    /// <summary>A member who connected, while their access has a minute left (member apps get no refresh token, so
    /// an expired connection is connected again by the person, not renewed here); or a page, while the page
    /// connection can still give a token (it renews itself until its refresh token expires).</summary>
    public bool IsConfiguredFor(string authorUrn) =>
        IsOrganization(authorUrn) ? page is { IsUsable: true } : MemberConfigured(authorUrn);

    public async Task<LinkedInSendResult> PostUpdateAsync(LinkedInUpdateRequest request, CancellationToken ct)
    {
        if (await CallerFor(request.AuthorUrn, ct) is not { } caller)
        {
            return NotConnected(request.AuthorUrn);
        }

        var images = new List<(string Urn, string Alt)>();
        foreach (var image in request.Images)
        {
            var (urn, refusal) = await Upload(caller, request.AuthorUrn, image, ct);
            if (refusal is not null)
            {
                return refusal;
            }

            images.Add((urn!, image.Alt));
        }

        using var response = await caller.Send(http, token => Json(
            HttpMethod.Post, $"{caller.App.ApiBase.TrimEnd('/')}/rest/posts", Body(request.AuthorUrn, request.Commentary, images), token, caller.App), ct);
        return response.StatusCode == HttpStatusCode.Unauthorized && caller.IsPage
            ? NotRenewed()
            : await LinkedInAnswers.Interpret(response, _clock.GetUtcNow(), ct);
    }

    /// <summary>The first comment, as the post's author, on the post LinkedIn returned (<c>POST
    /// /rest/socialActions/{post}/comments</c>, Comments API). Plain text: comments are not in LinkedIn's little text
    /// format, so nothing is escaped.</summary>
    public async Task<LinkedInSendResult> CommentAsync(LinkedInCommentRequest request, CancellationToken ct)
    {
        if (await CallerFor(request.AuthorUrn, ct) is not { } caller)
        {
            return NotConnected(request.AuthorUrn);
        }

        using var response = await caller.Send(http, token => Json(
            HttpMethod.Post, $"{caller.App.ApiBase.TrimEnd('/')}/rest/socialActions/{Uri.EscapeDataString(request.PostUrn)}/comments",
            CommentBody(request), token, caller.App), ct);
        if (response.StatusCode == HttpStatusCode.Unauthorized && caller.IsPage)
        {
            return NotRenewed();
        }

        return response.IsSuccessStatusCode
            ? await CommentCreated(response, ct)
            : await LinkedInAnswers.Interpret(response, _clock.GetUtcNow(), ct);
    }

    internal static JsonObject Body(string author, string commentary, IReadOnlyList<(string Urn, string Alt)> images)
    {
        var body = new JsonObject
        {
            ["author"] = author,
            ["commentary"] = Commentary(commentary),
            ["visibility"] = "PUBLIC",
            ["distribution"] = new JsonObject
            {
                ["feedDistribution"] = "MAIN_FEED",
                ["targetEntities"] = new JsonArray(),
                ["thirdPartyDistributionChannels"] = new JsonArray(),
            },
            ["lifecycleState"] = "PUBLISHED",
            ["isReshareDisabledByAuthor"] = false,
        };
        if (images.Count == 1)
        {
            body["content"] = new JsonObject { ["media"] = Image(images[0]) };
        }
        else if (images.Count > 1)
        {
            body["content"] = new JsonObject
            {
                ["multiImage"] = new JsonObject { ["images"] = new JsonArray([.. images.Select(i => (JsonNode)Image(i))]) },
            };
        }

        return body;
    }

    internal static JsonObject CommentBody(LinkedInCommentRequest request) => new()
    {
        ["actor"] = request.AuthorUrn,
        ["object"] = request.PostUrn,
        ["message"] = new JsonObject { ["text"] = request.Text },
    };

    /// <summary>The text in LinkedIn's little text format, with each <c>#word</c> as a real hashtag
    /// (<c>{hashtag|\#|word}</c>) and every other markup character escaped, so the post reads as written.</summary>
    internal static string Commentary(string text)
    {
        var builder = new StringBuilder();
        var position = 0;
        foreach (Match match in Hashtag().Matches(text))
        {
            builder.Append(LinkedInClient.EscapeLittleText(text[position..match.Index]));
            builder.Append("{hashtag|\\#|").Append(LinkedInClient.EscapeLittleText(match.Groups[1].Value)).Append('}');
            position = match.Index + match.Length;
        }

        return builder.Append(LinkedInClient.EscapeLittleText(text[position..])).ToString();
    }

    private static bool IsOrganization(string authorUrn) => authorUrn.StartsWith(OrganizationPrefix, StringComparison.Ordinal);

    private bool MemberConfigured(string authorUrn) =>
        memberApp.HasApp
        && authorUrn.StartsWith(PersonPrefix, StringComparison.Ordinal)
        && members.Read(authorUrn) is { } member
        && member.Tokens.AccessExpiresAt > _clock.GetUtcNow().AddMinutes(1);

    /// <summary>Who signs the calls for this author: the member's own token, or the page connection's (which may be
    /// renewed once after a 401); null when the author is not connected.</summary>
    private async Task<Caller?> CallerFor(string authorUrn, CancellationToken ct)
    {
        if (IsOrganization(authorUrn))
        {
            return page is { IsUsable: true } && await page.AccessToken(rejected: null, ct) is { } token ? new Caller(page.App, token, page) : null;
        }

        return MemberConfigured(authorUrn) && members.Read(authorUrn) is { } member ? new Caller(memberApp, member.Tokens.AccessToken, null) : null;
    }

    private static LinkedInSendResult NotConnected(string authorUrn) => new(LinkedInSendStatus.Refused, Reason: IsOrganization(authorUrn)
        ? "The page is not connected to LinkedIn, or its authorization has expired: a page admin connects it again in the editor (site settings, LinkedIn)."
        : "This member is not connected to LinkedIn, or the connection has expired: connect again in the editor.");

    // A 401 after one renewal: nothing was posted, and it will not change until the page is connected again.
    private static LinkedInSendResult NotRenewed() => new(LinkedInSendStatus.Refused,
        Reason: "LinkedIn refused the page's authorization and it could not be renewed: a page admin connects it again in the editor (site settings, LinkedIn).");

    private static HttpRequestMessage Json(HttpMethod method, string url, JsonNode body, string token, LinkedInOptions app)
    {
        var message = new HttpRequestMessage(method, url)
        {
            Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"),
        };
        Headers(message, token, app);
        return message;
    }

    private static JsonObject Image((string Urn, string Alt) image) => new() { ["id"] = image.Urn, ["altText"] = image.Alt };

    private async Task<(string? Urn, LinkedInSendResult? Refusal)> Upload(Caller caller, string owner, LinkedInUpdateImageFile image, CancellationToken ct)
    {
        var app = caller.App;
        using var answer = await caller.Send(http, token => Json(
            HttpMethod.Post, $"{app.ApiBase.TrimEnd('/')}/rest/images?action=initializeUpload",
            new JsonObject { ["initializeUploadRequest"] = new JsonObject { ["owner"] = owner } }, token, app), ct);
        if (!answer.IsSuccessStatusCode
            || JsonNode.Parse(await answer.Content.ReadAsStringAsync(ct))?["value"] is not JsonObject value
            || value["uploadUrl"]?.GetValue<string>() is not { Length: > 0 } uploadUrl
            || value["image"]?.GetValue<string>() is not { Length: > 0 } urn)
        {
            return (null, ImageRefused(answer, "starting an image upload"));
        }

        using var uploaded = await caller.Send(http, token =>
        {
            var put = new HttpRequestMessage(HttpMethod.Put, uploadUrl) { Content = new ByteArrayContent(image.Content) };
            put.Content.Headers.ContentType = new MediaTypeHeaderValue(image.ContentType);
            put.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return put;
        }, ct);
        return uploaded.IsSuccessStatusCode ? (urn, null) : (null, ImageRefused(uploaded, "uploading an image"));
    }

    // Nothing is posted until every image is up, so a failed image is always a refusal: safe to try again, and
    // retryable when LinkedIn says so (rate limit, unavailable) or the failure is on its side.
    private LinkedInSendResult ImageRefused(HttpResponseMessage response, string step)
    {
        var status = (int)response.StatusCode;
        logger.LogWarning("LinkedIn refused {Step}: {Status}", step, status);
        var retryable = status is 429 or >= 500;
        return new LinkedInSendResult(LinkedInSendStatus.Refused, Reason: $"LinkedIn refused {step} ({status}); nothing was posted.",
            Retryable: retryable, RetryAfter: retryable ? LinkedInAnswers.Wait(response.Headers.RetryAfter, _clock.GetUtcNow()) : null);
    }

    // A created comment: its URN from the answer's body (commentUrn), else the id LinkedIn put in the header. An
    // answer that names neither cannot be pointed at, so it is unknown, as for a post.
    private static async Task<LinkedInSendResult> CommentCreated(HttpResponseMessage response, CancellationToken ct)
    {
        string? urn = null;
        try
        {
            if (JsonNode.Parse(await response.Content.ReadAsStringAsync(ct)) is JsonObject json)
            {
                urn = json["commentUrn"]?.GetValue<string>() ?? json["$URN"]?.GetValue<string>();
            }
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException)
        {
        }

        urn ??= response.Headers.TryGetValues("x-restli-id", out var ids) ? ids.FirstOrDefault() : null;
        return urn is { Length: > 0 }
            ? new LinkedInSendResult(LinkedInSendStatus.Posted, Urn: urn)
            : new LinkedInSendResult(LinkedInSendStatus.Unknown, Reason: "LinkedIn answered without saying which comment it created.");
    }

    private static void Headers(HttpRequestMessage message, string token, LinkedInOptions app)
    {
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        message.Headers.Add("Linkedin-Version", app.Version);
        message.Headers.Add("X-Restli-Protocol-Version", "2.0.0");
    }

    [GeneratedRegex(@"(?<!\S)#(\w+)")]
    private static partial Regex Hashtag();

    /// <summary>The app and the token a sender's calls go out with. A member's token is used as it is; a page's is
    /// renewed once when LinkedIn answers 401, which proves the call did nothing, and the call is made again.</summary>
    private sealed class Caller(LinkedInOptions app, string token, LinkedInPageConnection? page)
    {
        public LinkedInOptions App => app;

        public bool IsPage => page is not null;

        private string Token { get; set; } = token;

        public async Task<HttpResponseMessage> Send(HttpClient http, Func<string, HttpRequestMessage> build, CancellationToken ct)
        {
            HttpResponseMessage response;
            using (var request = build(Token))
            {
                response = await http.SendAsync(request, ct);
            }

            if (page is null || response.StatusCode != HttpStatusCode.Unauthorized
                || await page.AccessToken(rejected: Token, ct) is not { } fresh)
            {
                return response;
            }

            response.Dispose();
            Token = fresh;
            using var again = build(Token);
            return await http.SendAsync(again, ct);
        }
    }
}
