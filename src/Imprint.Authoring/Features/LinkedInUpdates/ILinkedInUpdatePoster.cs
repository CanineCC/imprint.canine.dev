using Imprint.Authoring.Features.LinkedIn;

namespace Imprint.Authoring.Features.LinkedInUpdates;

/// <summary>One image as it goes to LinkedIn: the bytes, their type, and the alt text.</summary>
public sealed record LinkedInUpdateImageFile(byte[] Content, string ContentType, string Alt);

/// <summary>A standalone post: who posts it (a member or a page), the text, and its images in order.</summary>
public sealed record LinkedInUpdateRequest(string AuthorUrn, string Commentary, IReadOnlyList<LinkedInUpdateImageFile> Images);

/// <summary>Sends one update to LinkedIn. The real client lives in the editor host, which holds each member's
/// connection; tests use a fake. <see cref="IsConfiguredFor"/> false means nothing is sent for that author.</summary>
public interface ILinkedInUpdatePoster
{
    bool IsConfiguredFor(string authorUrn);

    Task<LinkedInSendResult> PostUpdateAsync(LinkedInUpdateRequest request, CancellationToken ct);
}

/// <summary>The default: no member app is set up, so nothing is sent.</summary>
public sealed class NotConfiguredLinkedInUpdatePoster : ILinkedInUpdatePoster
{
    public bool IsConfiguredFor(string authorUrn) => false;

    public Task<LinkedInSendResult> PostUpdateAsync(LinkedInUpdateRequest request, CancellationToken ct) =>
        Task.FromResult(new LinkedInSendResult(LinkedInSendStatus.Refused, Reason: "LinkedIn updates are not set up on this install."));
}

/// <summary>How late an update may still go out: one due more than <see cref="MaxLate"/> ago is held back as
/// missed, so a server that was down on Tuesday does not post Tuesday's text on Friday.</summary>
public sealed record LinkedInUpdateOptions
{
    public TimeSpan MaxLate { get; init; } = TimeSpan.FromHours(2);
}
