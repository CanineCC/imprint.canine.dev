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
/// The authoring API's post endpoints, over real HTTP against a real event store. Only the API is
/// mapped — no Blazor, no background workers — so this stays a fast test rather than a boot of the
/// whole editor, while still exercising routing, model binding and the command path end to end.
/// </summary>
public sealed class AuthoringPostApiHost : IAsyncLifetime
{
    private WebApplication? _app;
    private string _dataDirectory = "";

    public const string Token = "test-authoring-token";

    public HttpClient Client { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        _dataDirectory = Path.Combine(Path.GetTempPath(), $"imprint-api-{Guid.NewGuid():N}");
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

public sealed class AuthoringPostApiTests(AuthoringPostApiHost host) : IClassFixture<AuthoringPostApiHost>
{
    private static readonly DateTimeOffset GoLive = new(2027, 1, 12, 8, 0, 0, TimeSpan.Zero);

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

    /// <summary>A post with a body, on its own site, which reviews before it publishes.</summary>
    private async Task<string> APost(DateTimeOffset? publishAt = null)
    {
        var site = (await Send(HttpMethod.Post, "/api/authoring/sites",
            new { Name = $"Blog {Guid.NewGuid():N}" })).GetProperty("siteId").GetString()!;
        var post = (await Send(HttpMethod.Post, $"/api/authoring/sites/{site}/posts",
            new { Title = "A post that has a date" })).GetProperty("postId").GetString()!;

        await Send(HttpMethod.Put, $"/api/authoring/posts/{post}/body",
            new { Locale = "en", Markdown = "# A post\n\nWith a paragraph that converts.\n" });
        await Send(HttpMethod.Put, $"/api/authoring/sites/{site}/reviewer",
            new { Name = "Lasse", Email = "reviewer@example.com" });
        if (publishAt is { } at)
        {
            await Send(HttpMethod.Put, $"/api/authoring/posts/{post}/schedule", new { PublishAt = at });
        }

        return post;
    }

    /// <summary>The same, already handed to the reviewer with no proposal of its own.</summary>
    private async Task<string> ASubmittedPost(DateTimeOffset? publishAt)
    {
        var post = await APost(publishAt);
        await Send(HttpMethod.Post, $"/api/authoring/posts/{post}/submit-review", new { });
        return post;
    }

    private async Task<DateTimeOffset?> PublishAtOf(string post)
    {
        var view = await Send(HttpMethod.Get, $"/api/authoring/posts/{post}");
        return view.GetProperty("publishAt").ValueKind is JsonValueKind.Null
            ? null
            : view.GetProperty("publishAt").GetDateTimeOffset();
    }

    // The same defect one step earlier: post.submitted-for-review also carries the resulting date,
    // so submitting with no proposal unscheduled the post before the reviewer ever saw it.
    [Fact]
    public async Task Submitting_for_review_without_a_proposal_keeps_the_authors_date()
    {
        var post = await APost(GoLive);

        await Send(HttpMethod.Post, $"/api/authoring/posts/{post}/submit-review", new { });

        Assert.Equal(GoLive, await PublishAtOf(post));
    }

    // The author's proposal still travels with the submission when one is given.
    [Fact]
    public async Task Submitting_for_review_carries_the_proposal_it_is_given()
    {
        var post = await APost();

        await Send(HttpMethod.Post, $"/api/authoring/posts/{post}/submit-review",
            new { ProposedPublishAt = GoLive });

        Assert.Equal(GoLive, await PublishAtOf(post));
    }

    // The bug: approve passed the request's null straight through, raising post.review-approved
    // with no date — so saying yes silently unscheduled a post the author had already dated.
    [Fact]
    public async Task Approving_without_a_date_keeps_the_one_the_post_already_had()
    {
        var post = await ASubmittedPost(GoLive);

        await Send(HttpMethod.Post, $"/api/authoring/posts/{post}/approve", new { });

        Assert.Equal(GoLive, await PublishAtOf(post));
    }

    [Fact]
    public async Task Approving_with_no_body_at_all_also_keeps_the_date()
    {
        var post = await ASubmittedPost(GoLive);

        await Send(HttpMethod.Post, $"/api/authoring/posts/{post}/approve");

        Assert.Equal(GoLive, await PublishAtOf(post));
    }

    [Fact]
    public async Task The_reviewer_can_still_overrule_the_date_while_approving()
    {
        var post = await ASubmittedPost(GoLive);
        var moved = GoLive.AddDays(14);

        var result = await Send(HttpMethod.Post, $"/api/authoring/posts/{post}/approve", new { PublishAt = moved });

        Assert.Equal(moved, result.GetProperty("publishAt").GetDateTimeOffset());
        Assert.Equal(moved, await PublishAtOf(post));
    }

    // "Approving without a date keeps it waiting until someone sets one" — the mail's promise, and
    // the case that must NOT be mistaken for "keep the date" when there is no date to keep.
    [Fact]
    public async Task A_post_with_no_date_is_approved_and_stays_undated()
    {
        var post = await ASubmittedPost(null);

        await Send(HttpMethod.Post, $"/api/authoring/posts/{post}/approve", new { });

        Assert.Null(await PublishAtOf(post));
    }

    [Fact]
    public async Task Approving_an_unknown_post_is_a_not_found_rather_than_a_dispatch()
    {
        using var response = await host.Client.PostAsync(
            $"/api/authoring/posts/{Guid.NewGuid():N}/approve", JsonContent.Create(new { }));

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    // The history endpoint that made the blanked-post recovery possible.
    [Fact]
    public async Task History_carries_every_body_revision_and_its_markdown_on_request()
    {
        var post = await ASubmittedPost(GoLive);
        await Send(HttpMethod.Put, $"/api/authoring/posts/{post}/body", new { Locale = "en", Markdown = "" });

        var compact = await Send(HttpMethod.Get, $"/api/authoring/posts/{post}/history");
        var bodies = compact.GetProperty("revisions").EnumerateArray()
            .Where(r => r.GetProperty("change").GetString() == "body").ToList();
        Assert.Equal(2, bodies.Count);
        Assert.Equal(0, bodies[^1].GetProperty("detail").GetProperty("length").GetInt32());
        // Compact by default: the text is the bulk of the payload and is only sent when asked for.
        Assert.False(bodies[0].GetProperty("detail").TryGetProperty("markdown", out _));

        var full = await Send(HttpMethod.Get, $"/api/authoring/posts/{post}/history?body=true");
        var texts = full.GetProperty("revisions").EnumerateArray()
            .Where(r => r.GetProperty("change").GetString() == "body")
            .Select(r => r.GetProperty("detail").GetProperty("markdown").GetString()).ToList();
        Assert.Equal("# A post\n\nWith a paragraph that converts.\n", texts[0]);
        Assert.Equal("", texts[^1]);
    }
}
