using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Domain.Sites.Events;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.SetSiteRedirects;

/// <summary>Replace a site's redirect table (old address → where it now lives). Empty clears it.</summary>
public sealed record SetSiteRedirects(SiteId SiteId, IReadOnlyList<SiteRedirect> Redirects) : ICommand;

public sealed class SetSiteRedirectsHandler(IAggregateStore store) : ICommandHandler<SetSiteRedirects>
{
    public async Task<Result> Handle(SetSiteRedirects cmd, CancellationToken ct)
    {
        var site = await store.Load<Site>(cmd.SiteId.Stream, ct);
        site.SetRedirects(cmd.Redirects);
        await store.Save(site, ct);
        return Result.Ok();
    }
}
