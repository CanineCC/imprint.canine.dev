using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Assets;
using Imprint.Authoring.Domain.Assets.Events;
using Imprint.TestKit;

namespace Imprint.Authoring.Tests.Domain.Assets;

/// <summary>
/// The optional dark-mode variant's own state machine: it may only attach to a
/// processed Image/Vector, its kind must match the base, it processes exactly once, and
/// a failure or removal drops it back to neutral — all enforced by the aggregate,
/// because the worker that drives it is untrusted.
/// </summary>
public sealed class AssetDarkVariantTests
{
    private static readonly AssetId Id = AssetId.New();

    private static readonly IReadOnlyList<ImageVariant> LightVariants =
    [
        new ImageVariant(480, 320, "variants/logo-480.webp", 10_000),
        new ImageVariant(960, 640, "variants/logo-960.webp", 30_000),
    ];

    private static readonly IReadOnlyList<ImageVariant> DarkVariants =
    [
        new ImageVariant(480, 320, "variants/logo-dark-480.webp", 9_000),
        new ImageVariant(960, 640, "variants/logo-dark-960.webp", 28_000),
    ];

    private static readonly AssetUploaded UploadedImage =
        new(Id, "logo.png", "image/png", AssetKind.Image, 20_000, "originals/logo.png");

    private static readonly AssetUploaded UploadedVector =
        new(Id, "logo.svg", "image/svg+xml", AssetKind.Vector, 4_096, "originals/logo.svg");

    private static readonly AssetUploaded UploadedVideo =
        new(Id, "intro.mp4", "video/mp4", AssetKind.Video, 9_000_000, "originals/intro.mp4");

    private static readonly AssetUploaded UploadedFile =
        new(Id, "brochure.pdf", "application/pdf", AssetKind.File, 50_000, "originals/brochure.pdf");

    private static readonly ImageVariantsGenerated LightGenerated = new(LightVariants);
    private static readonly SvgSanitized LightSvg = new("derived/logo-clean.svg", 2);
    private static readonly DarkVariantUploaded DarkUploadedImage = new("originals/logo-dark.png", "image/png");
    private static readonly DarkVariantUploaded DarkUploadedVector = new("originals/logo-dark.svg", "image/svg+xml");
    private static readonly DarkImageVariantsGenerated DarkGenerated = new(DarkVariants);
    private static readonly DarkSvgSanitized DarkSvg = new("derived/logo-dark-clean.svg", 1);

    // Base states the dark pipeline layers onto.
    private static readonly object[] ReadyImage = [UploadedImage, LightGenerated];
    private static readonly object[] ReadyVector = [UploadedVector, LightSvg];

    // ------------------------------------------------------------ upload variant

    [Fact]
    public void UploadDarkVariant_on_ready_image_enters_pending()
    {
        var outcome = AggregateSpec.For<Asset>()
            .Given(ReadyImage)
            .When(a => a.UploadDarkVariant(AssetKind.Image, "originals/logo-dark.png", "image/png"));

        outcome.ThenRaised(new DarkVariantUploaded("originals/logo-dark.png", "image/png"));
        Assert.Equal(DarkVariantStatus.Pending, outcome.Aggregate.DarkStatus);
        Assert.Equal("originals/logo-dark.png", outcome.Aggregate.DarkOriginalStorageKey);
        Assert.False(outcome.Aggregate.HasDarkVariant); // Pending is not yet usable.
    }

    [Fact]
    public void UploadDarkVariant_on_ready_vector_enters_pending()
    {
        var outcome = AggregateSpec.For<Asset>()
            .Given(ReadyVector)
            .When(a => a.UploadDarkVariant(AssetKind.Vector, "originals/logo-dark.svg", "image/svg+xml"));

        outcome.ThenRaised(new DarkVariantUploaded("originals/logo-dark.svg", "image/svg+xml"));
        Assert.Equal(DarkVariantStatus.Pending, outcome.Aggregate.DarkStatus);
    }

