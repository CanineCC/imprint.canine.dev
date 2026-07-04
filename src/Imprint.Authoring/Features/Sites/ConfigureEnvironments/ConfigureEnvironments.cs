using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.ConfigureEnvironments;

// The full ordered list of deploy targets travels in one command — the settings gear
// edits them as a unit, mirroring the site.environments-changed event. Name/path shape
// and uniqueness are validated by the aggregate, whose messages are written for humans.
public sealed record ConfigureEnvironments(SiteId SiteId, IReadOnlyList<DeployEnvironment> Environments) : ICommand;
