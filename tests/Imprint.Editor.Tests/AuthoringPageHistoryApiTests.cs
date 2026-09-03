using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Imprint.Authoring;
using Imprint.Authoring.Features.Pages;
using Imprint.Editor.Api;
using Imprint.EventSourcing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Imprint.Editor.Tests;

/// <summary>
/// AddImprintAuthoring registers the slices but not a widget catalog — the editor supplies its own from
/// the filesystem manifest. Without one the node endpoints fail at request time while everything else
/// works, and it surfaces as a 500 with an empty body. These tests author no widgets, so an empty catalog
/// is the honest stand-in rather than a stub that pretends tags exist.
/// </summary>
internal sealed class NoWidgets : IWidgetCatalog
{
    public bool Exists(string tag) => false;

    public IReadOnlySet<string> PropNames(string tag) => new HashSet<string>();
}

/// <summary>
/// GET /api/authoring/pages/{id}/history and POST .../restore/{version}, over real HTTP against a real
/// event store.
///
/// These exist to close the gap that kept system documentation out of the CMS: a page's only recoverable
/// state used to be its last publish, so pages making claims somebody audits were kept as HTML in git and
/// generated in. What has to hold for that to stop being necessary is (a) an earlier wording is readable,
/// (b) it can be put back, and (c) putting it back does not erase the change being undone — an audit trail
/// that a rollback rewrites is not an audit trail.
/// </summary>
public sealed class AuthoringPageHistoryHost : IAsyncLifetime
{
    private WebApplication? _app;
    private string _dataDirectory = "";

    public const string Token = "test-authoring-token";

    public HttpClient Client { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        _dataDirectory = Path.Combine(Path.GetTempPath(), $"imprint-history-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_dataDirectory);

        var builder = WebApplication.CreateBuilder();
        builder.Configuration[AuthoringApi.TokenKey] = Token;
        builder.Services.AddImprintAuthoring($"Data Source={Path.Combine(_dataDirectory, "imprint.db")}");
        builder.Services.AddSingleton<IWidgetCatalog>(new NoWidgets());
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();

        _app = builder.Build();
        await _app.Services.InitializeImprintEventSourcing();
        _app.UseDeveloperExceptionPage();   // a 500 with an empty body is not a test failure you can read
        _app.MapAuthoringApi();
        await _app.StartAsync();

        Client = new HttpClient { BaseAddress = new Uri(_app.Urls.First()) };
        Client.DefaultRequestHeaders.Authorization = new("Bearer", Token);
    }

    public async ValueTask DisposeAsync()
    {
        Client?.Dispose();
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }

        try
        {
            Directory.Delete(_dataDirectory, recursive: true);
        }
        catch (IOException)
        {
            // A held file handle is not worth failing a green run over.
        }
    }
}

