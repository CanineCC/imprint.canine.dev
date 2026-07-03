using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Domain.Sites.Events;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Tests.Domain.Sites;

public sealed class SiteCreationTests
{
    private static readonly Locale En = new("en");

    [Fact]
    public void Create_with_valid_name_raises_created_and_starts_from_defaults()
    {
        var id = SiteId.New();

        var site = Site.Create(id, "Marketing site", En);

        Assert.Equal(new object[] { new SiteCreated(id, "Marketing site", En) }, site.UncommittedEvents);
        Assert.Equal(id, site.Id);
        Assert.Equal("Marketing site", site.Name);
        Assert.Equal(new[] { En }, site.Locales);
        Assert.Equal(En, site.DefaultLocale);
        Assert.Equal(Theme.Default, site.Theme);
        Assert.Empty(site.Navigation);
        Assert.Equal(id.Stream, site.StreamId);
    }

    [Fact]
    public void Create_with_surrounding_whitespace_stores_the_trimmed_name()
    {
        var site = Site.Create(SiteId.New(), "  Marketing site  ", En);

        Assert.Equal("Marketing site", site.Name);
    }

    [Fact]
    public void Create_with_empty_name_is_rejected()
    {
        var exception = Assert.Throws<DomainException>(() => Site.Create(SiteId.New(), "   ", En));

        Assert.Contains("name cannot be empty", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_with_name_over_100_characters_is_rejected()
    {
        var exception = Assert.Throws<DomainException>(() => Site.Create(SiteId.New(), new string('x', 101), En));

        Assert.Contains("100 characters", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_with_name_of_exactly_100_characters_is_accepted()
    {
        var site = Site.Create(SiteId.New(), new string('x', 100), En);

        Assert.Equal(new string('x', 100), site.Name);
    }

    [Fact]
    public void Replaying_an_unknown_event_throws()
    {
        var site = new Site();

        Assert.Throws<InvalidOperationException>(() => site.LoadFrom([new object()]));
    }
}
