using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.LinkedIn;
using Imprint.Authoring.Domain.LinkedIn.Events;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Projections;

/// <summary>One article's LinkedIn post as the editor and the scheduler see it.</summary>
public sealed record LinkedInPostRow(
    PostId PostId, SiteId SiteId, LinkedInPostState State, DateTimeOffset AttemptedAt,
    string? Urn = null, string? Reason = null, bool Retryable = false, DateTimeOffset? RetryAfter = null)
{
    /// <summary>The same rule as <see cref="LinkedInPost.MaySendAt"/>.</summary>
    public bool MaySendAt(DateTimeOffset now) => State switch
    {
        LinkedInPostState.None => true,
        LinkedInPostState.Failed => Retryable && (RetryAfter is null || RetryAfter <= now),
        _ => false,
    };
}

/// <summary>Every article's LinkedIn post (streams <c>linkedin-post-{id}</c>).</summary>
public sealed class LinkedInPosts : ReadModel
{
    private readonly Dictionary<PostId, LinkedInPostRow> _rows = [];
    private readonly List<(SiteId Site, DateTimeOffset At)> _attempts = [];

    public LinkedInPostRow? Get(PostId post) => _rows.GetValueOrDefault(post);

    /// <summary>How many attempts were made for a site since <paramref name="since"/>: what the hourly cap counts.</summary>
    public int AttemptsSince(SiteId site, DateTimeOffset since) => _attempts.Count(a => a.Site == site && a.At >= since);

    public override void Apply(StoredEvent @event)
    {
        if (StreamIds.IdOf(@event.StreamId, "linkedin-post-") is not { } guid)
        {
            return;
        }

        var post = PostId.From(guid);
        var row = _rows.GetValueOrDefault(post);
        LinkedInPostRow? next = @event.Event switch
        {
            LinkedInPostAttempting e => new LinkedInPostRow(e.PostId, e.SiteId, LinkedInPostState.Attempting, e.At, row?.Urn),
            LinkedInPostPosted e when row is not null => row with { State = LinkedInPostState.Posted, Urn = e.Urn },
            LinkedInPostFailed e when row is not null =>
                row with { State = LinkedInPostState.Failed, Reason = e.Reason, Retryable = e.Retryable, RetryAfter = e.RetryAfter },
            LinkedInPostUnknown e when row is not null => row with { State = LinkedInPostState.Unknown, Reason = e.Reason },
            LinkedInPostResolved e when row is not null =>
                row with { State = e.Urn is null ? LinkedInPostState.None : LinkedInPostState.Posted, Urn = e.Urn },
            _ => row,
        };

        if (@event.Event is LinkedInPostAttempting attempting)
        {
            _attempts.Add((attempting.SiteId, attempting.At));
        }

        if (next is null)
        {
            return;
        }

        _rows[post] = next;
        NotifyChanged();
    }

    public override void Reset()
    {
        _rows.Clear();
        _attempts.Clear();
    }
}
