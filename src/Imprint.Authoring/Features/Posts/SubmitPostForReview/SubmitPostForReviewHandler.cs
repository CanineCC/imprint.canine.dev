using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Posts;
using Imprint.Authoring.Domain.Sites;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Posts.SubmitPostForReview;

public sealed class SubmitPostForReviewHandler(IAggregateStore store) : ICommandHandler<SubmitPostForReview>
{
    public async Task<Result> Handle(SubmitPostForReview cmd, CancellationToken ct)
    {
        var post = await store.Load<Post>(cmd.PostId.Stream, ct);

        // Cross-aggregate check, in the slice where those belong: there is no one to review for
        // a site that has named nobody, and a post stuck Pending forever is worse than one that
        // was never submitted. Accepted race — the reviewer could be cleared in the same instant
        // this passes, which leaves a submitted post whose site has no reviewer: it still shows
        // as Pending in the editor and can be approved by anyone with access, which is exactly
        // what "no reviewer configured" means anyway.
        var site = await store.Load<Site>(post.SiteId.Stream, ct);
        if (!site.HasReviewer)
        {
            return Result.Fail(
                $"'{site.Name}' has no reviewer configured, so there is nobody to send this to. " +
                "Name one in the site's settings, or publish it yourself.");
        }

        post.SubmitForReview(new Locale(cmd.Locale), cmd.ProposedPublishAt, cmd.Note);
        await store.Save(post, ct);
        return Result.Ok();
    }
}
