using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Assets.RemoveAssetDarkVariant;

/// <summary>Reverts an asset to neutral by dropping its optional dark-mode variant.</summary>
public sealed record RemoveAssetDarkVariant(AssetId AssetId) : ICommand;
