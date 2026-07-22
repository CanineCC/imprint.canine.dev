using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.SetFavicon;

// The site's favicon (tab/bookmark icon). A null asset id clears it. Whether the asset
// exists is checked by the handler against the asset library — the aggregate only records
// the choice.
public sealed record SetFavicon(SiteId SiteId, AssetId? AssetId) : ICommand;
