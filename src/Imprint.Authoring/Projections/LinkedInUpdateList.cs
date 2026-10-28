using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.LinkedIn;
using Imprint.Authoring.Domain.LinkedIn.Events;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Projections;

/// <summary>One LinkedIn update as the editor, the MCP tools and the scheduler see it.</summary>
public sealed record LinkedInUpdateRow(
    LinkedInUpdateId Id, SiteId SiteId, string Sender, string Text, IReadOnlyList<LinkedInUpdateImage> Images,
    DateTimeOffset PublishAt, LinkedInUpdateState State,
    string? Urn = null, string? Reason = null, bool Retryable = false, DateTimeOffset? RetryAfter = null)
{
    /// <summary>The same rule as <see cref="LinkedInUpdate.MaySendAt"/>.</summary>
    public bool MaySendAt(DateTimeOffset now) => State switch
    {
        LinkedInUpdateState.Scheduled => PublishAt <= now,
        LinkedInUpdateState.Failed => Retryable && (RetryAfter is null || RetryAfter <= now),
        _ => false,
    };
}

/// <summary>Every LinkedIn update (streams <c>linkedin-update-{id}</c>).</summary>
public sealed class LinkedInUpdateList : ReadModel
{
    private readonly Dictionary<LinkedInUpdateId, LinkedInUpdateRow> _rows = [];

    public LinkedInUpdateRow? Get(LinkedInUpdateId id) => _rows.GetValueOrDefault(id);

    /// <summary>A site's updates, soonest first.</summary>
    public IReadOnlyList<LinkedInUpdateRow> ForSite(SiteId site) =>
        [.. _rows.Values.Where(r => r.SiteId == site).OrderBy(r => r.PublishAt)];

    public IReadOnlyList<LinkedInUpdateRow> All() => [.. _rows.Values.OrderBy(r => r.PublishAt)];

    public override void Apply(StoredEvent @event)
    {
        if (StreamIds.IdOf(@event.StreamId, "linkedin-update-") is not { } guid)
        {
            return;
        }

        var id = LinkedInUpdateId.From(guid);
        var row = _rows.GetValueOrDefault(id);
        LinkedInUpdateRow? next = @event.Event switch
        {
            LinkedInUpdateScheduled e => new LinkedInUpdateRow(e.Id, e.SiteId, e.Sender, e.Text, e.Images, e.PublishAt, LinkedInUpdateState.Scheduled),
            LinkedInUpdateEdited e when row is not null => row with
            {
                Sender = e.Sender, Text = e.Text, Images = e.Images, PublishAt = e.PublishAt,
                State = LinkedInUpdateState.Scheduled, Reason = null, Retryable = false, RetryAfter = null,
            },
            LinkedInUpdateCancelled when row is not null => row with { State = LinkedInUpdateState.Cancelled },
            LinkedInUpdateAttempting when row is not null =>
                row with { State = LinkedInUpdateState.Attempting, Reason = null, Retryable = false, RetryAfter = null },
            LinkedInUpdatePosted e when row is not null => row with { State = LinkedInUpdateState.Posted, Urn = e.Urn },
            LinkedInUpdateFailed e when row is not null =>
                row with { State = LinkedInUpdateState.Failed, Reason = e.Reason, Retryable = e.Retryable, RetryAfter = e.RetryAfter },
            LinkedInUpdateUnknown e when row is not null => row with { State = LinkedInUpdateState.Unknown, Reason = e.Reason },
            LinkedInUpdateMissed e when row is not null => row with { State = LinkedInUpdateState.Missed, Reason = e.Reason },
            LinkedInUpdateResolved e when row is not null => e.Urn is null
                ? row with { State = LinkedInUpdateState.Missed, Reason = "Not posted, by a person's check: give it a new time." }
                : row with { State = LinkedInUpdateState.Posted, Urn = e.Urn, Reason = null },
            _ => row,
        };

        if (next is null)
        {
            return;
        }

        _rows[id] = next;
        NotifyChanged();
    }

    public override void Reset() => _rows.Clear();
}
