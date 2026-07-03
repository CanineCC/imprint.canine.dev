using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.ChangeThemeToken;

// Token membership (closed set) and CSS color syntax are aggregate invariants with
// human-readable messages — nothing shape-only is left for a command validator.
public sealed record ChangeThemeToken(SiteId SiteId, string Token, string Light, string Dark) : ICommand;
