using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Assets;
using Imprint.Authoring.Domain.Assets.Events;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Tests.Domain.Assets;

public sealed class AssetUploadTests
{
    private static readonly AssetId Id = AssetId.New();

    private static Asset Upload(AssetKind kind, string fileName = "hero.png", long byteSize = 12_345) =>
        Asset.Upload(Id, fileName, "application/octet-stream", kind, byteSize, "originals/key");

    [Fact]
    public void Upload_image_raises_uploaded_and_starts_pending()
    {
        var asset = Asset.Upload(Id, "hero.png", "image/png", AssetKind.Image, 12_345, "originals/hero.png");

        var raised = Assert.Single(asset.UncommittedEvents);
        Assert.Equal(new AssetUploaded(Id, "hero.png", "image/png", AssetKind.Image, 12_345, "originals/hero.png"), raised);
        Assert.Equal(AssetStatus.Pending, asset.Status);
        Assert.Equal("originals/hero.png", asset.OriginalStorageKey);
        Assert.Equal(12_345, asset.ByteSize);
    }

    [Theory]
    [InlineData(AssetKind.Image)]
    [InlineData(AssetKind.Vector)]
    [InlineData(AssetKind.Video)]
    public void Upload_processable_kind_starts_pending(AssetKind kind) =>
        Assert.Equal(AssetStatus.Pending, Upload(kind).Status);

    [Fact]
    public void Upload_file_kind_is_ready_immediately() =>
        Assert.Equal(AssetStatus.Ready, Upload(AssetKind.File, "brochure.pdf").Status);

    [Fact]
    public void Upload_derives_name_from_file_name_sans_extension() =>
        Assert.Equal("hero", Upload(AssetKind.Image, "hero.png").Name);

    [Fact]
    public void Upload_dotfile_keeps_the_full_file_name_as_name() =>
        Assert.Equal(".gitignore", Upload(AssetKind.File, ".gitignore").Name);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Upload_blank_file_name_is_rejected(string fileName)
    {
        var ex = Assert.Throws<DomainException>(() => Upload(AssetKind.Image, fileName));
        Assert.Contains("file name", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Upload_file_name_over_200_characters_is_rejected()
    {
        var ex = Assert.Throws<DomainException>(() => Upload(AssetKind.Image, new string('a', 201)));
        Assert.Contains("200", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Upload_file_name_at_200_characters_is_accepted() =>
        Assert.Equal(AssetStatus.Pending, Upload(AssetKind.Image, new string('a', 200)).Status);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Upload_without_content_is_rejected(long byteSize)
    {
        var ex = Assert.Throws<DomainException>(() => Upload(AssetKind.Image, byteSize: byteSize));
        Assert.Contains("empty", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Upload_over_500_megabytes_is_rejected()
    {
        var ex = Assert.Throws<DomainException>(() => Upload(AssetKind.Video, byteSize: 500L * 1024 * 1024 + 1));
        Assert.Contains("500 MB", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Upload_at_exactly_500_megabytes_is_accepted() =>
        Assert.Equal(AssetStatus.Pending, Upload(AssetKind.Video, byteSize: 500L * 1024 * 1024).Status);
}
