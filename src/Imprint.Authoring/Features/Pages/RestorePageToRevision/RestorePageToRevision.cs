using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Pages.RestorePageToRevision;

/// <summary>
/// Put a page's content back to how it stood at a given stream version.
///
/// <para><see cref="RevertPageToPublished.RevertPageToPublished"/> answers "undo my unpublished edits"
/// and can only ever reach one state — the last published snapshot. This answers the other question a
/// documentation set needs: "what did this say three revisions ago, and put it back". Without it the
/// only recoverable state for a page was the most recent publish, which is why pages that make claims
/// somebody audits were kept as HTML in git and generated into the CMS instead of authored in it.</para>
/// </summary>
public sealed record RestorePageToRevision(PageId PageId, long Version) : ICommand;
