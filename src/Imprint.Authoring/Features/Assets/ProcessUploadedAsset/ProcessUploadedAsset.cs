using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Assets.ProcessUploadedAsset;

/// <summary>
/// Internal command dispatched by <see cref="AssetProcessingWorker"/>, never by the
/// editor UI. It still goes through the dispatcher: the worker is untrusted
/// infrastructure, and only the aggregate records what actually happened.
/// </summary>
public sealed record ProcessUploadedAsset(AssetId AssetId) : ICommand;
