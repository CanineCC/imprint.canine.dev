using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.SetSiteLinkedInSenders;

/// <summary>Replace the LinkedIn senders a site's articles may post as.</summary>
public sealed record SetSiteLinkedInSenders(SiteId SiteId, IReadOnlyList<LinkedInSender> Senders) : ICommand;

public sealed class SetSiteLinkedInSendersHandler(IAggregateStore store) : ICommandHandler<SetSiteLinkedInSenders>
{
    public async Task<Result> Handle(SetSiteLinkedInSenders cmd, CancellationToken ct)
    {
        var site = await store.Load<Site>(cmd.SiteId.Stream, ct);
        site.SetLinkedInSenders(cmd.Senders);
        await store.Save(site, ct);
        return Result.Ok();
    }
}
