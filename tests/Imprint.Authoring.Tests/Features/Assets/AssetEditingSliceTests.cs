using Imprint.Authoring.Domain;
using Imprint.Authoring.Features.Assets.RenameAsset;
using Imprint.Authoring.Features.Assets.SetAssetAlt;
using Imprint.Authoring.Features.Assets.TagAsset;
using Imprint.Authoring.Features.Assets.UntagAsset;
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

    // ------------------------------------------------------------------- tags

    [Fact]
    public async Task TagAsset_groups_the_asset_in_AssetLibrary()
    {
        await using var host = SliceTestHelpers.NewAssetHost();
        var tagged = AssetId.New();
        var other = AssetId.New();
        await host.Ok(SliceTestHelpers.NewUpload(tagged, "figure.jpg", "image/jpeg"));
        await host.Ok(SliceTestHelpers.NewUpload(other, "unrelated.jpg", "image/jpeg"));

        await host.Ok(new TagAsset(tagged, "Blog-entry-20"));

        var library = host.Get<AssetLibrary>();
        Assert.Equal(["Blog-entry-20"], library.Tags());
        Assert.Equal([tagged], library.Tagged("Blog-entry-20").Select(asset => asset.Id));
        Assert.Equal([other], library.Untagged().Select(asset => asset.Id));
    }

    [Fact]
    public async Task Tags_differing_only_in_case_are_one_group()
    {
        // Two assets, two spellings, one tag: the author typing 'blog-entry-20' the second
        // time is not starting a second collection.
        await using var host = SliceTestHelpers.NewAssetHost();
        var first = AssetId.New();
        var second = AssetId.New();
        await host.Ok(SliceTestHelpers.NewUpload(first, "one.jpg", "image/jpeg"));
        await host.Ok(SliceTestHelpers.NewUpload(second, "two.jpg", "image/jpeg"));

        await host.Ok(new TagAsset(first, "Blog-entry-20"));
        await host.Ok(new TagAsset(second, "blog-entry-20"));

        var library = host.Get<AssetLibrary>();
        Assert.Single(library.Tags());
        Assert.Equal(2, library.Tagged("BLOG-ENTRY-20").Count);
    }

    [Fact]
    public async Task UntagAsset_removes_the_group_when_it_was_the_last_member()
    {
        await using var host = SliceTestHelpers.NewAssetHost();
        var assetId = AssetId.New();
        await host.Ok(SliceTestHelpers.NewUpload(assetId, "figure.jpg", "image/jpeg"));
        await host.Ok(new TagAsset(assetId, "Blog-entry-20"));

        await host.Ok(new UntagAsset(assetId, "Blog-entry-20"));

        var library = host.Get<AssetLibrary>();
        Assert.Empty(library.Tags());
        Assert.Empty(library.Tagged("Blog-entry-20"));
        Assert.Equal([assetId], library.Untagged().Select(asset => asset.Id));
    }

    [Fact]
    public async Task TagAsset_with_a_blank_tag_is_rejected()
    {
        await using var host = SliceTestHelpers.NewAssetHost();
        var assetId = AssetId.New();
        await host.Ok(SliceTestHelpers.NewUpload(assetId, "photo.jpg", "image/jpeg"));

        var error = await host.Fails(new TagAsset(assetId, "   "));

        Assert.Contains("needs a name", error);
        Assert.Empty(host.Get<AssetLibrary>().Tags());
    }
}
