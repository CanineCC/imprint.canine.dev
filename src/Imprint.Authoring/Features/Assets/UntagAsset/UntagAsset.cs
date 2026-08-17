using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Assets.UntagAsset;

public sealed record UntagAsset(AssetId AssetId, string Tag) : ICommand;