    [Fact]
    public void UploadDarkVariant_on_ready_degraded_base_is_accepted() =>
        // A degraded base is still a usable rendition — a dark variant may layer onto it.
        AggregateSpec.For<Asset>()
            .Given(UploadedImage, new ProcessingSkipped("original served as-is"))
            .When(a => a.UploadDarkVariant(AssetKind.Image, "originals/logo-dark.png", "image/png"))
            .ThenRaised(new DarkVariantUploaded("originals/logo-dark.png", "image/png"));

    [Fact]
    public void UploadDarkVariant_before_the_base_is_processed_is_rejected() =>
        AggregateSpec.For<Asset>()
            .Given(UploadedImage) // still Pending
            .When(a => a.UploadDarkVariant(AssetKind.Image, "originals/logo-dark.png", "image/png"))
            .ThenFails("not ready yet");

    [Fact]
    public void UploadDarkVariant_on_a_video_is_rejected() =>
        AggregateSpec.For<Asset>()
            .Given(UploadedVideo, new VideoTranscoded("derived/intro.webm", 5_000_000))
            .When(a => a.UploadDarkVariant(AssetKind.Video, "originals/intro-dark.mp4", "video/mp4"))
            .ThenFails("only images and SVGs");

    [Fact]
    public void UploadDarkVariant_on_a_file_is_rejected() =>
        AggregateSpec.For<Asset>()
            .Given(UploadedFile) // File is Ready on upload
            .When(a => a.UploadDarkVariant(AssetKind.File, "originals/brochure-dark.pdf", "application/pdf"))
            .ThenFails("only images and SVGs");

    [Fact]
    public void UploadDarkVariant_with_a_mismatched_kind_is_rejected() =>
        // A dark SVG cannot back a raster image (and vice-versa).
        AggregateSpec.For<Asset>()
            .Given(ReadyImage)
            .When(a => a.UploadDarkVariant(AssetKind.Vector, "originals/logo-dark.svg", "image/svg+xml"))
            .ThenFails("same kind");

    [Fact]
    public void UploadDarkVariant_replacing_an_existing_variant_re_enters_pending()
    {
        var outcome = AggregateSpec.For<Asset>()
            .Given(UploadedImage, LightGenerated, DarkUploadedImage, DarkGenerated) // dark already Ready
            .When(a => a.UploadDarkVariant(AssetKind.Image, "originals/logo-dark-v2.png", "image/png"));

        outcome.ThenRaised(new DarkVariantUploaded("originals/logo-dark-v2.png", "image/png"));
        Assert.Equal(DarkVariantStatus.Pending, outcome.Aggregate.DarkStatus);
        Assert.Equal("originals/logo-dark-v2.png", outcome.Aggregate.DarkOriginalStorageKey);
        Assert.Empty(outcome.Aggregate.DarkVariants); // prior derivatives discarded
        Assert.False(outcome.Aggregate.HasDarkVariant);
    }

    [Fact]
    public void UploadDarkVariant_on_a_deleted_asset_is_rejected() =>
        AggregateSpec.For<Asset>()
            .Given(UploadedImage, LightGenerated, new AssetDeleted())
            .When(a => a.UploadDarkVariant(AssetKind.Image, "originals/logo-dark.png", "image/png"))
            .ThenFails("deleted");

    // --------------------------------------------------- complete dark image variants

    [Fact]
    public void CompleteDarkImageVariants_pending_image_becomes_ready()
    {
        var outcome = AggregateSpec.For<Asset>()
            .Given(UploadedImage, LightGenerated, DarkUploadedImage)
            .When(a => a.CompleteDarkImageVariants(DarkVariants));

        outcome.ThenRaised(new DarkImageVariantsGenerated(DarkVariants));
        Assert.Equal(DarkVariantStatus.Ready, outcome.Aggregate.DarkStatus);
        Assert.Equal(DarkVariants, outcome.Aggregate.DarkVariants);
        Assert.True(outcome.Aggregate.HasDarkVariant);
    }

