using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Imprint.Authoring;
using Imprint.Editor.Api;
using Imprint.EventSourcing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Imprint.Editor.Tests;

/// <summary>
/// DELETE /api/authoring/pages/{pageId}, over real HTTP against a real event store. The route was
/// added because the API could create pages but never remove one, so a superseded or mistaken page
/// could only be cleaned up in the editor UI. What is worth pinning is not the happy path but the two
/// guards: the route must surface them as a 400 that NAMES the reason, because a caller deleting a
/// batch of pages needs to tell "refused, still in navigation" apart from "failed".
/// </summary>
public sealed class AuthoringDeletePageHost : IAsyncLifetime
{
    private WebApplication? _app;
    private string _dataDirectory = "";

    public const string Token = "test-authoring-token";

    public HttpClient Client { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        _dataDirectory = Path.Combine(Path.GetTempPath(), $"imprint-delete-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_dataDirectory);

        var builder = WebApplication.CreateBuilder();
        builder.Configuration[AuthoringApi.TokenKey] = Token;
        builder.Services.AddImprintAuthoring($"Data Source={Path.Combine(_dataDirectory, "imprint.db")}");
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();

        _app = builder.Build();
        await _app.Services.InitializeImprintEventSourcing();
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

public sealed class AuthoringDeletePageApiTests(AuthoringDeletePageHost host)
    : IClassFixture<AuthoringDeletePageHost>
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

    private async Task<string> ASite() =>
        (await Send(HttpMethod.Post, "/api/authoring/sites",
            new { Name = $"Site {Guid.NewGuid():N}" })).GetProperty("siteId").GetString()!;

    // The API does not derive a slug from the title — it passes the null straight to the command,
    // which refuses it. So every page here is created with an explicit one.
    private async Task<string> APage(string site, string title, string slug) =>
        (await Send(HttpMethod.Post, $"/api/authoring/sites/{site}/pages",
            new { Title = title, Slug = slug })).GetProperty("pageId").GetString()!;

    // The listing comes back as a bare array, not wrapped in an object.
    private async Task<HashSet<string>> PageIds(string site)
    {
        var listed = await Send(HttpMethod.Get, $"/api/authoring/sites/{site}/pages");
        return listed.EnumerateArray()
            .Select(p => p.GetProperty("id").GetString()!)
            .ToHashSet();
    }

    [Fact]
    public async Task Deletes_a_page_that_is_not_in_navigation_and_not_the_last_one()
    {
        var site = await ASite();
        await APage(site, "Kept", "kept");
        var doomed = await APage(site, "Superseded draft", "superseded-draft");

        var deleted = await Send(HttpMethod.Delete, $"/api/authoring/pages/{doomed}");

        Assert.True(deleted.GetProperty("deleted").GetBoolean());
        Assert.DoesNotContain(doomed, await PageIds(site));
    }

    [Fact]
    public async Task Refuses_while_the_page_is_still_in_the_site_navigation()
    {
        var site = await ASite();
        await APage(site, "Kept", "kept");
        var linked = await APage(site, "Linked from the nav", "linked-from-the-nav");

        await Send(HttpMethod.Put, $"/api/authoring/sites/{site}/navigation",
            new { items = new[] { new { label = "Linked", pageId = linked } } });

        using var response = await Raw(HttpMethod.Delete, $"/api/authoring/pages/{linked}");
        var text = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        // The message has to say WHICH guard fired — "delete failed" alone would send the caller
        // hunting through logs for something the API already knows.
        Assert.Contains("navigation", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(linked, await PageIds(site));
    }

    /// <summary>
    /// ★ Characterises a KNOWN GAP rather than the desired behaviour. The handler's last-page guard is
    /// <c>pageList.All().Count &lt;= 1</c> — a count over the WHOLE store, on the stated assumption that
    /// "the editor manages a single site". This deployment serves five (Watchdog, CAI, Assay, Canine,
    /// Canine Blog), so the guard cannot fire in practice: a site's final page is deletable as long as
    /// any other site holds one, which leaves that site rendering nothing.
    /// If the guard is ever scoped per-site, THIS TEST SHOULD FAIL — flip it to assert the refusal then.
    /// </summary>
    [Fact]
    public async Task Last_page_guard_is_scoped_to_the_whole_store_not_the_site()
    {
        var populated = await ASite();
        await APage(populated, "Another site's page", "another-sites-page");

        var site = await ASite();
        var only = await APage(site, "The only page", "the-only-page");

        using var response = await Raw(HttpMethod.Delete, $"/api/authoring/pages/{only}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(await PageIds(site));
    }

    [Fact]
    public async Task Rejects_a_malformed_page_id_without_touching_the_store()
    {
        using var response = await Raw(HttpMethod.Delete, "/api/authoring/pages/not-a-page-id");
        var text = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("invalid pageId", text);
    }
}
