using Imprint.Authoring.Domain.Pages;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Pages.RevertPageToPublished;

public sealed class RestorePageContentHandler(IAggregateStore store) : ICommandHandler<RestorePageContent>
{
    public async Task<Result> Handle(RestorePageContent cmd, CancellationToken ct)
    {
        var page = await store.Load<Page>(cmd.PageId.Stream, ct);
        page.RestoreContent(cmd.Roots);
        await store.Save(page, ct);
        return Result.Ok();
    }
}
