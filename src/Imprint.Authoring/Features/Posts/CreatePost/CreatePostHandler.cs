using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Posts;
using Imprint.Authoring.Projections;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Posts.CreatePost;

public sealed class CreatePostHandler(IAggregateStore store, SiteOverview sites, PostList posts)
    : ICommandHandler<CreatePost>
{
    public async Task<Result> Handle(CreatePost cmd, CancellationToken ct)
    {
        _ = Slug.TryCreate(cmd.Slug, out var slug, out _);   // shape guaranteed by Validate()
        var locale = new Locale(cmd.Locale);

        var site = sites.Get(cmd.SiteId);
        if (site is null)
        {
            return Result.Fail("The site no longer exists.");
        }

        // A resource center's index and template are driven by the kind, so an article without one
        // would have nowhere to be listed and no header to be shown with.
        if (site.Kind is Domain.Sites.SiteKind.ResourceCenter && cmd.Kind is null)
        {
            return Result.Fail("An article on a resource center needs a kind: essay, guide, whitepaper or documentation.");
        }

        if (!site.Locales.Contains(locale))
        {
            return Result.Fail($"'{locale}' is not one of this site's locales.");
        }

        // Uniqueness against the post list only. Accepted race, and a deliberately NARROW
        // check: a post and a page may share a slug because posts are served under their own
        // prefix, so the two namespaces cannot collide (see PostPath).
        if (posts.SlugTaken(cmd.SiteId, slug))
        {
            return Result.Fail($"The slug '{slug}' is already used by another post on this site.");
        }

        var post = Post.Create(cmd.PostId, cmd.SiteId, slug, locale, cmd.Title);
        if (cmd.Kind is not null)
        {
            // Classified before the first save, so a refused kind leaves no post behind.
            post.Classify(cmd.Kind, cmd.Products ?? []);
        }

        await store.Save(post, ct);
        return Result.Ok();
    }
}
