using System.Net.Http.Json;
using System.Text.Json;

namespace Imprint.Editor.Tests;

/// <summary>A page's schedule over the authoring API: set, read back, cancelled.</summary>
public sealed class AuthoringPageScheduleApiTests(AuthoringPostApiHost host) : IClassFixture<AuthoringPostApiHost>
{
    private static readonly DateTimeOffset Monday9 = new(2026, 11, 2, 9, 0, 0, TimeSpan.FromHours(1));

    private async Task<JsonElement> Send(HttpMethod method, string path, object? payload = null)
    {
        using var request = new HttpRequestMessage(method, path) { Content = payload is null ? null : JsonContent.Create(payload) };
        using var response = await host.Client.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"{method} {path} -> {(int)response.StatusCode}: {text}");
        return JsonDocument.Parse(text).RootElement.Clone();
    }

    [Fact]
    public async Task A_page_is_scheduled_read_back_and_cancelled()
    {
        var site = (await Send(HttpMethod.Post, "/api/authoring/sites", new { Name = $"Site {Guid.NewGuid():N}" }))
            .GetProperty("siteId").GetString()!;
        var page = (await Send(HttpMethod.Post, $"/api/authoring/sites/{site}/pages", new { Title = "Launch", Slug = "launch" }))
            .GetProperty("pageId").GetString()!;

        var set = await Send(HttpMethod.Put, $"/api/authoring/pages/{page}/schedule", new { PublishAt = Monday9 });
        Assert.Equal("Pending", set.GetProperty("state").GetString());
        Assert.Equal(Monday9, set.GetProperty("publishAt").GetDateTimeOffset());

        var read = await Send(HttpMethod.Get, $"/api/authoring/pages/{page}/schedule");
        Assert.Equal("Pending", read.GetProperty("state").GetString());

        var cancelled = await Send(HttpMethod.Put, $"/api/authoring/pages/{page}/schedule", new { PublishAt = (DateTimeOffset?)null });
        Assert.Equal("Cancelled", cancelled.GetProperty("state").GetString());
    }
}
