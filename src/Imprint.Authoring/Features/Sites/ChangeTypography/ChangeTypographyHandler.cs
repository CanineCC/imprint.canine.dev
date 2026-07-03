using Imprint.Authoring.Domain.Sites;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.ChangeTypography;

public sealed class ChangeTypographyHandler(IAggregateStore store) : ICommandHandler<ChangeTypography>
{
    public async Task<Result> Handle(ChangeTypography cmd, CancellationToken ct)
    {
        var site = await store.Load<Site>(cmd.SiteId.Stream, ct);
        site.SetTypography(cmd.Typography);
        await store.Save(site, ct);
        return Result.Ok();
    }
}
