using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Assets;
using Imprint.Authoring.Domain.Assets.Events;
using Imprint.TestKit;

namespace Imprint.Authoring.Tests.Domain.Assets;

/// <summary>
/// The full status-machine matrix: every completion against every wrong kind and
/// every wrong status — the worker is untrusted, so the aggregate must reject all of it.
/// </summary>
public sealed class AssetProcessingTests
{
    private static readonly AssetId Id = AssetId.New();

    private static readonly IReadOnlyList<ImageVariant> SomeVariants =
    [
        new ImageVariant(480, 320, "variants/photo-480.webp", 20_000),
        new ImageVariant(960, 640, "variants/photo-960.webp", 58_000),
    ];

    private static AssetUploaded Uploaded(AssetKind kind) => kind switch
    {
        AssetKind.Image => new(Id, "photo.jpg", "image/jpeg", kind, 100_000, "originals/photo.jpg"),
        AssetKind.Vector => new(Id, "logo.svg", "image/svg+xml", kind, 4_096, "originals/logo.svg"),
        AssetKind.Video => new(Id, "intro.mp4", "video/mp4", kind, 9_000_000, "originals/intro.mp4"),
        _ => new(Id, "brochure.pdf", "application/pdf", kind, 50_000, "originals/brochure.pdf"),
    };

    // ------------------------------------------------------------ image variants

    [Fact]
    public void CompleteImageVariants_pending_image_becomes_ready()
    {
        var outcome = AggregateSpec.For<Asset>()
            .Given(Uploaded(AssetKind.Image))
            .When(a => a.CompleteImageVariants(SomeVariants));

        outcome.ThenRaised(new ImageVariantsGenerated(SomeVariants));
        Assert.Equal(AssetStatus.Ready, outcome.Aggregate.Status);
        Assert.Equal(SomeVariants, outcome.Aggregate.Variants);
    }

    [Fact]
    public void CompleteImageVariants_with_no_variants_is_rejected() =>
        AggregateSpec.For<Asset>()
            .Given(Uploaded(AssetKind.Image))
            .When(a => a.CompleteImageVariants([]))
            .ThenFails("at least one variant");

    [Theory]
    [InlineData(AssetKind.Vector)]
    [InlineData(AssetKind.Video)]
    [InlineData(AssetKind.File)]
    public void CompleteImageVariants_on_wrong_kind_is_rejected(AssetKind kind) =>
        AggregateSpec.For<Asset>()
            .Given(Uploaded(kind))
            .When(a => a.CompleteImageVariants(SomeVariants))
            .ThenFails("not Image");

    [Fact]
    public void CompleteImageVariants_when_already_ready_is_rejected() =>
        AggregateSpec.For<Asset>()
            .Given(Uploaded(AssetKind.Image), new ImageVariantsGenerated(SomeVariants))
            .When(a => a.CompleteImageVariants(SomeVariants))
            .ThenFails("not waiting for processing");

    [Fact]
    public void CompleteImageVariants_after_failure_is_rejected() =>
        AggregateSpec.For<Asset>()
            .Given(Uploaded(AssetKind.Image), new ProcessingFailed("corrupt file"))
            .When(a => a.CompleteImageVariants(SomeVariants))
            .ThenFails("not waiting for processing");

    [Fact]
    public void CompleteImageVariants_after_skip_is_rejected() =>
        AggregateSpec.For<Asset>()
            .Given(Uploaded(AssetKind.Image), new ProcessingSkipped("codec missing"))
            .When(a => a.CompleteImageVariants(SomeVariants))
            .ThenFails("not waiting for processing");

    // ------------------------------------------------------------- svg sanitize

    [Fact]
    public void CompleteSvgSanitize_pending_vector_becomes_ready()
    {
        var outcome = AggregateSpec.For<Asset>()
            .Given(Uploaded(AssetKind.Vector))
            .When(a => a.CompleteSvgSanitize("derived/logo-clean.svg", 2));

        outcome.ThenRaised(new SvgSanitized("derived/logo-clean.svg", 2));
        Assert.Equal(AssetStatus.Ready, outcome.Aggregate.Status);
        Assert.Equal("derived/logo-clean.svg", outcome.Aggregate.DerivedStorageKey);
    }

    [Fact]
    public void CompleteSvgSanitize_with_zero_removed_nodes_is_accepted() =>
        AggregateSpec.For<Asset>()
            .Given(Uploaded(AssetKind.Vector))
            .When(a => a.CompleteSvgSanitize("derived/logo-clean.svg", 0))
            .ThenRaised(new SvgSanitized("derived/logo-clean.svg", 0));

    [Fact]
    public void CompleteSvgSanitize_with_negative_removed_nodes_is_rejected() =>
        AggregateSpec.For<Asset>()
            .Given(Uploaded(AssetKind.Vector))
            .When(a => a.CompleteSvgSanitize("derived/logo-clean.svg", -1))
            .ThenFails("negative");

    [Theory]
    [InlineData(AssetKind.Image)]
    [InlineData(AssetKind.Video)]
    [InlineData(AssetKind.File)]
    public void CompleteSvgSanitize_on_wrong_kind_is_rejected(AssetKind kind) =>
        AggregateSpec.For<Asset>()
            .Given(Uploaded(kind))
            .When(a => a.CompleteSvgSanitize("derived/logo-clean.svg", 1))
            .ThenFails("not Vector");

