using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.CreateBlog;

public sealed class CreateBlogHandler(IAggregateStore store) : ICommandHandler<CreateBlog>
{
    /// <summary>The one environment a blog starts with when the author names a target.</summary>
    private const string ProductionEnvironment = "Production";

    public async Task<Result> Handle(CreateBlog cmd, CancellationToken ct)
    {
        var blog = Site.Create(cmd.SiteId, cmd.Name, new Locale(cmd.DefaultLocale), SiteKind.Blog);

        // Both changes are raised on ONE aggregate and saved once, so a rejected origin
        // (the aggregate validates its shape) takes the whole creation down with it. A
        // blog that exists but points nowhere, because the second half of the form threw
        // after the first half was committed, is a worse outcome than submitting twice.
        var folder = cmd.PublishFolder?.Trim();
        if (!string.IsNullOrEmpty(folder))
        {
            var origin = string.IsNullOrWhiteSpace(cmd.PublicUrl) ? null : cmd.PublicUrl;
            blog.SetEnvironments([new DeployEnvironment(ProductionEnvironment, folder, origin)]);
        }

        await store.Save(blog, ct);
        return Result.Ok();
    }
}
