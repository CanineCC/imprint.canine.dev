using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Assets.ProcessAssetDarkVariant;

/// <summary>
/// Internal command dispatched by the asset processing worker, never by the editor UI.
/// It still goes through the dispatcher: the worker is untrusted infrastructure, and
/// only the aggregate records what actually happened to the dark variant.
/// </summary>
public sealed record ProcessAssetDarkVariant(AssetId AssetId) : ICommand;
