using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.LinkedIn.Events;

namespace Imprint.Authoring.Tests.Domain.LinkedIn;

public sealed class LinkedInPostEventSamples : IEventSampleProvider
{
    private static readonly DateTimeOffset At = new(2026, 11, 10, 8, 0, 0, TimeSpan.Zero);

    public IEnumerable<object> Samples
    {
        get
        {
            yield return new LinkedInPostAttempting(PostId.New(), SiteId.New(), "canine", "sha", "https://resourcecenter.canine.dev/x/", At);
            yield return new LinkedInPostPosted("urn:li:share:6844785523593134080", At);
            yield return new LinkedInPostFailed("rate limited", true, At, At.AddMinutes(30));
            yield return new LinkedInPostFailed("no page role", false, At);
            yield return new LinkedInPostUnknown("timed out", At);
            yield return new LinkedInPostResolved("urn:li:share:1", At);
            yield return new LinkedInPostResolved(null, At);
        }
    }
}
