using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;

namespace Imprint.Authoring.Projections;

/// <summary>
/// Reference tracking as a query service over <see cref="PageDrafts"/> and
/// <see cref="BlockLibrary"/> rather than a separate fold: at CMS scale, walking the
/// trees on demand is microseconds, and a computed answer cannot drift out of sync
/// with the state it derives from. Serves delete guards and the publisher's
/// asset-staleness fan-out.
/// </summary>
public sealed class ContentUsage(PageDrafts drafts, BlockLibrary blocks)
{
    /// <summary>Pages whose draft references the asset — directly or through a placed block.</summary>
    public IReadOnlyList<PageId> PagesUsingAsset(AssetId assetId)
    {
        var blocksUsing = blocks.All()
            .Where(block => ReferencesAsset(PageTree.Flatten(block.Spec), assetId))
            .Select(block => block.Id)
            .ToHashSet();

        return
        [
            .. drafts.All
                .Where(page =>
                    ReferencesAsset(page.Tree.All(), assetId) ||
                    page.Tree.All().OfType<BlockInstanceNode>().Any(i => blocksUsing.Contains(i.DefinitionId)))
                .Select(page => page.Id),
        ];
    }

    /// <summary>Block definitions whose spec references the asset (blocks the asset's deletion too).</summary>
    public IReadOnlyList<BlockDefinitionId> BlocksUsingAsset(AssetId assetId) =>
        [.. blocks.All().Where(b => ReferencesAsset(PageTree.Flatten(b.Spec), assetId)).Select(b => b.Id)];

    public bool IsAssetInUse(AssetId assetId) =>
        PagesUsingAsset(assetId).Count > 0 || BlocksUsingAsset(assetId).Count > 0;

    public IReadOnlyList<PageId> PagesUsingBlock(BlockDefinitionId blockId) =>
        [
            .. drafts.All
                .Where(page => page.Tree.All().OfType<BlockInstanceNode>().Any(i => i.DefinitionId == blockId))
                .Select(page => page.Id),
        ];

    public int BlockInstanceCount(BlockDefinitionId blockId) =>
        drafts.All.Sum(page => page.Tree.All().OfType<BlockInstanceNode>().Count(i => i.DefinitionId == blockId));

    private static bool ReferencesAsset(IEnumerable<Node> nodes, AssetId assetId) =>
        nodes.Any(node => node switch
        {
            ImageNode image => image.AssetId == assetId,
            VideoNode video => video.AssetId == assetId,
            SvgNode svg => svg.AssetId == assetId,
            _ => false,
        });
}
