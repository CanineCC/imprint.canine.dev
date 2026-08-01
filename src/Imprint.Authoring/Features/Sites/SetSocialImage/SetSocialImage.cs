using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.SetSocialImage;

// The site's share card image (og:image) — what a chat app, a social platform or a model
// shows when it is handed the URL instead of the page. A null asset id clears it. Whether
// the asset exists is checked by the handler against the asset library — the aggregate
// only records the choice.
public sealed record SetSocialImage(SiteId SiteId, AssetId? AssetId) : ICommand;
