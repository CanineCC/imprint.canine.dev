using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Assets.RemoveAssetDarkVariant;

/// <summary>Editor action: drop an asset's dark-mode variant so it reverts to neutral.</summary>
public sealed record RemoveAssetDarkVariant(AssetId AssetId) : ICommand;
