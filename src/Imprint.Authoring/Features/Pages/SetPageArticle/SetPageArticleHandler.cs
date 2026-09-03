using Imprint.Authoring.Domain.Pages;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Pages.SetPageArticle;

public sealed class SetPageArticleHandler(IAggregateStore store)
    : ICommandHandler<SetPageArticle>
{
    public async Task<Result> Handle(SetPageArticle cmd, CancellationToken ct)
    {
        var page = await store.Load<Page>(cmd.PageId.Stream, ct);
        page.SetArticle(
            cmd.Author,
            cmd.Published is null ? null : DateOnly.ParseExact(cmd.Published, "yyyy-MM-dd"));
        await store.Save(page, ct);
        return Result.Ok();
    }
}