    [Fact]
    public void CompleteSvgSanitize_when_already_ready_is_rejected() =>
        AggregateSpec.For<Asset>()
            .Given(Uploaded(AssetKind.Vector), new SvgSanitized("derived/logo-clean.svg", 1))
            .When(a => a.CompleteSvgSanitize("derived/logo-clean.svg", 1))
            .ThenFails("not waiting for processing");

    // ---------------------------------------------------------- video transcode

    [Fact]
    public void CompleteVideoTranscode_pending_video_becomes_ready()
    {
        var outcome = AggregateSpec.For<Asset>()
            .Given(Uploaded(AssetKind.Video))
            .When(a => a.CompleteVideoTranscode("derived/intro.webm", 5_000_000));

        outcome.ThenRaised(new VideoTranscoded("derived/intro.webm", 5_000_000));
        Assert.Equal(AssetStatus.Ready, outcome.Aggregate.Status);
        Assert.Equal("derived/intro.webm", outcome.Aggregate.DerivedStorageKey);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void CompleteVideoTranscode_without_content_is_rejected(long byteSize) =>
        AggregateSpec.For<Asset>()
            .Given(Uploaded(AssetKind.Video))
            .When(a => a.CompleteVideoTranscode("derived/intro.webm", byteSize))
            .ThenFails("empty");

    [Theory]
    [InlineData(AssetKind.Image)]
    [InlineData(AssetKind.Vector)]
    [InlineData(AssetKind.File)]
    public void CompleteVideoTranscode_on_wrong_kind_is_rejected(AssetKind kind) =>
        AggregateSpec.For<Asset>()
            .Given(Uploaded(kind))
            .When(a => a.CompleteVideoTranscode("derived/intro.webm", 5_000_000))
            .ThenFails("not Video");

    [Fact]
    public void CompleteVideoTranscode_when_already_ready_is_rejected() =>
        AggregateSpec.For<Asset>()
            .Given(Uploaded(AssetKind.Video), new VideoTranscoded("derived/intro.webm", 5_000_000))
            .When(a => a.CompleteVideoTranscode("derived/intro.webm", 5_000_000))
            .ThenFails("not waiting for processing");

    // ------------------------------------------------------------- fail / skip

    [Fact]
    public void FailProcessing_pending_asset_becomes_failed_with_reason()
    {
        var outcome = AggregateSpec.For<Asset>()
            .Given(Uploaded(AssetKind.Image))
            .When(a => a.FailProcessing("The file is not a decodable image."));

        outcome.ThenRaised(new ProcessingFailed("The file is not a decodable image."));
        Assert.Equal(AssetStatus.Failed, outcome.Aggregate.Status);
        Assert.Equal("The file is not a decodable image.", outcome.Aggregate.StatusReason);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void FailProcessing_without_reason_is_rejected(string reason) =>
        AggregateSpec.For<Asset>()
            .Given(Uploaded(AssetKind.Image))
            .When(a => a.FailProcessing(reason))
            .ThenFails("reason");

    [Fact]
    public void FailProcessing_when_already_ready_is_rejected() =>
        AggregateSpec.For<Asset>()
            .Given(Uploaded(AssetKind.Image), new ImageVariantsGenerated(SomeVariants))
            .When(a => a.FailProcessing("too late"))
            .ThenFails("not waiting for processing");

    [Fact]
    public void FailProcessing_on_file_kind_is_rejected_because_files_are_never_pending() =>
        AggregateSpec.For<Asset>()
            .Given(Uploaded(AssetKind.File))
            .When(a => a.FailProcessing("nothing to process"))
            .ThenFails("not waiting for processing");

    [Fact]
    public void SkipProcessing_pending_asset_becomes_ready_degraded_with_reason()
    {
        var outcome = AggregateSpec.For<Asset>()
            .Given(Uploaded(AssetKind.Video))
            .When(a => a.SkipProcessing("ffmpeg is not installed."));

        outcome.ThenRaised(new ProcessingSkipped("ffmpeg is not installed."));
        Assert.Equal(AssetStatus.ReadyDegraded, outcome.Aggregate.Status);
        Assert.Equal("ffmpeg is not installed.", outcome.Aggregate.StatusReason);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void SkipProcessing_without_reason_is_rejected(string reason) =>
        AggregateSpec.For<Asset>()
            .Given(Uploaded(AssetKind.Video))
            .When(a => a.SkipProcessing(reason))
            .ThenFails("reason");

    [Fact]
    public void SkipProcessing_when_already_failed_is_rejected() =>
        AggregateSpec.For<Asset>()
            .Given(Uploaded(AssetKind.Video), new ProcessingFailed("broken"))
            .When(a => a.SkipProcessing("also skipping"))
            .ThenFails("not waiting for processing");

    [Fact]
    public void SkipProcessing_when_already_skipped_is_rejected() =>
        AggregateSpec.For<Asset>()
            .Given(Uploaded(AssetKind.Video), new ProcessingSkipped("first skip"))
            .When(a => a.SkipProcessing("second skip"))
            .ThenFails("not waiting for processing");
}
