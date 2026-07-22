using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Pages.RevertPageToPublished;

/// <summary>
/// Discard every unpublished change to a page and restore its content to the last
/// PUBLISHED version. The published node tree comes from the <c>PublishedContent</c>
/// projection; the slice only names the page, so the editor never has to ship the tree.
/// </summary>
public sealed record RevertPageToPublished(PageId PageId) : ICommand;
