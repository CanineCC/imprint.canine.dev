using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Posts;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Features.Pages;
using Imprint.Authoring.Markdown;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Posts.PublishPost;

public sealed class PublishPostHandler(IAggregateStore store, IWidgetCatalog widgets)
    : ICommandHandler<PublishPost>
{
    public async Task<Result> Handle(PublishPost cmd, CancellationToken ct)
    {
        var post = await store.Load<Post>(cmd.PostId.Stream, ct);
        var locale = new Locale(cmd.Locale);

        // The review gate. It is a SITE policy — "this blog has someone who clears copy" — so it
        // is checked here rather than in the aggregate, the same way the widget catalog is. A site
        // with nobody named publishes exactly as it always did, which is what keeps this change
        // from reaching every other site in the estate.
        //
        // Accepted race: the reviewer could be named in the instant between this check and the
        // append, letting one post through unreviewed. The window is a single command and the
        // remedy is the Unpublish button — the alternative, a lock across two aggregates, buys
        // less than it costs.
        var site = await store.Load<Site>(post.SiteId.Stream, ct);
        if (site.HasReviewer && !post.IsApproved && !post.IsPublished)
        {
            return Result.Fail(
                $"'{site.Name}' publishes through review: {site.ReviewerName ?? site.ReviewerEmail} has to approve " +
                "this post before it can go live. Send it for review instead.");
        }

        // The widget half of "can this be published", checked HERE because the catalog is a
        // slice concern — the aggregate is manifest-blind exactly like the Page aggregate is
        // (see IWidgetCatalog). A post whose island the browser would never upgrade is not a
        // post that should go live, and the author needs to hear the tag name, not see a gap
        // on the published page.
        //
        // The aggregate re-runs the conversion for its own verdict a moment later. That is a
        // duplicate parse of one post body on an explicit user action, and worth it: the rule
        // "a published post converts" then holds even for a caller that never came through
        // this slice.
        var markdown = post.Body.Get(locale);
        if (!string.IsNullOrWhiteSpace(markdown))
        {
            var conversion = MarkdownToNodes.Convert(markdown, locale);
            foreach (var node in conversion.Nodes)
            {
                if (widgets.CheckWidgets(node) is { Succeeded: false } failure)
                {
                    return failure;
                }
            }
        }

        // The system clock, read once here: the aggregate takes the instant as a parameter so
        // its own rules (first date wins) stay unit-testable without a clock abstraction.
        post.Publish(locale, TimeProvider.System.GetUtcNow());
        await store.Save(post, ct);
        return Result.Ok();
    }
}
