using Imprint.Authoring.Domain.Pages;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Pages.UnpublishPage;

public sealed class UnpublishPageHandler(IAggregateStore store) : ICommandHandler<UnpublishPage>
{
    public async Task<Result> Handle(UnpublishPage cmd, CancellationToken ct)
    {
        var page = await store.Load<Page>(cmd.PageId.Stream, ct);
        page.Unpublish();
        await store.Save(page, ct);
        return Result.Ok();
    }
}
