namespace Imprint.Publishing.Tests.Pipeline;

public sealed class PreviewSlugsTests
{
    // The real roster on 2026-10-29: three sites start with "Canine" and two with "Assay".
    private static readonly (string Id, string Name)[] Roster =
    [
        ("63030ae7aeb94355a019362c150fd420", "Watchdog"),
        ("c3236bdf87db403ebc06379649f7a2d8", "CAI"),
        ("91884b35d86542a898bdc7960fce6f89", "Assay"),
        ("30d6c37927124e2692046abdc716f5eb", "Canine"),
        ("cb3e2582c50f4704960669fa6ffaec7a", "Canine Blog"),
        ("f58213a3eb694ba399a8d6fe0b15fb0f", "Canine Code Analyzer"),
        ("707963ae892b40f99c5bbb3bbee9dcda", "Resource Center"),
        ("7330cf66b16e44728f1acc1728558a5e", "Assay Preview"),
    ];

    [Fact]
    public void Every_site_gets_its_own_preview_folder()
    {
        var slugs = PreviewSlugs.Assign(Roster);

        Assert.Equal(Roster.Length, slugs.Values.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void A_site_whose_first_word_is_its_own_keeps_it()
    {
        var slugs = PreviewSlugs.Assign(Roster);

        Assert.Equal("watchdog", slugs["63030ae7aeb94355a019362c150fd420"]);
        Assert.Equal("resource", slugs["707963ae892b40f99c5bbb3bbee9dcda"]);
    }

    [Fact]
    public void Sites_that_share_a_first_word_are_told_apart_by_their_whole_name()
    {
        var slugs = PreviewSlugs.Assign(Roster);

        Assert.Equal("canine", slugs["30d6c37927124e2692046abdc716f5eb"]);
        Assert.Equal("canineblog", slugs["cb3e2582c50f4704960669fa6ffaec7a"]);
        Assert.Equal("caninecodeanalyzer", slugs["f58213a3eb694ba399a8d6fe0b15fb0f"]);
        Assert.Equal("assay", slugs["91884b35d86542a898bdc7960fce6f89"]);
        Assert.Equal("assaypreview", slugs["7330cf66b16e44728f1acc1728558a5e"]);
    }

    [Fact]
    public void Two_sites_with_the_same_name_fall_back_to_their_ids()
    {
        var slugs = PreviewSlugs.Assign([("aaaa", "Twin Site"), ("bbbb", "Twin Site"), ("cccc", "")]);

        Assert.Equal("aaaa", slugs["aaaa"]);
        Assert.Equal("bbbb", slugs["bbbb"]);
        Assert.Equal("cccc", slugs["cccc"]);
    }
}
