using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Projections;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Pages.EditText;

public sealed class EditTextHandler(IAggregateStore store, SiteOverview sites)
    : ICommandHandler<EditText>
{
    public async Task<Result> Handle(EditText cmd, CancellationToken ct)
    {
        var locale = new Locale(cmd.Locale);
        var page = await store.Load<Page>(cmd.PageId.Stream, ct);

        // Cross-aggregate check via the SiteOverview read model. Accepted race: a
        // locale removed in this same instant might slip through — the render simply
        // ignores unknown locales (docs/architecture.md §Consistency), so the stray
        // value is dormant, not harmful. The aggregate does the rest, including the
        // CanonicalHtml grammar for 'html' fields.
        var site = sites.Get(page.SiteId);
        if (site is null || !site.Locales.Contains(locale))
        {
            return Result.Fail($"'{locale}' is not one of this site's locales.");
        }

        page.EditText(cmd.NodeId, cmd.Field, locale, cmd.Value);
        await store.Save(page, ct);
        return Result.Ok();
    }
}
