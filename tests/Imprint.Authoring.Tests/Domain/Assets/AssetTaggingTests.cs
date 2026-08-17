using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Assets;
using Imprint.Authoring.Domain.Assets.Events;
using Imprint.TestKit;

namespace Imprint.Authoring.Tests.Domain.Assets;

public sealed class AssetTaggingTests
{
    private static readonly AssetId Id = AssetId.New();

    private static AssetUploaded Uploaded() =>
        new(Id, "hero.png", "image/png", AssetKind.Image, 12_345, "originals/hero.png");

    [Fact]
    public void Tag_files_the_asset_under_the_tag()
    {
        var outcome = AggregateSpec.For<Asset>()
            .Given(Uploaded())
            .When(a => a.Tag("Blog-entry-20"));

        outcome.ThenRaised(new AssetTagged("Blog-entry-20"));
        Assert.Equal(["Blog-entry-20"], outcome.Aggregate.Tags);
    }

    [Fact]
    public void Tag_keeps_the_casing_the_author_typed()
    {
        var outcome = AggregateSpec.For<Asset>()
            .Given(Uploaded())
            .When(a => a.Tag("Blog-Entry-20"));

        outcome.ThenRaised(new AssetTagged("Blog-Entry-20"));
    }

    [Theory]
    [InlineData("  Blog-entry-20  ", "Blog-entry-20")]
    [InlineData("Blog   entry 20", "Blog entry 20")]
    [InlineData("\tSpring   photos\n", "Spring photos")]
    public void Tag_trims_and_collapses_whitespace(string typed, string stored) =>
        AggregateSpec.For<Asset>()
            .Given(Uploaded())
            .When(a => a.Tag(typed))
            .ThenRaised(new AssetTagged(stored));

    [Fact]
    public void Tag_that_is_already_there_raises_nothing() =>
        AggregateSpec.For<Asset>()
            .Given(Uploaded(), new AssetTagged("Blog-entry-20"))
            .When(a => a.Tag("Blog-entry-20"))
            .ThenNothing();

    [Fact]
    public void Tag_differing_only_in_case_is_the_same_tag() =>
        AggregateSpec.For<Asset>()
            .Given(Uploaded(), new AssetTagged("Blog-entry-20"))
            .When(a => a.Tag("BLOG-ENTRY-20"))
            .ThenNothing();

    [Fact]
    public void Tag_a_second_label_keeps_both()
    {
        var outcome = AggregateSpec.For<Asset>()
            .Given(Uploaded(), new AssetTagged("Blog-entry-20"))
            .When(a => a.Tag("Portraits"));

        outcome.ThenRaised(new AssetTagged("Portraits"));
        Assert.Equal(["Blog-entry-20", "Portraits"], outcome.Aggregate.Tags);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Tag_that_is_blank_is_rejected(string tag) =>
        AggregateSpec.For<Asset>()
            .Given(Uploaded())
            .When(a => a.Tag(tag))
            .ThenFails("needs a name");

    [Fact]
    public void Tag_over_60_characters_is_rejected() =>
        AggregateSpec.For<Asset>()
            .Given(Uploaded())
            .When(a => a.Tag(new string('a', 61)))
            .ThenFails("60");

    [Fact]
    public void Tag_at_60_characters_is_accepted() =>
        AggregateSpec.For<Asset>()
            .Given(Uploaded())
            .When(a => a.Tag(new string('a', 60)))
            .ThenRaised(new AssetTagged(new string('a', 60)));

    [Fact]
    public void Tag_beyond_the_limit_of_25_is_rejected() =>
        AggregateSpec.For<Asset>()
            .Given([Uploaded(), .. Enumerable.Range(0, 25).Select(i => new AssetTagged($"tag-{i}"))])
            .When(a => a.Tag("one-too-many"))
            .ThenFails("limit");

    [Fact]
    public void Untag_removes_the_tag()
    {
        var outcome = AggregateSpec.For<Asset>()
            .Given(Uploaded(), new AssetTagged("Blog-entry-20"), new AssetTagged("Portraits"))
            .When(a => a.Untag("Blog-entry-20"));

        outcome.ThenRaised(new AssetUntagged("Blog-entry-20"));
        Assert.Equal(["Portraits"], outcome.Aggregate.Tags);
    }

    [Fact]
    public void Untag_reports_the_stored_spelling_not_the_callers()
    {
        // The event is the record of what left the asset, so it says what was on it.
        var outcome = AggregateSpec.For<Asset>()
            .Given(Uploaded(), new AssetTagged("Blog-Entry-20"))
            .When(a => a.Untag("blog-entry-20"));

        outcome.ThenRaised(new AssetUntagged("Blog-Entry-20"));
        Assert.Empty(outcome.Aggregate.Tags);
    }

    [Fact]
    public void Untag_a_tag_the_asset_never_had_raises_nothing() =>
        AggregateSpec.For<Asset>()
            .Given(Uploaded(), new AssetTagged("Portraits"))
            .When(a => a.Untag("Blog-entry-20"))
            .ThenNothing();

    [Fact]
    public void Tag_on_deleted_asset_is_rejected() =>
        AggregateSpec.For<Asset>()
            .Given(Uploaded(), new AssetDeleted())
            .When(a => a.Tag("Blog-entry-20"))
            .ThenFails("deleted");

    [Fact]
    public void Untag_on_deleted_asset_is_rejected() =>
        AggregateSpec.For<Asset>()
            .Given(Uploaded(), new AssetTagged("Blog-entry-20"), new AssetDeleted())
            .When(a => a.Untag("Blog-entry-20"))
            .ThenFails("deleted");
}
