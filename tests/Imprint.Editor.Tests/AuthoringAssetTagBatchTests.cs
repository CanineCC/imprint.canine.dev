using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Assets;
using Imprint.Authoring.Projections;
using Imprint.Editor.Api;
using Imprint.EventSourcing;

namespace Imprint.Editor.Tests;

/// <summary>
/// The batch check in front of <c>POST /assets/tags/add|remove</c> and the <c>tag_assets</c> /
/// <c>untag_assets</c> MCP tools. It validates the WHOLE batch before a single command is
/// dispatched, on purpose: a caller filing a post's figures sends one call with a dozen ids, and
/// a half-applied batch would leave it working out which pairs landed before the typo'd id.
/// </summary>
public sealed class AuthoringAssetTagBatchTests
{
    private static readonly EventMetadata Meta = new("alice", DateTimeOffset.UnixEpoch, Guid.Empty, Guid.Empty);

    private static AssetLibrary LibraryWith(params AssetId[] ids)
    {
        var library = new AssetLibrary();
        long position = 0;
        foreach (var id in ids)
        {
            var asset = Asset.Upload(id, "figure.svg", "image/svg+xml", AssetKind.Vector, 4_096, $"originals/{id.Compact}.svg");
            long version = 0;
            foreach (var @event in asset.UncommittedEvents)
            {
                library.Apply(new StoredEvent(++position, asset.StreamId, ++version, @event.GetType().Name, @event, Meta));
            }
        }

        return library;
    }

    [Fact]
    public void ReadTagBatch_returns_the_ids_and_the_normalized_tags()
    {
        var first = AssetId.New();
        var second = AssetId.New();

        var (ids, tags, error) = AuthoringApi.ReadTagBatch(
            [first.Compact, second.Compact], ["  B20 ", "Blog"], LibraryWith(first, second));

        Assert.Null(error);
        Assert.Equal([first, second], ids);
        Assert.Equal(["B20", "Blog"], tags);
    }

    [Fact]
    public void ReadTagBatch_drops_duplicate_tags_case_insensitively()
    {
        var id = AssetId.New();

        var (_, tags, error) = AuthoringApi.ReadTagBatch([id.Compact], ["B20", "b20", "Blog"], LibraryWith(id));

        Assert.Null(error);
        Assert.Equal(["B20", "Blog"], tags);
    }

    [Fact]
    public void ReadTagBatch_rejects_an_unknown_asset_without_applying_the_rest()
    {
        var known = AssetId.New();
        var stranger = AssetId.New();

        var (ids, tags, error) = AuthoringApi.ReadTagBatch(
            [known.Compact, stranger.Compact], ["B20"], LibraryWith(known));

        Assert.Contains("unknown asset", error);
        Assert.Empty(ids);
        Assert.Empty(tags);
    }

    [Fact]
    public void ReadTagBatch_rejects_a_malformed_id()
    {
        var known = AssetId.New();

        var (_, _, error) = AuthoringApi.ReadTagBatch([known.Compact, "not-a-guid"], ["B20"], LibraryWith(known));

        Assert.Contains("invalid assetId", error);
    }

    [Fact]
    public void ReadTagBatch_without_assets_is_rejected()
    {
        var library = LibraryWith(AssetId.New());

        Assert.Equal("assetIds is required", AuthoringApi.ReadTagBatch(null, ["B20"], library).Error);
        Assert.Equal("assetIds is required", AuthoringApi.ReadTagBatch([], ["B20"], library).Error);
    }

    [Fact]
    public void ReadTagBatch_with_only_blank_tags_is_rejected()
    {
        var id = AssetId.New();

        var (_, _, error) = AuthoringApi.ReadTagBatch([id.Compact], ["  ", ""], LibraryWith(id));

        Assert.Equal("tags is required", error);
    }
}