    [Fact]
    public void CompleteDarkImageVariants_with_no_variants_is_rejected() =>
        AggregateSpec.For<Asset>()
            .Given(UploadedImage, LightGenerated, DarkUploadedImage)
            .When(a => a.CompleteDarkImageVariants([]))
            .ThenFails("at least one variant");

    [Fact]
    public void CompleteDarkImageVariants_on_a_vector_dark_variant_is_rejected() =>
        AggregateSpec.For<Asset>()
            .Given(UploadedVector, LightSvg, DarkUploadedVector)
            .When(a => a.CompleteDarkImageVariants(DarkVariants))
            .ThenFails("not Image");

    [Fact]
    public void CompleteDarkImageVariants_without_a_pending_variant_is_rejected() =>
        AggregateSpec.For<Asset>()
            .Given(ReadyImage) // no dark variant at all
            .When(a => a.CompleteDarkImageVariants(DarkVariants))
            .ThenFails("no dark variant awaiting processing");

    [Fact]
    public void CompleteDarkImageVariants_when_dark_already_ready_is_rejected() =>
        AggregateSpec.For<Asset>()
            .Given(UploadedImage, LightGenerated, DarkUploadedImage, DarkGenerated)
            .When(a => a.CompleteDarkImageVariants(DarkVariants))
            .ThenFails("no dark variant awaiting processing");

    // ------------------------------------------------------------- complete dark svg

    [Fact]
    public void CompleteDarkSvg_pending_vector_becomes_ready()
    {
        var outcome = AggregateSpec.For<Asset>()
            .Given(UploadedVector, LightSvg, DarkUploadedVector)
            .When(a => a.CompleteDarkSvg("derived/logo-dark-clean.svg", 1));

        outcome.ThenRaised(new DarkSvgSanitized("derived/logo-dark-clean.svg", 1));
        Assert.Equal(DarkVariantStatus.Ready, outcome.Aggregate.DarkStatus);
        Assert.Equal("derived/logo-dark-clean.svg", outcome.Aggregate.DarkDerivedStorageKey);
        Assert.True(outcome.Aggregate.HasDarkVariant);
    }

    [Fact]
    public void CompleteDarkSvg_with_negative_removed_nodes_is_rejected() =>
        AggregateSpec.For<Asset>()
            .Given(UploadedVector, LightSvg, DarkUploadedVector)
            .When(a => a.CompleteDarkSvg("derived/logo-dark-clean.svg", -1))
            .ThenFails("negative");

    [Fact]
    public void CompleteDarkSvg_on_an_image_dark_variant_is_rejected() =>
        AggregateSpec.For<Asset>()
            .Given(UploadedImage, LightGenerated, DarkUploadedImage)
            .When(a => a.CompleteDarkSvg("derived/logo-dark-clean.svg", 1))
            .ThenFails("not Vector");

    [Fact]
    public void CompleteDarkSvg_without_a_pending_variant_is_rejected() =>
        AggregateSpec.For<Asset>()
            .Given(ReadyVector)
            .When(a => a.CompleteDarkSvg("derived/logo-dark-clean.svg", 1))
            .ThenFails("no dark variant awaiting processing");

    // ------------------------------------------------------------------- fail variant

