using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Imprint.Authoring.Features.LinkedIn;
using Imprint.Authoring.Features.LinkedInUpdates;

namespace Imprint.Editor.LinkedIn;

/// <summary>
/// A member's standalone update through LinkedIn's Images and Posts APIs: each image goes up first (as the member),
/// then one post carries them. An image that cannot go up means nothing was posted, so that refusal is safe to
/// repeat; the post itself is read like an article's (<see cref="LinkedInAnswers"/>).
/// </summary>
public sealed partial class LinkedInUpdateClient(
    HttpClient http, LinkedInOptions memberApp, LinkedInMemberTokenStore members, ILogger<LinkedInUpdateClient> logger,
    TimeProvider? clock = null) : ILinkedInUpdatePoster
{
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;

    /// <summary>Only a member who connected, while their access has a minute left (member apps get no refresh
    /// token, so an expired connection is connected again by the person, not renewed here).</summary>
    public bool IsConfiguredFor(string authorUrn) =>
        memberApp.HasApp
        && authorUrn.StartsWith("urn:li:person:", StringComparison.Ordinal)
        && members.Read(authorUrn) is { } member
        && member.Tokens.AccessExpiresAt > _clock.GetUtcNow().AddMinutes(1);

    public async Task<LinkedInSendResult> PostUpdateAsync(LinkedInUpdateRequest request, CancellationToken ct)
    {
        if (!IsConfiguredFor(request.AuthorUrn) || members.Read(request.AuthorUrn) is not { } member)
        {
            return new LinkedInSendResult(LinkedInSendStatus.Refused,
                Reason: "This member is not connected to LinkedIn, or the connection has expired: connect again in the editor.");
        }

        var token = member.Tokens.AccessToken;
        var images = new List<(string Urn, string Alt)>();
        foreach (var image in request.Images)
        {
            var (urn, refusal) = await Upload(request.AuthorUrn, token, image, ct);
            if (refusal is not null)
            {
                return refusal;
            }

            images.Add((urn!, image.Alt));
        }

        using var message = new HttpRequestMessage(HttpMethod.Post, $"{memberApp.ApiBase.TrimEnd('/')}/rest/posts")
        {
            Content = new StringContent(Body(request.AuthorUrn, request.Commentary, images).ToJsonString(), Encoding.UTF8, "application/json"),
        };
        Headers(message, token);
        using var response = await http.SendAsync(message, ct);
        return await LinkedInAnswers.Interpret(response, _clock.GetUtcNow(), ct);
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

    private static JsonObject Image((string Urn, string Alt) image) => new() { ["id"] = image.Urn, ["altText"] = image.Alt };

    private async Task<(string? Urn, LinkedInSendResult? Refusal)> Upload(string owner, string token, LinkedInUpdateImageFile image, CancellationToken ct)
    {
        using var init = new HttpRequestMessage(HttpMethod.Post, $"{memberApp.ApiBase.TrimEnd('/')}/rest/images?action=initializeUpload")
        {
            Content = new StringContent(new JsonObject { ["initializeUploadRequest"] = new JsonObject { ["owner"] = owner } }.ToJsonString(),
                Encoding.UTF8, "application/json"),
        };
        Headers(init, token);
        using var answer = await http.SendAsync(init, ct);
        if (!answer.IsSuccessStatusCode
            || JsonNode.Parse(await answer.Content.ReadAsStringAsync(ct))?["value"] is not JsonObject value
            || value["uploadUrl"]?.GetValue<string>() is not { Length: > 0 } uploadUrl
            || value["image"]?.GetValue<string>() is not { Length: > 0 } urn)
        {
            return (null, ImageRefused(answer, "starting an image upload"));
        }

        using var put = new HttpRequestMessage(HttpMethod.Put, uploadUrl) { Content = new ByteArrayContent(image.Content) };
        put.Content.Headers.ContentType = new MediaTypeHeaderValue(image.ContentType);
        put.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var uploaded = await http.SendAsync(put, ct);
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

    private void Headers(HttpRequestMessage message, string token)
    {
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        message.Headers.Add("Linkedin-Version", memberApp.Version);
        message.Headers.Add("X-Restli-Protocol-Version", "2.0.0");
    }

    [GeneratedRegex(@"(?<!\S)#(\w+)")]
    private static partial Regex Hashtag();
}
