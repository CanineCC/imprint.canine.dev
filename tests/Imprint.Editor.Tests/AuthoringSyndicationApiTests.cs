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
/// The syndication endpoints, over real HTTP against a real event store and the real
/// <see cref="Imprint.Authoring.Syndication.SyndicatedPageStore"/>. Only the API is mapped — no Blazor,
/// no publisher — so this stays fast while still exercising routing, the node parse and the hash.
/// </summary>
public sealed class AuthoringSyndicationApiHost : IAsyncLifetime
{
    private WebApplication? _app;
    private string _dataDirectory = "";

    public const string Token = "test-authoring-token";

    public HttpClient Client { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        _dataDirectory = Path.Combine(Path.GetTempPath(), $"imprint-syndication-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_dataDirectory);

        var builder = WebApplication.CreateBuilder();
        builder.Configuration[AuthoringApi.TokenKey] = Token;
        builder.Services.AddImprintAuthoring($"Data Source={Path.Combine(_dataDirectory, "imprint.db")}");
        builder.Services.AddSingleton<IWidgetCatalog>(new NoWidgets());   // the editor supplies the real one from its manifest
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

public sealed class AuthoringSyndicationApiTests(AuthoringSyndicationApiHost host)
    : IClassFixture<AuthoringSyndicationApiHost>
{
    private async Task<JsonElement> Send(HttpMethod method, string path, object? payload = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (payload is not null)
        {
            request.Content = JsonContent.Create(payload);
        }

        using var response = await host.Client.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"{method} {path} -> {(int)response.StatusCode} {text}");
        return JsonDocument.Parse(text).RootElement.Clone();
    }

    private async Task<string> ASite() =>
        (await Send(HttpMethod.Post, "/api/authoring/sites", new { Name = $"Corpus {Guid.NewGuid():N}" }))
        .GetProperty("siteId").GetString()!;

    /// <summary>
    /// The body a producer pushes: a section containing a heading, prose and a two-column split — the
    /// columns matter because their implicit cell stacks are a SECOND place node ids are minted.
    /// </summary>
    private static object APush(string heading, string body = "<p>A document database and event store.</p>") => new
    {
        title = heading,
        metaTitle = heading,
        metaDescription = "One project in the corpus.",
        node = new
        {
            type = "section",
            width = "Normal",
            children = new object[]
            {
                new { type = "heading", level = 2, text = heading },
                new { type = "richtext", html = body },
                new
                {
                    type = "columns",
                    ratios = new[] { 2, 1 },
                    children = Array.Empty<object>(),
                },
            },
        },
    };

    private async Task<string?> StoredHash(string siteId, string path)
    {
        var listed = await Send(HttpMethod.Get, $"/api/authoring/sites/{siteId}/syndicated");
        foreach (var page in listed.GetProperty("pages").EnumerateArray())
        {
            if (page.GetProperty("path").GetString() == path)
            {
                return page.GetProperty("contentHash").GetString();
            }
        }

        return null;
    }

    [Fact]
    public async Task Re_pushing_identical_syndicated_content_reports_no_change()
    {
        // The endpoint's contract, and the whole reason it answers with a boolean: the producer re-pushes
        // everything it owns on every run, and knowing which pushes were no-ops is how it reports real work
        // instead of traffic. A hash that cannot match itself makes every hourly sweep look like a rewrite
        // of the entire site, which is then exactly what the publisher performs.
        var siteId = await ASite();
        var push = APush("JasperFx/marten");

        var first = await Send(HttpMethod.Put, $"/api/authoring/sites/{siteId}/syndicated/registry/github/jasperfx/marten", push);
        Assert.True(first.GetProperty("changed").GetBoolean());
        var hashAfterFirst = await StoredHash(siteId, "registry/github/jasperfx/marten");

        var second = await Send(HttpMethod.Put, $"/api/authoring/sites/{siteId}/syndicated/registry/github/jasperfx/marten", push);
        var third = await Send(HttpMethod.Put, $"/api/authoring/sites/{siteId}/syndicated/registry/github/jasperfx/marten", push);

        Assert.False(second.GetProperty("changed").GetBoolean());
        Assert.False(third.GetProperty("changed").GetBoolean());

        // And the stored hash is the same hash, not merely a boolean that happened to say false: the publisher
        // reads it as the page's version, so a churning hash re-renders the file even when nothing moved.
        Assert.Equal(hashAfterFirst, await StoredHash(siteId, "registry/github/jasperfx/marten"));
    }

    [Fact]
    public async Task A_syndicated_push_that_really_differs_reports_a_change()
    {
        // The other half of the contract: silence about a real change would be far worse than noise about
        // an imagined one, so content that differs must still say so — and then settle again.
        var siteId = await ASite();
        const string Path = "registry/github/jasperfx/wolverine";

        await Send(HttpMethod.Put, $"/api/authoring/sites/{siteId}/syndicated/{Path}", APush("JasperFx/wolverine"));
        var changedBody = await Send(HttpMethod.Put, $"/api/authoring/sites/{siteId}/syndicated/{Path}",
            APush("JasperFx/wolverine", "<p>Now it says something else entirely.</p>"));
        var settled = await Send(HttpMethod.Put, $"/api/authoring/sites/{siteId}/syndicated/{Path}",
            APush("JasperFx/wolverine", "<p>Now it says something else entirely.</p>"));

        Assert.True(changedBody.GetProperty("changed").GetBoolean());
        Assert.False(settled.GetProperty("changed").GetBoolean());
    }

    [Fact]
    public async Task A_changed_title_alone_reports_a_change()
    {
        // The hash covers the metadata too, and it must keep doing so: a page whose title changed renders
        // differently even though its body did not.
        var siteId = await ASite();
        const string Path = "registry/github/jasperfx/lamar";

        await Send(HttpMethod.Put, $"/api/authoring/sites/{siteId}/syndicated/{Path}", APush("JasperFx/lamar"));
        var renamed = await Send(HttpMethod.Put, $"/api/authoring/sites/{siteId}/syndicated/{Path}", APush("JasperFx/Lamar (IoC)"));

        Assert.True(renamed.GetProperty("changed").GetBoolean());
    }

    [Fact]
    public async Task An_authored_page_still_mints_a_fresh_id_for_every_added_node()
    {
        // Syndication and authoring share one node parser, and they must not share this: an authored page is
        // edited by a person, so pushing the same spec twice means TWO nodes. Ids minted per add are what makes
        // that true — and what makes an add unable to collide with or hijack an existing node.
        var siteId = await ASite();
        var pageId = (await Send(HttpMethod.Post, $"/api/authoring/sites/{siteId}/pages",
            new { Title = "About", Slug = "about" })).GetProperty("pageId").GetString()!;

        var section = (await Send(HttpMethod.Post, $"/api/authoring/pages/{pageId}/nodes",
            new { node = new { type = "section" } })).GetProperty("nodeId").GetString()!;

        var spec = new { parentId = section, node = new { type = "heading", level = 2, text = "Twice" } };
        var one = (await Send(HttpMethod.Post, $"/api/authoring/pages/{pageId}/nodes", spec)).GetProperty("nodeId").GetString();
        var two = (await Send(HttpMethod.Post, $"/api/authoring/pages/{pageId}/nodes", spec)).GetProperty("nodeId").GetString();

        Assert.NotEqual(one, two);

        var tree = await Send(HttpMethod.Get, $"/api/authoring/pages/{pageId}/tree");
        var ids = tree.GetProperty("nodes").EnumerateArray().Select(n => n.GetProperty("id").GetString()).ToList();
        Assert.Equal(3, ids.Count);
        Assert.Equal(3, ids.Distinct().Count());
    }
}