    [Fact]
    public void FailDarkVariant_pending_drops_the_variant_and_reverts_to_neutral()
    {
        var outcome = AggregateSpec.For<Asset>()
            .Given(UploadedImage, LightGenerated, DarkUploadedImage)
            .When(a => a.FailDarkVariant("The dark file is not a decodable image."));

        outcome.ThenRaised(new DarkVariantFailed("The dark file is not a decodable image."));
        Assert.Equal(DarkVariantStatus.None, outcome.Aggregate.DarkStatus);
        Assert.Null(outcome.Aggregate.DarkOriginalStorageKey);
        Assert.False(outcome.Aggregate.HasDarkVariant);
        // The base asset is untouched: still a usable, neutral image.
        Assert.Equal(AssetStatus.Ready, outcome.Aggregate.Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void FailDarkVariant_without_reason_is_rejected(string reason) =>
        AggregateSpec.For<Asset>()
            .Given(UploadedImage, LightGenerated, DarkUploadedImage)
            .When(a => a.FailDarkVariant(reason))
            .ThenFails("reason");

    [Fact]
    public void FailDarkVariant_without_a_pending_variant_is_rejected() =>
        AggregateSpec.For<Asset>()
            .Given(ReadyImage)
            .When(a => a.FailDarkVariant("nothing to fail"))
            .ThenFails("no dark variant awaiting processing");

    [Fact]
    public void FailDarkVariant_when_dark_already_ready_is_rejected() =>
        AggregateSpec.For<Asset>()
            .Given(UploadedImage, LightGenerated, DarkUploadedImage, DarkGenerated)
            .When(a => a.FailDarkVariant("too late"))
            .ThenFails("no dark variant awaiting processing");

    // ----------------------------------------------------------------- remove variant

    [Fact]
    public void RemoveDarkVariant_when_ready_reverts_to_neutral()
    {
        var outcome = AggregateSpec.For<Asset>()
            .Given(UploadedImage, LightGenerated, DarkUploadedImage, DarkGenerated)
            .When(a => a.RemoveDarkVariant());

        outcome.ThenRaised(new DarkVariantRemoved());
        Assert.Equal(DarkVariantStatus.None, outcome.Aggregate.DarkStatus);
        Assert.Null(outcome.Aggregate.DarkOriginalStorageKey);
        Assert.Empty(outcome.Aggregate.DarkVariants);
        Assert.False(outcome.Aggregate.HasDarkVariant);
    }

    [Fact]
    public void RemoveDarkVariant_while_still_pending_is_accepted() =>
        AggregateSpec.For<Asset>()
            .Given(UploadedImage, LightGenerated, DarkUploadedImage)
            .When(a => a.RemoveDarkVariant())
            .ThenRaised(new DarkVariantRemoved());

    [Fact]
    public void RemoveDarkVariant_when_none_exists_is_rejected() =>
        AggregateSpec.For<Asset>()
            .Given(ReadyImage)
            .When(a => a.RemoveDarkVariant())
            .ThenFails("no dark-mode version to remove");

    [Fact]
    public void RemoveDarkVariant_on_a_deleted_asset_is_rejected() =>
        AggregateSpec.For<Asset>()
            .Given(UploadedImage, LightGenerated, DarkUploadedImage, DarkGenerated, new AssetDeleted())
            .When(a => a.RemoveDarkVariant())
            .ThenFails("deleted");

    // ------------------------------------------------------------------ deleted guards

    [Fact]
    public void CompleteDarkImageVariants_on_a_deleted_asset_is_rejected() =>
        AggregateSpec.For<Asset>()
            .Given(UploadedImage, LightGenerated, DarkUploadedImage, new AssetDeleted())
            .When(a => a.CompleteDarkImageVariants(DarkVariants))
            .ThenFails("deleted");

    [Fact]
    public void CompleteDarkSvg_on_a_deleted_asset_is_rejected() =>
        AggregateSpec.For<Asset>()
            .Given(UploadedVector, LightSvg, DarkUploadedVector, new AssetDeleted())
            .When(a => a.CompleteDarkSvg("derived/logo-dark-clean.svg", 1))
            .ThenFails("deleted");

    [Fact]
    public void FailDarkVariant_on_a_deleted_asset_is_rejected() =>
        AggregateSpec.For<Asset>()
            .Given(UploadedImage, LightGenerated, DarkUploadedImage, new AssetDeleted())
            .When(a => a.FailDarkVariant("moot"))
            .ThenFails("deleted");
}
