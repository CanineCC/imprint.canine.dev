using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.AddCollaborator;

// Email shape, duplicates and the collaborator cap are validated by the aggregate,
// whose messages are already written for humans — no command-side duplicate.
public sealed record AddCollaborator(SiteId SiteId, string Email) : ICommand;
