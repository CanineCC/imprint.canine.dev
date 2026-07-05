using Imprint.Authoring.Domain.Sites;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.RemoveCollaborator;

public sealed class RemoveCollaboratorHandler(IAggregateStore store) : ICommandHandler<RemoveCollaborator>
{
    public async Task<Result> Handle(RemoveCollaborator cmd, CancellationToken ct)
    {
        var site = await store.Load<Site>(cmd.SiteId.Stream, ct);
        site.RemoveCollaborator(cmd.Email);
        await store.Save(site, ct);
        return Result.Ok();
    }
}
