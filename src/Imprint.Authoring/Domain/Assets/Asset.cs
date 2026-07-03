using Imprint.Authoring.Domain.Assets.Events;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Domain.Assets;

/// <summary>
/// The media lifecycle as an explicit event-sourced state machine:
/// uploaded (Pending) → variants/sanitized/transcoded (Ready) | failed (Failed) |
/// skipped (ReadyDegraded). The processing worker is untrusted infrastructure — only
/// this aggregate records truth, so every completion re-checks kind and status.
/// </summary>
public sealed class Asset : AggregateRoot
{
    // Generous for video, small enough that a runaway upload cannot fill the disk.
    private const long MaxUploadBytes = 500L * 1024 * 1024;
    private const int MaxNameLength = 200;
    private const int MaxAltLength = 500;

    public AssetId Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public AssetKind Kind { get; private set; }
    public long ByteSize { get; private set; }
    public string OriginalStorageKey { get; private set; } = string.Empty;
    public AssetStatus Status { get; private set; }
    public IReadOnlyList<ImageVariant> Variants { get; private set; } = [];

    /// <summary>Sanitized SVG or transcoded WebM; null until processing completes (and always for images/files).</summary>
    public string? DerivedStorageKey { get; private set; }

    /// <summary>Why the asset is Failed or ReadyDegraded — surfaced in the editor's asset panel.</summary>
    public string? StatusReason { get; private set; }

    /// <summary>The raw dark-mode original; null until a dark variant is uploaded (and again if it is dropped).</summary>
    public string? DarkOriginalStorageKey { get; private set; }

    /// <summary>Dark WebP derivatives (same widths as the base); empty unless a dark image variant is Ready.</summary>
    public IReadOnlyList<ImageVariant> DarkVariants { get; private set; } = [];

    /// <summary>Sanitized dark SVG; null unless a dark vector variant is Ready.</summary>
    public string? DarkDerivedStorageKey { get; private set; }

    public DarkVariantStatus DarkStatus { get; private set; }

    /// <summary>True only when a usable dark rendition exists — a Pending variant is not one yet.</summary>
    public bool HasDarkVariant => DarkStatus is DarkVariantStatus.Ready;

    public LocalizedText DefaultAlt { get; private set; } = LocalizedText.Empty;
    public bool IsDeleted { get; private set; }

    public override string StreamId => Id.Stream;

