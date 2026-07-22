using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.SetHeaderLogo;

// The site's header logo, rendered in place of the brand dot in the published header and
// footer. A null asset id clears it. Existence is checked by the handler.
public sealed record SetHeaderLogo(SiteId SiteId, AssetId? AssetId) : ICommand;
