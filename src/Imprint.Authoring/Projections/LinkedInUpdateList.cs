using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.LinkedIn;
using Imprint.Authoring.Domain.LinkedIn.Events;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Projections;

/// <summary>One LinkedIn update as the editor, the MCP tools and the scheduler see it. The first comment's fields
/// are null when the update has none.</summary>
public sealed record LinkedInUpdateRow(
    LinkedInUpdateId Id, SiteId SiteId, string Sender, string Text, IReadOnlyList<LinkedInUpdateImage> Images,
    DateTimeOffset PublishAt, LinkedInUpdateState State,
    string? Urn = null, string? Reason = null, bool Retryable = false, DateTimeOffset? RetryAfter = null,
    DateTimeOffset? PostedAt = null, string? FirstComment = null, LinkedInUpdateCommentState? CommentState = null,
    string? CommentUrn = null, string? CommentReason = null, bool CommentRetryable = false, DateTimeOffset? CommentRetryAfter = null)
{
    /// <summary>The same rule as <see cref="LinkedInUpdate.MaySendAt"/>.</summary>
    public bool MaySendAt(DateTimeOffset now) => State switch
    {
        LinkedInUpdateState.Scheduled => PublishAt <= now,
        LinkedInUpdateState.Failed => Retryable && (RetryAfter is null || RetryAfter <= now),
        _ => false,
    };

    /// <summary>The same rule as <see cref="LinkedInUpdate.MayCommentAt"/>.</summary>
    public bool MayCommentAt(DateTimeOffset now) =>
        State is LinkedInUpdateState.Posted && Urn is { Length: > 0 } && CommentState switch
        {
            LinkedInUpdateCommentState.Waiting => true,
            LinkedInUpdateCommentState.Failed => CommentRetryable && (CommentRetryAfter is null || CommentRetryAfter <= now),
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

    /// <summary>
    /// What is still to come, soonest first. A refused, unclear or missed update stays here, not with the posted or
    /// cancelled ones: it waits for a person, and filed away it would be out of sight exactly when it needs to be seen.
    /// </summary>
    public IReadOnlyList<LinkedInUpdateRow> PlannedForSite(SiteId site) =>
        [.. ForSite(site).Where(r => r.State is not (LinkedInUpdateState.Posted or LinkedInUpdateState.Cancelled))];

    /// <summary>A site's posted updates, newest first.</summary>
    public IReadOnlyList<LinkedInUpdateRow> PostedForSite(SiteId site) => NewestFirst(site, LinkedInUpdateState.Posted);

    /// <summary>A site's cancelled updates, newest first.</summary>
    public IReadOnlyList<LinkedInUpdateRow> CancelledForSite(SiteId site) => NewestFirst(site, LinkedInUpdateState.Cancelled);

    private IReadOnlyList<LinkedInUpdateRow> NewestFirst(SiteId site, LinkedInUpdateState state) =>
        [.. _rows.Values.Where(r => r.SiteId == site && r.State == state).OrderByDescending(r => r.PublishAt)];

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
            LinkedInUpdateScheduled e => WithComment(
                new LinkedInUpdateRow(e.Id, e.SiteId, e.Sender, e.Text, e.Images, e.PublishAt, LinkedInUpdateState.Scheduled), e.FirstComment),
            LinkedInUpdateEdited e when row is not null => WithComment(row with
            {
                Sender = e.Sender, Text = e.Text, Images = e.Images, PublishAt = e.PublishAt,
                State = LinkedInUpdateState.Scheduled, Reason = null, Retryable = false, RetryAfter = null,
            }, e.FirstComment),
            LinkedInUpdateCancelled when row is not null => row with { State = LinkedInUpdateState.Cancelled },
            LinkedInUpdateAttempting when row is not null =>
                row with { State = LinkedInUpdateState.Attempting, Reason = null, Retryable = false, RetryAfter = null },
            LinkedInUpdatePosted e when row is not null => row with { State = LinkedInUpdateState.Posted, Urn = e.Urn, PostedAt = e.At },
            LinkedInUpdateFailed e when row is not null =>
                row with { State = LinkedInUpdateState.Failed, Reason = e.Reason, Retryable = e.Retryable, RetryAfter = e.RetryAfter },
            LinkedInUpdateUnknown e when row is not null => row with { State = LinkedInUpdateState.Unknown, Reason = e.Reason },
            LinkedInUpdateMissed e when row is not null => row with { State = LinkedInUpdateState.Missed, Reason = e.Reason },
            LinkedInUpdateResolved e when row is not null => e.Urn is null
                ? row with { State = LinkedInUpdateState.Missed, Reason = "Not posted, by a person's check: give it a new time.", PostedAt = null }
                : row with { State = LinkedInUpdateState.Posted, Urn = e.Urn, Reason = null, PostedAt = e.At },
            LinkedInUpdateCommentAttempting when row is not null => row with
            {
                CommentState = LinkedInUpdateCommentState.Attempting, CommentReason = null, CommentRetryable = false, CommentRetryAfter = null,
            },
            LinkedInUpdateCommentPosted e when row is not null => row with { CommentState = LinkedInUpdateCommentState.Posted, CommentUrn = e.Urn },
            LinkedInUpdateCommentFailed e when row is not null => row with
            {
                CommentState = LinkedInUpdateCommentState.Failed, CommentReason = e.Reason, CommentRetryable = e.Retryable, CommentRetryAfter = e.RetryAfter,
            },
            LinkedInUpdateCommentUnknown e when row is not null => row with { CommentState = LinkedInUpdateCommentState.Unknown, CommentReason = e.Reason },
            LinkedInUpdateCommentMissed e when row is not null => row with
            {
                CommentState = LinkedInUpdateCommentState.Missed, CommentReason = e.Reason, CommentRetryable = false, CommentRetryAfter = null,
            },
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

    // A new or restated first comment waits for its post; none clears the comment's fields.
    private static LinkedInUpdateRow WithComment(LinkedInUpdateRow row, string? firstComment) => row with
    {
        FirstComment = firstComment,
        CommentState = firstComment is null ? null : LinkedInUpdateCommentState.Waiting,
        CommentUrn = null, CommentReason = null, CommentRetryable = false, CommentRetryAfter = null,
    };
}
