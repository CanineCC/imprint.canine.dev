using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Projections;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Pages.AddPreset;

public sealed class AddPresetHandler(IAggregateStore store, SiteOverview sites)
    : ICommandHandler<AddPreset>
{
    public async Task<Result> Handle(AddPreset cmd, CancellationToken ct)
    {
        var page = await store.Load<Page>(cmd.PageId.Stream, ct);

        // The preset's starter text lands in the site's default locale, read from the
        // SiteOverview read model. Accepted race: a default-locale change in this same
        // instant puts the text in the previous default — still present, still
        // editable, and resolved by LocalizedText fallback at render.
        var site = sites.Get(page.SiteId);
        if (site is null)
        {
            return Result.Fail("The site no longer exists.");
        }

        // Build mints fresh node ids on every call, so the resulting event is
        // self-contained and replay never regenerates anything.
        var section = SectionPresets.Find(cmd.PresetKey)!.Build(site.DefaultLocale);
        page.AddNode(NodeId.Root, cmd.Index, section);
        await store.Save(page, ct);
        return Result.Ok();
    }
}
