using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Assets.RenameAsset;

// Name shape (empty, length) is validated by the aggregate with human messages.
public sealed record RenameAsset(AssetId AssetId, string Name) : ICommand;
