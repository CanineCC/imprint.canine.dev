using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Assets;
using Imprint.Authoring.Domain.Assets.Events;
using Imprint.TestKit;

namespace Imprint.Authoring.Tests.Domain.Assets;

public sealed class AssetEditingTests
{
    private static readonly AssetId Id = AssetId.New();
    private static readonly Locale En = new("en");

    private static AssetUploaded Uploaded() =>
        new(Id, "hero.png", "image/png", AssetKind.Image, 12_345, "originals/hero.png");

    // ------------------------------------------------------------------- alt

    [Fact]
    public void SetAlt_stores_the_default_alt_for_the_locale()
    {
        var outcome = AggregateSpec.For<Asset>()
            .Given(Uploaded())
            .When(a => a.SetAlt(En, "A hero image"));

        outcome.ThenRaised(new AssetAltChanged(En, "A hero image"));
        Assert.Equal("A hero image", outcome.Aggregate.DefaultAlt.Get(En));
    }

    [Fact]
    public void SetAlt_with_empty_value_clears_the_locale()
    {
        var outcome = AggregateSpec.For<Asset>()
            .Given(Uploaded(), new AssetAltChanged(En, "A hero image"))
            .When(a => a.SetAlt(En, ""));

        outcome.ThenRaised(new AssetAltChanged(En, ""));
        Assert.False(outcome.Aggregate.DefaultAlt.Has(En));
    }

    [Fact]
    public void SetAlt_over_500_characters_is_rejected() =>
        AggregateSpec.For<Asset>()
            .Given(Uploaded())
            .When(a => a.SetAlt(En, new string('a', 501)))
            .ThenFails("500");

    [Fact]
    public void SetAlt_at_500_characters_is_accepted() =>
        AggregateSpec.For<Asset>()
            .Given(Uploaded())
            .When(a => a.SetAlt(En, new string('a', 500)))
            .ThenRaised(new AssetAltChanged(En, new string('a', 500)));

    // ---------------------------------------------------------------- rename

    [Fact]
    public void Rename_changes_the_name()
    {
        var outcome = AggregateSpec.For<Asset>()
            .Given(Uploaded())
            .When(a => a.Rename("Homepage hero"));

        outcome.ThenRaised(new AssetRenamed("Homepage hero"));
        Assert.Equal("Homepage hero", outcome.Aggregate.Name);
    }

    [Fact]
    public void Rename_to_unchanged_name_raises_nothing() =>
        AggregateSpec.For<Asset>()
            .Given(Uploaded())
            .When(a => a.Rename("hero"))
            .ThenNothing();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_to_blank_is_rejected(string name) =>
        AggregateSpec.For<Asset>()
            .Given(Uploaded())
            .When(a => a.Rename(name))
            .ThenFails("needs a name");

    [Fact]
    public void Rename_over_200_characters_is_rejected() =>
        AggregateSpec.For<Asset>()
            .Given(Uploaded())
            .When(a => a.Rename(new string('a', 201)))
            .ThenFails("200");

    // ---------------------------------------------------------------- delete

    [Fact]
    public void Delete_marks_the_asset_deleted()
    {
        var outcome = AggregateSpec.For<Asset>()
            .Given(Uploaded())
            .When(a => a.Delete());

        outcome.ThenRaised(new AssetDeleted());
        Assert.True(outcome.Aggregate.IsDeleted);
    }

    [Fact]
    public void Delete_twice_is_rejected() =>
        AggregateSpec.For<Asset>()
            .Given(Uploaded(), new AssetDeleted())
            .When(a => a.Delete())
            .ThenFails("deleted");

    [Fact]
    public void Rename_on_deleted_asset_is_rejected() =>
        AggregateSpec.For<Asset>()
            .Given(Uploaded(), new AssetDeleted())
            .When(a => a.Rename("New name"))
            .ThenFails("deleted");

    [Fact]
    public void SetAlt_on_deleted_asset_is_rejected() =>
        AggregateSpec.For<Asset>()
            .Given(Uploaded(), new AssetDeleted())
            .When(a => a.SetAlt(En, "alt"))
            .ThenFails("deleted");

    [Fact]
    public void CompleteImageVariants_on_deleted_asset_is_rejected() =>
        AggregateSpec.For<Asset>()
            .Given(Uploaded(), new AssetDeleted())
            .When(a => a.CompleteImageVariants([new ImageVariant(480, 320, "variants/hero-480.webp", 9_000)]))
            .ThenFails("deleted");

    [Fact]
    public void CompleteSvgSanitize_on_deleted_asset_is_rejected() =>
        AggregateSpec.For<Asset>()
            .Given(new AssetUploaded(Id, "logo.svg", "image/svg+xml", AssetKind.Vector, 4_096, "originals/logo.svg"),
                new AssetDeleted())
            .When(a => a.CompleteSvgSanitize("derived/logo.svg", 0))
            .ThenFails("deleted");

    [Fact]
    public void CompleteVideoTranscode_on_deleted_asset_is_rejected() =>
        AggregateSpec.For<Asset>()
            .Given(new AssetUploaded(Id, "intro.mp4", "video/mp4", AssetKind.Video, 9_000_000, "originals/intro.mp4"),
                new AssetDeleted())
            .When(a => a.CompleteVideoTranscode("derived/intro.webm", 5_000_000))
            .ThenFails("deleted");

    [Fact]
    public void FailProcessing_on_deleted_asset_is_rejected() =>
        AggregateSpec.For<Asset>()
            .Given(Uploaded(), new AssetDeleted())
            .When(a => a.FailProcessing("broken"))
            .ThenFails("deleted");

    [Fact]
    public void SkipProcessing_on_deleted_asset_is_rejected() =>
        AggregateSpec.For<Asset>()
            .Given(Uploaded(), new AssetDeleted())
            .When(a => a.SkipProcessing("skipped"))
            .ThenFails("deleted");
}
