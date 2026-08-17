using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Posts.Events;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Projections;

/// <summary>One post handed to a reviewer, as the notifier needs to describe it.</summary>
/// <param name="Position">The event's global position — the notifier's cursor, and the reason
/// a restart does not re-send yesterday's mail.</param>
public sealed record PostReviewRequest(
    PostId PostId,
    SiteId SiteId,
    long Position,
    DateTimeOffset At,
    DateTimeOffset? ProposedPublishAt,
    string? Note,
    string SubmittedBy);

/// <summary>
/// Submissions waiting to be told to someone. A read model rather than a call inside the
/// handler: the mail must go whoever submitted — the editor, the authoring API, a script — and
/// a handler that sends mail is a handler that fails when the mail server does.
///
/// <para>The queue is bounded and never "consumed": a consumer keeps its own cursor and asks
/// for what came after it (<see cref="Since"/>). That is what makes the replay at startup
/// harmless — the whole history folds through here, and the notifier simply starts from the
/// position the log had reached by then.</para>
/// </summary>
public sealed class PostReviewQueue : ReadModel
{
    // A cap, not a window of interest: whatever a consumer has not read within this many
    // submissions it is never going to read, and an unbounded list in a process that runs for
    // months is a leak with extra steps.
    private const int MaxRetained = 500;

    private readonly List<PostReviewRequest> _requests = [];

    /// <summary>Submissions after <paramref name="position"/>, oldest first — a consumer's next batch.</summary>
    public IReadOnlyList<PostReviewRequest> Since(long position) =>
        [.. _requests.Where(request => request.Position > position).OrderBy(request => request.Position)];

    public override void Apply(StoredEvent @event)
    {
        if (@event.Event is not PostSubmittedForReview submitted ||
            StreamIds.IdOf(@event.StreamId, "post-") is not { } guid)
        {
            return;
        }

        // The site id is not on the event — it is on the post's creation — so the notifier looks
        // it up in PostList rather than this model carrying a second copy of the same fact.
        _requests.Add(new PostReviewRequest(
            PostId.From(guid),
            SiteId: default,
            @event.GlobalPosition,
            @event.Metadata.TimestampUtc,
            submitted.ProposedPublishAt,
            submitted.Note,
            @event.Metadata.Actor));

        if (_requests.Count > MaxRetained)
        {
            _requests.RemoveRange(0, _requests.Count - MaxRetained);
        }

        NotifyChanged();
    }

    public override void Reset() => _requests.Clear();
}
