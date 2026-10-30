using System.Text.Json;
using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Projections;
using Imprint.Editor.Mcp;
using Imprint.EventSourcing;
using Microsoft.Extensions.Configuration;
using ConfigureEnvironmentsCmd = Imprint.Authoring.Features.Sites.ConfigureEnvironments.ConfigureEnvironments;
using RenameSiteCmd = Imprint.Authoring.Features.Sites.RenameSite.RenameSite;

namespace Imprint.Editor.Tests;

/// <summary>
/// The MCP tools that move a site onto another site's address: rename, the deploy environments, and the read of
/// the current environments. Moving the new Assay site onto assay.canine.dev needed all three, and they existed only
/// in the interactive editor. The environments tool reuses the authoring API's parser, so what is pinned here is the
/// forward (the command carries what was sent) and the refusals that must never reach the aggregate.
/// </summary>
public sealed class McpSiteMoveToolsTests
{
    private static readonly EventMetadata Meta = new("alice", DateTimeOffset.UnixEpoch, Guid.Empty, Guid.Empty);

    private sealed class FakeDispatcher : ICommandDispatcher
    {
        public List<ICommand> Dispatched { get; } = [];

        public Task<Result> Dispatch(ICommand command, CancellationToken ct = default)
        {
            Dispatched.Add(command);
            return Task.FromResult(Result.Ok());
        }
    }

    private static readonly IConfiguration Config = new ConfigurationBuilder().Build();

    private static JsonElement Json(object result) => JsonSerializer.SerializeToElement(result);

    [Fact]
    public async Task Rename_forwards_the_new_name()
    {
        var id = SiteId.New();
        var dispatcher = new FakeDispatcher();

        var result = await ImprintAuthoringMcpTools.RenameSite(id.Compact, "Assay", dispatcher, Config);

        Assert.True(Json(result).GetProperty("ok").GetBoolean());
        var sent = Assert.IsType<RenameSiteCmd>(Assert.Single(dispatcher.Dispatched));
        Assert.Equal(id, sent.SiteId);
        Assert.Equal("Assay", sent.Name);
    }

    [Fact]
    public async Task Environments_forward_the_folder_and_the_trimmed_origin()
    {
        var id = SiteId.New();
        var dispatcher = new FakeDispatcher();

        var result = await ImprintAuthoringMcpTools.SetSiteEnvironments(
            id.Compact, """[{"name":"Production","path":"/srv/assay","baseUrl":"https://assay.canine.dev/"}]""", dispatcher, Config);

        Assert.True(Json(result).GetProperty("ok").GetBoolean());
        var sent = Assert.IsType<ConfigureEnvironmentsCmd>(Assert.Single(dispatcher.Dispatched));
        var environment = Assert.Single(sent.Environments);
        Assert.Equal("/srv/assay", environment.Path);
        Assert.Equal("https://assay.canine.dev", environment.BaseUrl);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("""{"name":"Production","path":"/srv"}""")]
    [InlineData("""[{"name":"Production"}]""")]
    [InlineData("""[{"name":"Production","path":"/srv","baseUrl":"assay.canine.dev"}]""")]
    public async Task A_malformed_list_is_refused_before_the_aggregate(string environmentsJson)
    {
        var dispatcher = new FakeDispatcher();

        var result = await ImprintAuthoringMcpTools.SetSiteEnvironments(SiteId.New().Compact, environmentsJson, dispatcher, Config);

        Assert.False(Json(result).GetProperty("ok").GetBoolean());
        Assert.Empty(dispatcher.Dispatched);
    }

    [Fact]
    public void Get_site_reads_back_the_environments()
    {
        var id = SiteId.New();
        var site = Site.Create(id, "Assay Preview", new Locale("en"));
        site.SetEnvironments([new DeployEnvironment("Production", "/srv/assaypreview", "https://assay.canine.dev")]);
        var overview = new SiteOverview();
        long position = 0, version = 0;
        foreach (var @event in site.UncommittedEvents)
        {
            overview.Apply(new StoredEvent(++position, site.StreamId, ++version, @event.GetType().Name, @event, Meta));
        }

        var json = Json(ImprintAuthoringMcpTools.GetSite(id.Compact, overview, new PageList()));

        var environment = Assert.Single(json.GetProperty("environments").EnumerateArray());
        Assert.Equal("/srv/assaypreview", environment.GetProperty("path").GetString());
        Assert.Equal("https://assay.canine.dev", environment.GetProperty("baseUrl").GetString());
    }
}
