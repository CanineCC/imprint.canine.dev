using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Assets.TagAsset;

// Tag shape (blank, length, the per-asset limit) is validated by the aggregate with
// human messages — the same division RenameAsset draws.
public sealed record TagAsset(AssetId AssetId, string Tag) : ICommand;
