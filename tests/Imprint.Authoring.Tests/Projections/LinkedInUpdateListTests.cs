using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.LinkedIn;
using Imprint.Authoring.Domain.LinkedIn.Events;
using Imprint.Authoring.Projections;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Tests.Projections;

/// <summary>
/// The LinkedIn page opens on what is still to come, so the plan is not buried under what already went out. Anything
/// that needs a person (refused, unclear, missed) stays with the plan: filed under "posted" or "cancelled" it would be
/// out of sight exactly when it needs to be seen.
/// </summary>
public sealed class LinkedInUpdateListTests
{
    private static readonly SiteId Site = SiteId.New();
    private static readonly DateTimeOffset Day1 = new(2026, 11, 1, 9, 0, 0, TimeSpan.Zero);

    private readonly LinkedInUpdateList _model = new();
    private long _position;
    private readonly Dictionary<string, long> _versions = new(StringComparer.Ordinal);

    private LinkedInUpdateId Update(int day, params object[] after)
    {
        var id = LinkedInUpdateId.New();
        Apply(id, new LinkedInUpdateScheduled(id, Site, "jimmy", $"Day {day}", [], Day1.AddDays(day), Day1));
        foreach (var @event in after)
        {
            Apply(id, @event);
        }

        return id;
    }

    private void Apply(LinkedInUpdateId id, object @event)
    {
        var stream = $"linkedin-update-{id.Value:N}";
        _versions[stream] = _versions.GetValueOrDefault(stream) + 1;
        _position++;
        _model.Apply(new StoredEvent(
            _position, stream, _versions[stream], $"e{_position}", @event,
            new EventMetadata("test", Day1.AddMinutes(_position), Guid.Empty, Guid.Empty)));
    }

    [Fact]
    public void Planned_holds_what_is_to_come_and_what_needs_a_person_soonest_first()
    {
        var later = Update(9);
        var sooner = Update(2);
        var refused = Update(3, new LinkedInUpdateFailed("Refused", Retryable: false, Day1));
        var unclear = Update(4, new LinkedInUpdateUnknown("Timed out", Day1));
        var missed = Update(5, new LinkedInUpdateMissed("Too late", Day1));
        Update(1, new LinkedInUpdatePosted("urn:li:share:1", Day1));
        Update(6, new LinkedInUpdateCancelled(Day1));

        Assert.Equal([sooner, refused, unclear, missed, later], _model.PlannedForSite(Site).Select(r => r.Id));
    }

    [Fact]
    public void Posted_and_cancelled_are_newest_first()
    {
        var first = Update(1, new LinkedInUpdatePosted("urn:li:share:1", Day1));
        var second = Update(2, new LinkedInUpdatePosted("urn:li:share:2", Day1));
        var resolved = Update(3, new LinkedInUpdateUnknown("Timed out", Day1), new LinkedInUpdateResolved("urn:li:share:3", Day1));
        var dropped = Update(4, new LinkedInUpdateCancelled(Day1));
        var droppedLater = Update(7, new LinkedInUpdateCancelled(Day1));
        Update(5);

        Assert.Equal([resolved, second, first], _model.PostedForSite(Site).Select(r => r.Id));
        Assert.Equal([droppedLater, dropped], _model.CancelledForSite(Site).Select(r => r.Id));
    }

    [Fact]
    public void Another_sites_updates_are_not_shown()
    {
        var other = LinkedInUpdateId.New();
        Apply(other, new LinkedInUpdateScheduled(other, SiteId.New(), "jimmy", "Elsewhere", [], Day1, Day1));

        Assert.Empty(_model.PlannedForSite(Site));
    }
}
