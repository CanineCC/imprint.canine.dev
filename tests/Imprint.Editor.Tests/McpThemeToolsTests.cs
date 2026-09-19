using System.Text.Json;
using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Projections;
using Imprint.Editor.Mcp;
using Imprint.EventSourcing;
using Microsoft.Extensions.Configuration;
using ChangeThemeTokenCmd = Imprint.Authoring.Features.Sites.ChangeThemeToken.ChangeThemeToken;
using ChangeTypographyCmd = Imprint.Authoring.Features.Sites.ChangeTypography.ChangeTypography;

namespace Imprint.Editor.Tests;

/// <summary>
/// The MCP theme tools. The colour tool is a thin forward — the aggregate owns token
/// membership and colour syntax — so what is worth pinning here is the TYPOGRAPHY tool's
/// patch semantics: it takes a whole value object but is called with single dials, so an
/// omitted argument must come back as the site's current value and never as a default.
/// A tool that quietly reset the type scale while changing a corner radius would be
/// indistinguishable from a working one until someone looked at the published site.
/// </summary>
public sealed class McpThemeToolsTests
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

    // Folds the aggregate's uncommitted events into the read model exactly as the
    // projection engine would — no shortcut into the read model's internals.
    private static SiteOverview OverviewOf(Site site)
    {
        var overview = new SiteOverview();
        long position = 0, version = 0;
        foreach (var @event in site.UncommittedEvents)
        {
            overview.Apply(new StoredEvent(
                ++position, site.StreamId, ++version, @event.GetType().Name, @event, Meta));
        }

        return overview;
    }

    private static (SiteId Id, SiteOverview Sites, FakeDispatcher Dispatcher, IConfiguration Config) NewSite()
    {
        var id = SiteId.New();
        var site = Site.Create(id, "Acme Studio", new Locale("en"));
        return (id, OverviewOf(site), new FakeDispatcher(), new ConfigurationBuilder().Build());
    }

    private static JsonElement Json(object result) => JsonSerializer.SerializeToElement(result);

    [Fact]
    public async Task Setting_one_typography_dial_keeps_every_other_current_value()
    {
        var (id, sites, dispatcher, config) = NewSite();
        var before = sites.Get(id)!.Theme.Typography;

        var result = await ImprintAuthoringMcpTools.SetTypography(
            id.Compact, headingFont: null, bodyFont: null, baseSizePx: null,
            scaleRatio: null, radiusPx: 0, spacing: null, panelKickerRule: null, dispatcher, config, sites);

        Assert.True(Json(result).GetProperty("ok").GetBoolean());
        var sent = Assert.IsType<ChangeTypographyCmd>(Assert.Single(dispatcher.Dispatched));
        Assert.Equal(0, sent.Typography.RadiusPx);
        Assert.Equal(before with { RadiusPx = 0 }, sent.Typography);
    }

    [Fact]
    public async Task A_font_stack_is_chosen_by_name_case_insensitively()
    {
        var (id, sites, dispatcher, config) = NewSite();

        await ImprintAuthoringMcpTools.SetTypography(
            id.Compact, headingFont: "grotesk", bodyFont: null, baseSizePx: null,
            scaleRatio: null, radiusPx: null, spacing: null, panelKickerRule: null, dispatcher, config, sites);

        var sent = Assert.IsType<ChangeTypographyCmd>(Assert.Single(dispatcher.Dispatched));
        Assert.Equal(FontStack.Grotesk, sent.Typography.Heading);
        Assert.Equal(sites.Get(id)!.Theme.Typography.Body, sent.Typography.Body);
    }

    [Fact]
    public async Task An_unknown_font_stack_is_refused_by_name_and_nothing_is_dispatched()
    {
        var (id, sites, dispatcher, config) = NewSite();

        var result = await ImprintAuthoringMcpTools.SetTypography(
            id.Compact, headingFont: "Comic Sans", bodyFont: null, baseSizePx: null,
            scaleRatio: null, radiusPx: null, spacing: null, panelKickerRule: null, dispatcher, config, sites);

        var json = Json(result);
        Assert.False(json.GetProperty("ok").GetBoolean());
        var error = json.GetProperty("error").GetString()!;
        Assert.Contains("Comic Sans", error, StringComparison.Ordinal);
        Assert.Contains("Grotesk", error, StringComparison.Ordinal);
        Assert.Empty(dispatcher.Dispatched);
    }

    [Fact]
    public async Task A_colour_token_is_forwarded_with_both_of_its_values()
    {
        var (id, sites, dispatcher, config) = NewSite();

        var result = await ImprintAuthoringMcpTools.SetThemeToken(
            id.Compact, "primary", "#3b5bdb", "#748ffc", dispatcher, config, sites);

        Assert.True(Json(result).GetProperty("ok").GetBoolean());
        var sent = Assert.IsType<ChangeThemeTokenCmd>(Assert.Single(dispatcher.Dispatched));
        Assert.Equal("primary", sent.Token);
        Assert.Equal("#3b5bdb", sent.Light);
        Assert.Equal("#748ffc", sent.Dark);
    }

    [Fact]
    public void Get_site_reports_the_theme_so_a_caller_can_edit_what_it_read_back()
    {
        var (id, sites, _, _) = NewSite();

        var theme = Json(ImprintAuthoringMcpTools.GetSite(id.Compact, sites, new PageList()))
            .GetProperty("theme");

        Assert.Equal("#3b5bdb", theme.GetProperty("tokens").GetProperty("primary").GetProperty("light").GetString());
        Assert.Equal(16, theme.GetProperty("typography").GetProperty("baseSizePx").GetInt32());
        Assert.Equal("Comfortable", theme.GetProperty("typography").GetProperty("spacing").GetString());

        // Every known token is reported, so a caller never has to guess a name.
        foreach (var name in ThemeTokens.All)
        {
            Assert.True(theme.GetProperty("tokens").TryGetProperty(name, out _), name);
        }
    }
}
