using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Features.Sites.CreateBlog;
using Imprint.Authoring.Projections;
using CreateSiteCommand = Imprint.Authoring.Features.Sites.CreateSite.CreateSite;

namespace Imprint.Authoring.Tests.Features.Sites;

/// <summary>
/// Creating a blog is creating a site with a kind and, usually, the one thing that makes
/// a blog worth having a subdomain for: somewhere to publish it. The origin and the
/// publish folder travel with the creation because that is the moment the author is
/// thinking about them — "a blog at blog.canine.dev" is one thought, not a creation
/// followed by a trip to a settings page.
/// </summary>
public sealed class CreateBlogTests
{
    [Fact]
    public async Task CreateBlog_appears_in_SiteOverview_as_a_blog()
    {
        await using var host = new AuthoringTestHost();
        var blogId = SiteId.New();

        await host.Ok(new CreateBlog(blogId, "Canine blog", "en"));

        var blog = host.Get<SiteOverview>().Get(blogId);
        Assert.NotNull(blog);
        Assert.Equal(SiteKind.Blog, blog.Kind);
        Assert.Equal("Canine blog", blog.Name);
        Assert.Equal(new Locale("en"), blog.DefaultLocale);
    }

    [Fact]
    public async Task CreateSite_still_makes_a_site()
    {
        // The kinds must not blur: the existing creation path is what every site in the
        // wild came through, and it keeps meaning "page tree".
        await using var host = new AuthoringTestHost();
        var siteId = SiteId.New();

        await host.Ok(new CreateSiteCommand(siteId, "Marketing", "en"));

        Assert.Equal(SiteKind.Site, host.Get<SiteOverview>().Get(siteId)!.Kind);
    }

    [Fact]
    public async Task CreateBlog_with_a_publish_folder_and_url_configures_production()
    {
        await using var host = new AuthoringTestHost();
        var blogId = SiteId.New();

        await host.Ok(new CreateBlog(
            blogId, "Canine blog", "en",
            PublishFolder: "/srv/www/blog.canine.dev",
            PublicUrl: "https://blog.canine.dev"));

        var environment = Assert.Single(host.Get<SiteOverview>().Get(blogId)!.Environments);
        Assert.Equal("Production", environment.Name);
        Assert.Equal("/srv/www/blog.canine.dev", environment.Path);
        Assert.Equal("https://blog.canine.dev", environment.BaseUrl);
    }

    [Fact]
    public async Task CreateBlog_with_only_a_publish_folder_configures_production_without_an_origin()
    {
        // A folder alone is a working publish target: output is root-relative, exactly as
        // a site with no BaseUrl has always published.
        await using var host = new AuthoringTestHost();
        var blogId = SiteId.New();

        await host.Ok(new CreateBlog(blogId, "Canine blog", "en", PublishFolder: "/srv/www/blog"));

        var environment = Assert.Single(host.Get<SiteOverview>().Get(blogId)!.Environments);
        Assert.Equal("/srv/www/blog", environment.Path);
        Assert.Null(environment.BaseUrl);
    }

    [Fact]
    public async Task CreateBlog_with_neither_leaves_the_blog_unpublished()
    {
        // Both fields are optional: a blog you can write in today and point somewhere
        // tomorrow is better than a form that will not submit.
        await using var host = new AuthoringTestHost();
        var blogId = SiteId.New();

        await host.Ok(new CreateBlog(blogId, "Canine blog", "en"));

        Assert.Empty(host.Get<SiteOverview>().Get(blogId)!.Environments);
    }

    [Fact]
    public async Task CreateBlog_with_a_url_but_no_folder_fails_validation()
    {
        // A deploy environment cannot exist without a publish folder, so a URL alone
        // would silently do nothing. Say so instead of accepting it.
        await using var host = new AuthoringTestHost();

        var error = await host.Fails(new CreateBlog(
            SiteId.New(), "Canine blog", "en", PublicUrl: "https://blog.canine.dev"));

        Assert.Contains("publish folder", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateBlog_with_empty_name_fails_validation()
    {
        await using var host = new AuthoringTestHost();

        var error = await host.Fails(new CreateBlog(SiteId.New(), "   ", "en"));

        Assert.Contains("name cannot be empty", error);
    }

    [Fact]
    public async Task CreateBlog_with_overlong_name_fails_validation()
    {
        await using var host = new AuthoringTestHost();

        var error = await host.Fails(new CreateBlog(SiteId.New(), new string('x', 101), "en"));

        Assert.Contains("100 characters", error);
    }

    [Fact]
    public async Task CreateBlog_with_invalid_locale_fails_validation()
    {
        await using var host = new AuthoringTestHost();

        var error = await host.Fails(new CreateBlog(SiteId.New(), "Canine blog", "not a locale"));

        Assert.Contains("locale", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateBlog_with_a_malformed_url_fails_and_creates_nothing()
    {
        // The origin is validated by the aggregate on the way to the environment. The
        // whole command must fail closed: a blog left half-created, named but pointing
        // nowhere, is worse than one the author has to submit twice.
        await using var host = new AuthoringTestHost();
        var blogId = SiteId.New();

        await host.Fails(new CreateBlog(
            blogId, "Canine blog", "en",
            PublishFolder: "/srv/www/blog",
            PublicUrl: "not-a-url"));

        Assert.Null(host.Get<SiteOverview>().Get(blogId));
    }
}
