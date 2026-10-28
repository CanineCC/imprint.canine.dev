using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.LinkedIn;
using Imprint.Authoring.Domain.LinkedIn.Events;

namespace Imprint.Authoring.Tests.Domain.LinkedIn;

public sealed class LinkedInUpdateEventSamples : IEventSampleProvider
{
    private static readonly DateTimeOffset At = new(2026, 11, 3, 7, 15, 0, TimeSpan.Zero);

    public IEnumerable<object> Samples
    {
        get
        {
            yield return new LinkedInUpdateScheduled(LinkedInUpdateId.New(), SiteId.New(), "jimmy", "Hi", [new LinkedInUpdateImage(AssetId.New(), "A chart")], At, At);
            yield return new LinkedInUpdateEdited("jimmy", "Hi again", [], At.AddDays(1), At);
            yield return new LinkedInUpdateCancelled(At);
            yield return new LinkedInUpdateAttempting("sha", At);
            yield return new LinkedInUpdatePosted("urn:li:share:1", At);
            yield return new LinkedInUpdateFailed("rate limited", true, At, At.AddMinutes(15));
            yield return new LinkedInUpdateFailed("no permission", false, At);
            yield return new LinkedInUpdateUnknown("timed out", At);
            yield return new LinkedInUpdateMissed("late", At);
            yield return new LinkedInUpdateResolved("urn:li:share:1", At);
            yield return new LinkedInUpdateResolved(null, At);
        }
    }
}
