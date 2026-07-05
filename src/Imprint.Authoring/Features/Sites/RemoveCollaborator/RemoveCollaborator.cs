using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.RemoveCollaborator;

// Whether the email actually is a collaborator is validated by the aggregate, whose
// messages are already written for humans — no command-side duplicate.
public sealed record RemoveCollaborator(SiteId SiteId, string Email) : ICommand;
