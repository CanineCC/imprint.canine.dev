using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Pages.RevertPageToPublished;

/// <summary>
/// Replace a page's whole content with an explicit set of section roots. This is the
/// general primitive <see cref="RevertPageToPublished"/> is built on, and it is also the
/// compensating command the editor uses as the <em>undo</em> of a revert: it captures the
/// pre-revert draft roots and restores them, so one Ctrl+Z brings the discarded work back.
/// </summary>
public sealed record RestorePageContent(PageId PageId, NodeList Roots) : ICommand;
