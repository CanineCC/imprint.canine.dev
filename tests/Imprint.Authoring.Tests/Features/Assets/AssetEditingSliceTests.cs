using Imprint.Authoring.Domain;
using Imprint.Authoring.Features.Assets.RenameAsset;
using Imprint.Authoring.Features.Assets.SetAssetAlt;
using Imprint.Authoring.Projections;

namespace Imprint.Authoring.Tests.Features.Assets;

public sealed class AssetEditingSliceTests
{
    [Fact]
    public async Task SetAssetAlt_happy_path_updates_the_default_alt_in_AssetLibrary()
    {
        await using var host = SliceTestHelpers.NewAssetHost();
        var assetId = AssetId.New();
        await host.Ok(SliceTestHelpers.NewUpload(assetId, "photo.jpg", "image/jpeg"));

        await host.Ok(new SetAssetAlt(assetId, "en", "A golden retriever"));

        Assert.Equal("A golden retriever", host.Get<AssetLibrary>().Get(assetId)!.DefaultAlt.Get(new Locale("en")));
    }

    [Fact]
    public async Task SetAssetAlt_with_invalid_locale_fails_validation()
    {
        await using var host = SliceTestHelpers.NewAssetHost();
        var assetId = AssetId.New();
        await host.Ok(SliceTestHelpers.NewUpload(assetId, "photo.jpg", "image/jpeg"));

        var error = await host.Fails(new SetAssetAlt(assetId, "english!", "Alt"));

        Assert.Contains("not a valid locale tag", error);
    }

    [Fact]
    public async Task SetAssetAlt_over_500_characters_is_rejected()
    {
        await using var host = SliceTestHelpers.NewAssetHost();
        var assetId = AssetId.New();
        await host.Ok(SliceTestHelpers.NewUpload(assetId, "photo.jpg", "image/jpeg"));

        var error = await host.Fails(new SetAssetAlt(assetId, "en", new string('a', 501)));

        Assert.Contains("500 characters", error);
    }

    [Fact]
    public async Task RenameAsset_happy_path_updates_AssetLibrary()
    {
        await using var host = SliceTestHelpers.NewAssetHost();
        var assetId = AssetId.New();
        await host.Ok(SliceTestHelpers.NewUpload(assetId, "photo.jpg", "image/jpeg"));

        await host.Ok(new RenameAsset(assetId, "Team photo"));

        Assert.Equal("Team photo", host.Get<AssetLibrary>().Get(assetId)!.Name);
    }

    [Fact]
    public async Task RenameAsset_with_empty_name_is_rejected()
    {
        await using var host = SliceTestHelpers.NewAssetHost();
        var assetId = AssetId.New();
        await host.Ok(SliceTestHelpers.NewUpload(assetId, "photo.jpg", "image/jpeg"));

        var error = await host.Fails(new RenameAsset(assetId, " "));

        Assert.Contains("needs a name", error);
        Assert.Equal("photo", host.Get<AssetLibrary>().Get(assetId)!.Name);
    }
}
