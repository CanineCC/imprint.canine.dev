using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Pages.ChangeNodeProps;

public sealed record ChangeNodeProps(PageId PageId, Node Replacement) : ICommand;
