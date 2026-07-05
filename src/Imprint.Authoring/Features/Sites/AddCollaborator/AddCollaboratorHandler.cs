using Imprint.Authoring.Domain.Sites;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.AddCollaborator;

public sealed class AddCollaboratorHandler(IAggregateStore store) : ICommandHandler<AddCollaborator>
{
    public async Task<Result> Handle(AddCollaborator cmd, CancellationToken ct)
    {
        var site = await store.Load<Site>(cmd.SiteId.Stream, ct);
        site.AddCollaborator(cmd.Email);
        await store.Save(site, ct);
        return Result.Ok();
    }
}
