using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Assets;
using Imprint.Authoring.Domain.Assets.Events;

namespace Imprint.Authoring.Tests.Domain.Assets;

public sealed class AssetEventSamples : IEventSampleProvider
{
    public IEnumerable<object> Samples =>
    [
        new AssetUploaded(
            AssetId.New(), "harbour-sunset.jpg", "image/jpeg", AssetKind.Image, 482_113, "originals/harbour-sunset.jpg"),
        new ImageVariantsGenerated(
        [
            new ImageVariant(480, 320, "variants/harbour-sunset-480.webp", 24_512),
            new ImageVariant(960, 640, "variants/harbour-sunset-960.webp", 71_204),
            new ImageVariant(1440, 960, "variants/harbour-sunset-1440.webp", 138_777),
        ]),
        new SvgSanitized("derived/logo-clean.svg", 3),
        new VideoTranscoded("derived/intro.webm", 5_242_880),
        new ProcessingFailed("The file is not a decodable image."),
        new ProcessingSkipped("ffmpeg is not installed; the original file will be published as-is."),
        new AssetAltChanged(new Locale("da-DK"), "En solnedgang over havnen"),
        new AssetRenamed("Harbour sunset"),
        new AssetDeleted(),
    ];
}
