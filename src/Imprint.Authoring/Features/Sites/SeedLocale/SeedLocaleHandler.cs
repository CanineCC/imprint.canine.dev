using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Projections;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.SeedLocale;

/// <summary>
/// Seeds the site chrome and then every page, each in its own aggregate transaction.
/// </summary>
/// <remarks>
/// Chrome goes first because it is the one piece that must not be half-done: a header and
/// footer left in the wrong language are visible on every page, whereas a page that has
/// not been reached yet simply still reads in the source language, which is where it
/// started. If a later page fails, re-running finishes the job — seeding only fills gaps,
/// so the pages already done are untouched.
/// </remarks>
public sealed class SeedLocaleHandler(IAggregateStore store, PageList pageList)
    : ICommandHandler<SeedLocale>
{
    public async Task<Result> Handle(SeedLocale command, CancellationToken ct)
    {
        var target = new Locale(command.Target);
        var source = new Locale(command.Source);

        var site = await store.Load<Site>(command.SiteId.Stream, ct);
        if (!site.Locales.Contains(target))
        {
            return Result.Fail($"'{target}' is not one of this site's locales. Add it first.");
        }

        if (!site.Locales.Contains(source))
        {
            return Result.Fail($"'{source}' is not one of this site's locales.");
        }

        site.SeedChromeLocale(target, source);
        await store.Save(site, ct);

        // Per page, for the PublishAllStale reason: independent streams, and one page's
        // domain error must not decide the fate of its siblings.
        var failures = new List<string>();
        foreach (var summary in pageList.All(command.SiteId))
        {
            try
            {
                var page = await store.Load<Page>(summary.Id.Stream, ct);
                page.SeedLocale(target, source);
                await store.Save(page, ct);
            }
            catch (DomainException failure)
            {
                failures.Add($"{summary.Slug}: {failure.Message}");
            }
        }

        return failures.Count == 0 ? Result.Ok() : Result.Fail([.. failures]);
    }
}
