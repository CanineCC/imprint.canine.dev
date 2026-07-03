using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Blocks;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Features.Assets;
using Imprint.Authoring.Features.Assets.UploadAsset;
using Imprint.Authoring.Features.Sites.CreateSite;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Tests.Features;

/// <summary>
/// Arrangement helpers for slice tests. Page editing has its own feature area (and its
/// own slices, out of this suite's scope), so tests arrange page and block content
/// directly through the aggregates — appending exactly the events those slices would —
/// and then catch projections up manually.
/// </summary>
internal static class SliceTestHelpers
{
    /// <summary>
    /// A host with the asset pipeline registered. The hosted worker is never started
    /// (no IHost in tests) — worker logic is driven directly where a test needs it.
    /// </summary>
    public static AuthoringTestHost NewAssetHost() =>
        new(services => services.AddImprintAssetProcessing());

    public static Task CatchUp(this AuthoringTestHost host) =>
        host.Get<ProjectionEngine>().CatchUp();

    public static async Task SaveAggregate(this AuthoringTestHost host, AggregateRoot aggregate)
    {
        await host.Get<IAggregateStore>().Save(aggregate);
        await host.CatchUp();
    }

    public static async Task<SiteId> CreateTestSite(this AuthoringTestHost host, string defaultLocale = "en")
    {
        var siteId = SiteId.New();
        await host.Ok(new CreateSite(siteId, "Test site", defaultLocale));
        return siteId;
    }

    public static async Task<PageId> CreateTestPage(
        this AuthoringTestHost host,
        SiteId siteId,
        string slug = "home",
        string title = "Home",
        Action<Page>? build = null)
    {
        var pageId = PageId.New();
        Assert.True(Slug.TryCreate(slug, out var parsed, out _), $"test slug '{slug}' is invalid");
        var page = Page.Create(pageId, siteId, parsed, new Locale("en"), title);
        build?.Invoke(page);
        await host.SaveAggregate(page);
        return pageId;
    }

    public static async Task MutatePage(this AuthoringTestHost host, PageId pageId, Action<Page> mutate)
    {
        var store = host.Get<IAggregateStore>();
        var page = await store.Load<Page>(pageId.Stream);
        mutate(page);
        await store.Save(page);
        await host.CatchUp();
    }

    public static async Task<BlockDefinitionId> CreateTestBlock(this AuthoringTestHost host, string name, Node spec)
    {
        var blockId = BlockDefinitionId.New();
        await host.SaveAggregate(BlockDefinition.Define(blockId, name, spec));
        return blockId;
    }

    /// <summary>An upload command over an in-memory payload of the given size.</summary>
    public static UploadAsset NewUpload(AssetId assetId, string fileName, string contentType, int byteSize = 1024) =>
        new(assetId, fileName, contentType, byteSize, new MemoryStream(new byte[byteSize]));
}
