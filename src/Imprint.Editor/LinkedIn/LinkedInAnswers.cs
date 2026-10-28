using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using Imprint.Authoring.Features.LinkedIn;

namespace Imprint.Editor.LinkedIn;

/// <summary>What LinkedIn's answer to a post means for a second attempt, shared by article posts and updates. Only
/// an answer that proves nothing was posted may be sent again: the Posts API has no idempotency key.</summary>
internal static class LinkedInAnswers
{
    private static readonly TimeSpan DefaultRetryAfter = TimeSpan.FromMinutes(15);

    public static async Task<LinkedInSendResult> Interpret(HttpResponseMessage response, DateTimeOffset now, CancellationToken ct)
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
            return new LinkedInSendResult(LinkedInSendStatus.Refused, Reason: $"{status}: {detail}", Retryable: true,
                RetryAfter: Wait(response.Headers.RetryAfter, now));
        }

        if (status is >= 400 and < 500 and not 408 and not 409)
        {
            return new LinkedInSendResult(LinkedInSendStatus.Refused, Reason: $"{status}: {detail}");
        }

        // A timeout, a conflict, a server error: the post may exist. A person checks the page.
        return new LinkedInSendResult(LinkedInSendStatus.Unknown, Reason: $"{status}: {detail}");
    }

    public static TimeSpan Wait(RetryConditionHeaderValue? after, DateTimeOffset now)
    {
        var wait = after is null ? null : after.Delta ?? (after.Date is { } date ? date - now : null);
        return wait is { } w && w > TimeSpan.Zero ? w : DefaultRetryAfter;
    }

    public static async Task<string> Detail(HttpResponseMessage response, CancellationToken ct)
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
}
