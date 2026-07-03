using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Pages.PublishPage;

public sealed record PublishPage(PageId PageId) : ICommand;
