namespace Imprint.Authoring.Features.LinkedIn;

/// <summary>An article post: the organization that posts it, the text, and the link card's fields (LinkedIn's
/// Posts API does not fetch a link's preview, so the title and description are sent).</summary>
public sealed record LinkedInPostRequest(string AuthorUrn, string Commentary, string ArticleUrl, string Title, string Description);

public enum LinkedInSendStatus
{
    /// <summary>LinkedIn accepted it and returned the post's URN.</summary>
    Posted,

    /// <summary>LinkedIn refused it, so nothing was posted.</summary>
    Refused,

    /// <summary>The result is not known: it may have posted.</summary>
    Unknown,
}

/// <param name="Retryable">Only for <see cref="LinkedInSendStatus.Refused"/>: a refusal that may be sent again
/// (rate limit, service unavailable).</param>
public sealed record LinkedInSendResult(
    LinkedInSendStatus Status, string? Urn = null, string? Reason = null, bool Retryable = false, TimeSpan? RetryAfter = null);

/// <summary>Sends one post to LinkedIn. The real client lives in the editor host (it holds the tokens); tests use
/// a fake. <see cref="IsConfigured"/> false means nothing is sent and nothing is recorded.</summary>
public interface ILinkedInPoster
{
    bool IsConfigured { get; }

    Task<LinkedInSendResult> PostAsync(LinkedInPostRequest request, CancellationToken ct);
}

/// <summary>The default: LinkedIn is not set up, so the scheduler sends nothing.</summary>
public sealed class NotConfiguredLinkedInPoster : ILinkedInPoster
{
    public bool IsConfigured => false;

    public Task<LinkedInSendResult> PostAsync(LinkedInPostRequest request, CancellationToken ct) =>
        Task.FromResult(new LinkedInSendResult(LinkedInSendStatus.Refused, Reason: "LinkedIn is not set up on this install."));
}

/// <summary>How the scheduler paces posts: at most <see cref="MaxPerSitePerHour"/> per site in any hour (so a
/// launch that publishes many articles at once cannot flood the page), and only for articles published within
/// <see cref="MaxAge"/> (an old article given a text later is not announced as news).</summary>
public sealed record LinkedInPostingOptions
{
    public int MaxPerSitePerHour { get; init; } = 2;

    public TimeSpan MaxAge { get; init; } = TimeSpan.FromDays(7);
}
