using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Posts;
using Imprint.Authoring.Projections;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Posts.SetPostLinkedIn;

/// <summary>Set (or clear, with blank text) the LinkedIn post that goes out with an article.</summary>
public sealed record SetPostLinkedIn(PostId PostId, string? Text, string? Sender) : ICommand;

public sealed class SetPostLinkedInHandler(IAggregateStore store, PostList posts, SiteOverview sites) : ICommandHandler<SetPostLinkedIn>
{
    public async Task<Result> Handle(SetPostLinkedIn cmd, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(cmd.Text))
        {
            var site = posts.Get(cmd.PostId) is { } summary ? sites.Get(summary.SiteId) : null;
            var key = (cmd.Sender ?? "").Trim().ToLowerInvariant();
            if (site is null || site.LinkedInSenders.All(s => s.Key != key))
            {
                var known = site is null || site.LinkedInSenders.Count == 0
                    ? "none is set up on this site yet"
                    : "use one of: " + string.Join(", ", site.LinkedInSenders.Select(s => s.Key));
                return Result.Fail($"'{cmd.Sender}' is not a LinkedIn sender of this site ({known}).");
            }
        }

        var post = await store.Load<Post>(cmd.PostId.Stream, ct);
        post.SetLinkedIn(cmd.Text, cmd.Sender);
        await store.Save(post, ct);
        return Result.Ok();
    }
}
