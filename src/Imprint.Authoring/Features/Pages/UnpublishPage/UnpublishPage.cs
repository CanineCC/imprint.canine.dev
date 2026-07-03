using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Pages.UnpublishPage;

public sealed record UnpublishPage(PageId PageId) : ICommand;
