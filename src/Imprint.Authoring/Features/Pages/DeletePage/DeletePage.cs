using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Pages.DeletePage;

public sealed record DeletePage(PageId PageId) : ICommand;
