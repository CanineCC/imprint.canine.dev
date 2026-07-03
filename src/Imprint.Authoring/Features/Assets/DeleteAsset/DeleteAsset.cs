using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Assets.DeleteAsset;

public sealed record DeleteAsset(AssetId AssetId) : ICommand;
