using System.Text.Json;
using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Projections;
using Imprint.Editor.Mcp;
using Imprint.EventSourcing;

namespace Imprint.Editor.Tests;

/// <summary>
/// get_site must return the llms.txt settings, because set_llms_preamble REPLACES the whole value.
/// Without a read there is no way to amend a preamble — only to overwrite one blind — and a real site's
/// preamble is thousands of words of hand-written copy. Same contract as navigation and footer: you edit
/// what you read back.
/// </summary>
public sealed class McpSiteLlmsReadTests
{
    private static readonly EventMetadata Meta = new("alice", DateTimeOffset.UnixEpoch, Guid.Empty, Guid.Empty);

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

    private static JsonElement Read(Site site, SiteId id) =>
        JsonSerializer.SerializeToElement(
            ImprintAuthoringMcpTools.GetSite(id.Compact, OverviewOf(site), new PageList()));

    [Fact]
    public void The_preamble_comes_back_as_it_was_written()
    {
        var id = SiteId.New();
        var site = Site.Create(id, "Acme Studio", new Locale("en"));
        const string Preamble = "# Acme Studio\n\n> The independent surveyor.\n\n- One reproducible score.";
        site.SetLlmsPreamble(Preamble);

        var json = Read(site, id);

        Assert.True(json.GetProperty("ok").GetBoolean());
        Assert.Equal(Preamble, json.GetProperty("llmsPreamble").GetString());
    }

    [Fact]
    public void An_unset_preamble_reads_as_null_rather_than_as_an_empty_edit()
    {
        var id = SiteId.New();
        var site = Site.Create(id, "Acme Studio", new Locale("en"));

        var json = Read(site, id);

        Assert.Equal(JsonValueKind.Null, json.GetProperty("llmsPreamble").ValueKind);
        Assert.Empty(json.GetProperty("llmsExcludedPaths").EnumerateArray());
    }

    /// <summary>
    /// ★ The aggregate NORMALISES an excluded path by stripping the leading slash, so what comes back is
    /// not byte-identical to what went in. That is exactly why the read matters: a caller who assumed the
    /// round trip was lossless and wrote the list straight back would be fine here, but a caller
    /// COMPARING the two to decide whether a change is needed would see a difference on every call.
    /// </summary>
    [Fact]
    public void The_excluded_paths_come_back_normalised_without_their_leading_slash()
    {
        var id = SiteId.New();
        var site = Site.Create(id, "Acme Studio", new Locale("en"));
        site.SetLlmsExcludedPaths(["/page-tos", "page-dpa"]);

        var paths = Read(site, id).GetProperty("llmsExcludedPaths")
            .EnumerateArray().Select(p => p.GetString()).ToList();

        Assert.Equal(["page-tos", "page-dpa"], paths);
    }
}
