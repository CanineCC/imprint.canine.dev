using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;

namespace Imprint.Authoring.Tests.Domain.Pages;

/// <summary>
/// The <c>asset:{guid}</c> href scheme: a link to an uploaded file that survives publishing.
/// Stored as the reference because the editor and the published site serve the same file from
/// different URLs — a raw media URL can be right on at most one plane.
/// </summary>
public sealed class AssetHrefTests
{
    private static readonly Guid Id = new("7b0e2a1cc3d94f6e8a5b9c0d1e2f3a4b");

    [Fact]
    public void A_well_formed_asset_reference_parses_in_both_guid_formats()
    {
        Assert.True(AssetHref.TryParse($"asset:{Id:N}", out var compact));
        Assert.Equal(AssetId.From(Id), compact);

        Assert.True(AssetHref.TryParse($"asset:{Id:D}", out var dashed));
        Assert.Equal(AssetId.From(Id), dashed);
    }

    [Fact]
    public void Anything_that_is_not_a_guid_is_refused()
    {
        Assert.False(AssetHref.TryParse("asset:", out _));
        Assert.False(AssetHref.TryParse("asset:not-a-guid", out _));
        Assert.False(AssetHref.TryParse("https://example.com/file.pdf", out _));
    }

    [Fact]
    public void The_validator_admits_asset_references_through_the_same_gate_as_prose_links()
    {
        Assert.True(CanonicalHtml.IsAllowedHref($"asset:{Id:N}"));
        Assert.False(CanonicalHtml.IsAllowedHref("asset:not-a-guid"));
    }
}