    public static Asset Upload(
        AssetId id, string fileName, string contentType, AssetKind kind, long byteSize, string storageKey)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new DomainException("An uploaded file needs a file name.");
        }

        if (fileName.Length > MaxNameLength)
        {
            throw new DomainException($"File names are limited to {MaxNameLength} characters.");
        }

        if (byteSize <= 0)
        {
            throw new DomainException("An uploaded file cannot be empty.");
        }

        if (byteSize > MaxUploadBytes)
        {
            throw new DomainException("Uploads are limited to 500 MB.");
        }

        var asset = new Asset();
        asset.Raise(new AssetUploaded(id, fileName, contentType, kind, byteSize, storageKey));
        return asset;
    }

    public void CompleteImageVariants(IReadOnlyList<ImageVariant> variants)
    {
        EnsureNotDeleted();
        EnsureKind(AssetKind.Image, "generate image variants");
        EnsurePending("generate image variants");
        if (variants.Count == 0)
        {
            throw new DomainException("Image processing must produce at least one variant.");
        }

        Raise(new ImageVariantsGenerated(variants));
    }

    public void CompleteSvgSanitize(string storageKey, int removedNodes)
    {
        EnsureNotDeleted();
        EnsureKind(AssetKind.Vector, "sanitize as SVG");
        EnsurePending("sanitize as SVG");
        if (removedNodes < 0)
        {
            throw new DomainException("The count of removed SVG nodes cannot be negative.");
        }

        Raise(new SvgSanitized(storageKey, removedNodes));
    }

    public void CompleteVideoTranscode(string storageKey, long byteSize)
    {
        EnsureNotDeleted();
        EnsureKind(AssetKind.Video, "transcode as video");
        EnsurePending("transcode as video");
        if (byteSize <= 0)
        {
            throw new DomainException("A transcoded video cannot be empty.");
        }

        Raise(new VideoTranscoded(storageKey, byteSize));
    }

    public void FailProcessing(string reason)
    {
        EnsureNotDeleted();
        EnsurePending("mark processing as failed");
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("A processing failure needs a reason — the editor shows it to the user.");
        }

        Raise(new ProcessingFailed(reason));
    }

    public void SkipProcessing(string reason)
    {
        EnsureNotDeleted();
        EnsurePending("skip processing");
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("Skipping processing needs a reason — the editor shows it to the user.");
        }

        Raise(new ProcessingSkipped(reason));
    }

    /// <summary>
    /// Attaches a dark-mode original. Only images and SVGs adapt this way (a photo is
    /// neutral, a monochrome SVG already follows <c>currentColor</c>, video is out of
    /// scope). The base must already be processed, and the dark file's kind must match
    /// the base — no dark SVG on a raster image. Re-uploading replaces the previous dark
    /// variant and re-enters <see cref="DarkVariantStatus.Pending"/>.
    /// </summary>
    public void UploadDarkVariant(AssetKind darkKind, string storageKey, string contentType)
    {
        EnsureNotDeleted();
        if (Kind is not (AssetKind.Image or AssetKind.Vector))
        {
            throw new DomainException(
                $"Cannot add a dark-mode version: '{Name}' is a {Kind} asset — only images and SVGs support one.");
        }

        // Ready or ReadyDegraded: a dark variant layers onto a usable base rendition.
        if (Status is not (AssetStatus.Ready or AssetStatus.ReadyDegraded))
        {
            throw new DomainException(
                $"Cannot add a dark-mode version: '{Name}' is not ready yet (it is {Status}).");
        }

        if (darkKind != Kind)
        {
            throw new DomainException(
                $"The dark-mode file must be the same kind as the asset: '{Name}' is {Kind}, not {darkKind}.");
        }

        Raise(new DarkVariantUploaded(storageKey, contentType));
    }

    public void CompleteDarkImageVariants(IReadOnlyList<ImageVariant> variants)
    {
        EnsureNotDeleted();
        EnsureKind(AssetKind.Image, "generate dark image variants");
        EnsureDarkPending("generate dark image variants");
        if (variants.Count == 0)
        {
            throw new DomainException("Dark image processing must produce at least one variant.");
        }

        Raise(new DarkImageVariantsGenerated(variants));
    }

    public void CompleteDarkSvg(string storageKey, int removedNodes)
    {
        EnsureNotDeleted();
        EnsureKind(AssetKind.Vector, "sanitize the dark SVG");
        EnsureDarkPending("sanitize the dark SVG");
        if (removedNodes < 0)
        {
            throw new DomainException("The count of removed SVG nodes cannot be negative.");
        }

        Raise(new DarkSvgSanitized(storageKey, removedNodes));
    }

    public void FailDarkVariant(string reason)
    {
        EnsureNotDeleted();
        EnsureDarkPending("fail the dark variant");
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("A dark-variant failure needs a reason — the editor shows it to the user.");
        }

        Raise(new DarkVariantFailed(reason));
    }

    public void RemoveDarkVariant()
    {
        EnsureNotDeleted();
        if (DarkStatus is DarkVariantStatus.None)
        {
            throw new DomainException($"'{Name}' has no dark-mode version to remove.");
        }

        Raise(new DarkVariantRemoved());
    }

    public void SetAlt(Locale locale, string alt)
    {
        EnsureNotDeleted();
        if (alt.Length > MaxAltLength)
        {
            throw new DomainException($"Alt text is limited to {MaxAltLength} characters.");
        }

        Raise(new AssetAltChanged(locale, alt));
    }

    public void Rename(string name)
    {
        EnsureNotDeleted();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("An asset needs a name.");
        }

        if (name.Length > MaxNameLength)
        {
            throw new DomainException($"Asset names are limited to {MaxNameLength} characters.");
        }

        // A no-op rename would be noise in the history and a pointless republish trigger.
        if (name == Name)
        {
            return;
        }

        Raise(new AssetRenamed(name));
    }

    public void Delete()
    {
        EnsureNotDeleted();
        // Deleting an asset that pages still reference is forbidden by the slice via
        // the AssetUsage read model, not here: a page could gain a reference in the
        // same instant the check passes. Accepted race — a broken reference renders as
        // an empty media node and is visible in the editor, never a crash.
        Raise(new AssetDeleted());
    }

    protected override void When(object @event)
    {
        switch (@event)
        {
            case AssetUploaded e:
                Id = e.AssetId;
                FileName = e.FileName;
                Name = NameFrom(e.FileName);
                ContentType = e.ContentType;
                Kind = e.Kind;
                ByteSize = e.ByteSize;
                OriginalStorageKey = e.StorageKey;
                // Plain files have no processing pipeline — publishable immediately.
                Status = e.Kind == AssetKind.File ? AssetStatus.Ready : AssetStatus.Pending;
                break;
            case ImageVariantsGenerated e:
                Variants = e.Variants;
                Status = AssetStatus.Ready;
                break;
            case SvgSanitized e:
                DerivedStorageKey = e.StorageKey;
                Status = AssetStatus.Ready;
                break;
            case VideoTranscoded e:
                DerivedStorageKey = e.StorageKey;
                Status = AssetStatus.Ready;
                break;
            case ProcessingFailed e:
                Status = AssetStatus.Failed;
                StatusReason = e.Reason;
                break;
            case ProcessingSkipped e:
                Status = AssetStatus.ReadyDegraded;
                StatusReason = e.Reason;
                break;
            case DarkVariantUploaded e:
                DarkOriginalStorageKey = e.StorageKey;
                // A replacement re-enters Pending and discards the prior derivatives.
                DarkVariants = [];
                DarkDerivedStorageKey = null;
                DarkStatus = DarkVariantStatus.Pending;
                break;
            case DarkImageVariantsGenerated e:
                DarkVariants = e.Variants;
                DarkStatus = DarkVariantStatus.Ready;
                break;
            case DarkSvgSanitized e:
                DarkDerivedStorageKey = e.StorageKey;
                DarkStatus = DarkVariantStatus.Ready;
                break;
            case DarkVariantFailed:
            case DarkVariantRemoved:
                // Both drop the variant: the asset reverts to neutral, one file, both schemes.
                DarkOriginalStorageKey = null;
                DarkVariants = [];
                DarkDerivedStorageKey = null;
                DarkStatus = DarkVariantStatus.None;
                break;
            case AssetAltChanged e:
                DefaultAlt = DefaultAlt.With(e.Locale, e.Alt);
                break;
            case AssetRenamed e:
                Name = e.Name;
                break;
            case AssetDeleted:
                IsDeleted = true;
                break;
            default:
                throw new InvalidOperationException($"Asset cannot fold unknown event {@event.GetType().Name}.");
        }
    }

    // Editors see a friendly name; the extension is noise once the kind is known.
    // Dotfiles ('.gitignore') have no stem — fall back to the full file name.
    private static string NameFrom(string fileName)
    {
        var stem = Path.GetFileNameWithoutExtension(fileName);
        return stem.Length > 0 ? stem : fileName;
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
        {
            throw new DomainException($"'{Name}' has been deleted.");
        }
    }

    private void EnsureKind(AssetKind required, string action)
    {
        if (Kind != required)
        {
            throw new DomainException($"Cannot {action}: '{Name}' is a {Kind} asset, not {required}.");
        }
    }

    private void EnsurePending(string action)
    {
        if (Status != AssetStatus.Pending)
        {
            throw new DomainException($"Cannot {action}: '{Name}' is not waiting for processing (it is {Status}).");
        }
    }

    private void EnsureDarkPending(string action)
    {
        if (DarkStatus != DarkVariantStatus.Pending)
        {
            throw new DomainException(
                $"Cannot {action}: '{Name}' has no dark variant awaiting processing (it is {DarkStatus}).");
        }
    }
}