public sealed class AuthoringPageHistoryApiTests(AuthoringPageHistoryHost host)
    : IClassFixture<AuthoringPageHistoryHost>
{
    private async Task<HttpResponseMessage> Raw(HttpMethod method, string path, object? payload = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (payload is not null)
        {
            request.Content = JsonContent.Create(payload);
        }

        return await host.Client.SendAsync(request);
    }

    private async Task<JsonElement> Send(HttpMethod method, string path, object? payload = null)
    {
        using var response = await Raw(method, path, payload);
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"{method} {path} -> {(int)response.StatusCode} {text}");
        return JsonDocument.Parse(text).RootElement.Clone();
    }

    /// <summary>A page carrying one paragraph, whose text is then edited twice.</summary>
    private async Task<(string PageId, string NodeId)> APageEditedTwice()
    {
        var site = (await Send(HttpMethod.Post, "/api/authoring/sites",
            new { Name = $"Site {Guid.NewGuid():N}" })).GetProperty("siteId").GetString()!;
        var page = (await Send(HttpMethod.Post, $"/api/authoring/sites/{site}/pages",
            new { Title = "A documented claim", Slug = "a-documented-claim" })).GetProperty("pageId").GetString()!;

        await Send(HttpMethod.Post, $"/api/authoring/pages/{page}/nodes", new
        {
            // section > stack > richtext: a section holds layout, not prose directly.
            node = new
            {
                type = "section",
                children = new object[]
                {
                    new
                    {
                        type = "stack",
                        children = new object[]
                        {
                            new { type = "richtext", html = "<p>The database is encrypted at rest.</p>" },
                        },
                    },
                },
            },
            parentId = (string?)null,
            index = (int?)null,
            locale = "en",
        });

        var tree = await Send(HttpMethod.Get, $"/api/authoring/pages/{page}/tree?content=false");
        var node = tree.GetProperty("nodes").EnumerateArray()
            .First(n => n.GetProperty("type").GetString() == "RichTextNode")
            .GetProperty("id").GetString()!;

        await Send(HttpMethod.Put, $"/api/authoring/pages/{page}/nodes/{node}/text",
            new { field = "html", value = "<p>The database is encrypted at rest with LUKS2.</p>", locale = "en" });
        await Send(HttpMethod.Put, $"/api/authoring/pages/{page}/nodes/{node}/text",
            new { field = "html", value = "<p>Storage is encrypted.</p>", locale = "en" });

        return (page, node);
    }

    private static string HtmlOf(JsonElement tree, string nodeId) =>
        tree.GetProperty("nodes").EnumerateArray()
            .First(n => n.GetProperty("id").GetString() == nodeId)
            .GetProperty("props").GetProperty("html").GetProperty("en").GetString()!;

    [Fact]
    public async Task Reports_every_revision_with_who_changed_what_and_when()
    {
        var (page, _) = await APageEditedTwice();

        var history = await Send(HttpMethod.Get, $"/api/authoring/pages/{page}/history");
        var revisions = history.GetProperty("revisions").EnumerateArray().ToList();
        var changes = revisions.Select(r => r.GetProperty("change").GetString()).ToList();

        Assert.Equal("created", changes[0]);
        Assert.Contains("node-added", changes);
        Assert.Equal(2, changes.Count(c => c == "text"));
        // Versions are the handle a restore is addressed by, so they must be present and ordered.
        Assert.Equal(revisions.Select((_, i) => (long)(i + 1)),
            revisions.Select(r => r.GetProperty("version").GetInt64()));
        Assert.All(revisions, r => Assert.False(string.IsNullOrWhiteSpace(r.GetProperty("actor").GetString())));
    }

    [Fact]
    public async Task Compact_log_withholds_the_text_but_still_shows_a_blanking()
    {
        var (page, _) = await APageEditedTwice();

        var compact = await Send(HttpMethod.Get, $"/api/authoring/pages/{page}/history");
        var text = compact.GetProperty("revisions").EnumerateArray()
            .First(r => r.GetProperty("change").GetString() == "text").GetProperty("detail");

        Assert.False(text.TryGetProperty("value", out _));
        // The length is the point: it is what makes an overwrite-to-nothing legible without the body.
        Assert.True(text.GetProperty("length").GetInt32() > 0);
    }

    [Fact]
    public async Task Content_true_returns_the_earlier_wording_so_it_can_be_recovered_by_reading()
    {
        var (page, _) = await APageEditedTwice();

        var full = await Send(HttpMethod.Get, $"/api/authoring/pages/{page}/history?content=true");
        var values = full.GetProperty("revisions").EnumerateArray()
            .Where(r => r.GetProperty("change").GetString() == "text")
            .Select(r => r.GetProperty("detail").GetProperty("value").GetString())
            .ToList();

        Assert.Contains(values, v => v!.Contains("LUKS2"));
    }

    [Fact]
    public async Task Restores_the_content_that_stood_at_a_given_revision()
    {
        var (page, node) = await APageEditedTwice();

        var history = await Send(HttpMethod.Get, $"/api/authoring/pages/{page}/history?content=true");
        var luks = history.GetProperty("revisions").EnumerateArray()
            .First(r => r.GetProperty("change").GetString() == "text"
                        && r.GetProperty("detail").GetProperty("value").GetString()!.Contains("LUKS2"));
        var version = luks.GetProperty("version").GetInt64();

        await Send(HttpMethod.Post, $"/api/authoring/pages/{page}/restore/{version}");

        var tree = await Send(HttpMethod.Get, $"/api/authoring/pages/{page}/tree?content=true");
        Assert.Contains("LUKS2", HtmlOf(tree, node));
    }

    [Fact]
    public async Task A_restore_is_appended_so_the_change_it_undoes_stays_readable()
    {
        var (page, _) = await APageEditedTwice();

        var before = await Send(HttpMethod.Get, $"/api/authoring/pages/{page}/history");
        var countBefore = before.GetProperty("revisions").GetArrayLength();

        await Send(HttpMethod.Post, $"/api/authoring/pages/{page}/restore/1");

        var after = await Send(HttpMethod.Get, $"/api/authoring/pages/{page}/history?content=true");
        var revisions = after.GetProperty("revisions").EnumerateArray().ToList();

        // The history GREW. A rollback that truncated the stream would leave the audit trail saying the
        // undone edit never happened, which is the failure this whole feature exists to avoid.
        Assert.Equal(countBefore + 1, revisions.Count);
        Assert.Equal("content-restored", revisions[^1].GetProperty("change").GetString());
        Assert.Contains(revisions, r => r.GetProperty("change").GetString() == "text"
                                        && r.GetProperty("detail").GetProperty("value").GetString()!.Contains("LUKS2"));
    }

    [Fact]
    public async Task Refuses_a_revision_the_page_does_not_have_rather_than_restoring_the_latest()
    {
        var (page, _) = await APageEditedTwice();

        using var response = await Raw(HttpMethod.Post, $"/api/authoring/pages/{page}/restore/900");
        var text = await response.Content.ReadAsStringAsync();

        // ReadStream is bounded, not exact — without the explicit check this would silently restore the
        // newest revision while the caller believed it had restored revision 900.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("there is no revision 900", text);
    }

    [Fact]
    public async Task Rejects_an_unknown_page_and_a_malformed_id()
    {
        using var unknown = await Raw(HttpMethod.Get, $"/api/authoring/pages/{Guid.NewGuid():N}/history");
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);

        using var malformed = await Raw(HttpMethod.Get, "/api/authoring/pages/not-a-page-id/history");
        Assert.Equal(HttpStatusCode.BadRequest, malformed.StatusCode);
    }
}
