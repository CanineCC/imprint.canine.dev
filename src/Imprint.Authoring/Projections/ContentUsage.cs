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

    // The cases here have to match the ones the publisher collects in
    // SitePublisher.AssetReferencesOf: a file ships precisely because something references it,
    // so anything that makes a file ship must also hold its deletion. A media node carries the
    // reference as a prop; a button or a prose anchor carries it as an asset LINK, and those two
    // were the ones missing — which left a linked whitepaper PDF deletable while a published page
    // still offered it for download.
    private static bool ReferencesAsset(IEnumerable<Node> nodes, AssetId assetId) =>
        nodes.Any(node => node switch
        {
            ImageNode image => image.AssetId == assetId,
            VideoNode video => video.AssetId == assetId,
            SvgNode svg => svg.AssetId == assetId,
            ButtonNode { LinkTo: AssetLink link } => link.AssetId == assetId,
            RichTextNode richText => LinksAsset(richText.Html, assetId),
            _ => false,
        });

    /// <summary>
    /// Asset links inside prose. Read through <see cref="AssetHref"/> rather than by walking
    /// anchors, so this side stays clear of the renderer: the guard only asks whether the id is
    /// referenced at all, and over-counting refuses a deletion, which is the safe direction to err.
    /// </summary>
    private static bool LinksAsset(LocalizedText html, AssetId assetId)
    {
        foreach (var (_, value) in html.Values)
        {
            var pos = value.IndexOf(AssetHref.Scheme, StringComparison.OrdinalIgnoreCase);
            while (pos >= 0)
            {
                var end = pos + AssetHref.Scheme.Length;
                while (end < value.Length && (char.IsAsciiLetterOrDigit(value[end]) || value[end] == '-'))
                {
                    end++;
                }

                if (AssetHref.TryParse(value[pos..end], out var linked) && linked == assetId)
                {
                    return true;
                }

                pos = value.IndexOf(AssetHref.Scheme, end, StringComparison.OrdinalIgnoreCase);
            }
        }

        return false;
    }
}
