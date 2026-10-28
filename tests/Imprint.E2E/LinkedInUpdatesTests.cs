using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Imprint.E2E;

/// <summary>
/// An update planned through the MCP tools, as an agent plans one, appears on the site's LinkedIn page with its
/// time, sender and state, and can be cancelled there. Site settings offer "Connect my LinkedIn" and say when the
/// member app is not set up, as it is not here: nothing in this fixture can reach LinkedIn. Each update is shown as it
/// will look in the feed: the sender's name, the text cut where LinkedIn cuts it with "…more" to open it, and its images.
/// </summary>
[Collection("editor")]
public sealed class LinkedInUpdatesTests(EditorFixture fixture)
{
    [Fact]
    public async Task An_update_planned_through_mcp_is_listed_and_can_be_cancelled()
    {
        var page = await fixture.OpenEditor();
        await page.ClickAsync(".ed-gear");
        await page.WaitForURLAsync("**/settings");
        await page.WaitForInteractive();
        var siteId = page.Url.Split("/sites/")[1].Split('/')[0];

        await Expect(page.Locator("[data-testid='linkedin-members']")).ToContainTextAsync("not set up on this install");
        await Shot(page, "linkedin-members-settings.png", "#set-linkedin-members");

        using var mcp = new McpSession(fixture.BaseUrl);
        await mcp.Call("set_site_linkedin_senders", new JsonObject
        {
            ["siteId"] = siteId,
            ["senders"] = new JsonArray(
                new JsonObject { ["key"] = "canine", ["name"] = "Canine Development", ["organizationUrn"] = "urn:li:organization:12345" },
                new JsonObject { ["key"] = "jimmy", ["name"] = "Jimmy Borch", ["organizationUrn"] = "urn:li:person:Ab3" }),
        });
        var uploaded = await mcp.Call("upload_asset", new JsonObject
        {
            ["base64"] = OnePixelPng, ["fileName"] = "levels.png", ["contentType"] = "image/png",
        });
        var assetId = uploaded["assetId"]!.GetValue<string>();
        for (var tries = 0; tries < 100; tries++)
        {
            var asset = await mcp.Call("get_asset", new JsonObject { ["assetId"] = assetId });
            if (asset.ToJsonString().Contains("\"Ready\"", StringComparison.Ordinal)) break;
            await Task.Delay(100);
        }

        var text = $"Review cannot keep up {Guid.NewGuid():N}"[..30];
        var scheduled = await mcp.Call("schedule_linkedin_update", new JsonObject
        {
            ["siteId"] = siteId, ["sender"] = "jimmy", ["text"] = text + "\n\nLine two.\n\nLine three.\n\nThe hidden part. #AI",
            ["images"] = new JsonArray(new JsonObject { ["assetId"] = assetId, ["alt"] = "The five levels" }),
            ["publishAt"] = DateTimeOffset.UtcNow.AddDays(3).ToOffset(TimeSpan.FromHours(1)).ToString("yyyy-MM-ddTHH:mm:sszzz"),
        });
        Assert.True(scheduled["ok"]!.GetValue<bool>(), scheduled.ToJsonString());

        await page.GotoAsync($"/site/{siteId}/linkedin");
        await page.WaitForInteractive();
        var row = page.Locator("[data-testid='linkedin-update']", new() { HasText = text });
        await Expect(row.Locator("[data-testid='linkedin-update-state']")).ToHaveTextAsync("Scheduled");
        var preview = row.Locator("[data-testid='linkedin-update-preview']");
        await Expect(preview).ToContainTextAsync("Jimmy Borch");
        await Expect(preview.GetByRole(AriaRole.Img, new() { Name = "The five levels" })).ToBeVisibleAsync();
        await Expect(preview.GetByText("The hidden part.")).Not.ToBeVisibleAsync();
        await preview.Locator("[data-testid='linkedin-update-more']").ClickAsync();
        await Expect(preview.GetByText("The hidden part.")).ToBeVisibleAsync();
        await Shot(page, "linkedin-updates.png", "main");

        await row.Locator("[data-testid='linkedin-update-cancel']").ClickAsync();
        await Expect(row.Locator("[data-testid='linkedin-update-state']")).ToHaveTextAsync("Cancelled");

        var listed = await mcp.Call("list_linkedin_updates", new JsonObject { ["siteId"] = siteId });
        var mine = listed["updates"]!.AsArray().Single(u => u!["firstLine"]!.GetValue<string>() == text)!;
        Assert.Equal("Cancelled", mine["state"]!.GetValue<string>());

        await page.GotoAsync("/");
        await page.WaitForInteractive();
        await Expect(page.Locator("[data-testid='site-linkedin']").First).ToBeVisibleAsync();
    }

    private const string OnePixelPng =
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkaPhfDwAEhQGAhKmMIQAAAABJRU5ErkJggg==";

    private static async Task Shot(IPage page, string name, string selector)
    {
        if (Environment.GetEnvironmentVariable("IMPRINT_E2E_SHOTS") is { Length: > 0 } shots)
        {
            await page.Locator(selector).ScrollIntoViewIfNeededAsync();
            await page.ScreenshotAsync(new() { Path = Path.Combine(shots, name), FullPage = true });
        }
    }

    /// <summary>The MCP streamable-HTTP handshake and tool calls, as an agent's client makes them.</summary>
    private sealed class McpSession(string baseUrl) : IDisposable
    {
        private readonly HttpClient _http = new() { BaseAddress = new Uri(baseUrl) };
        private string? _session;
        private int _id;

        public void Dispose() => _http.Dispose();

        public async Task<JsonObject> Call(string tool, JsonObject arguments)
        {
            if (_session is null)
            {
                await Send("initialize", new JsonObject
                {
                    ["protocolVersion"] = "2025-06-18",
                    ["capabilities"] = new JsonObject(),
                    ["clientInfo"] = new JsonObject { ["name"] = "imprint-e2e", ["version"] = "1" },
                });
                await Send("notifications/initialized", null, notification: true);
            }

            var result = await Send("tools/call", new JsonObject { ["name"] = tool, ["arguments"] = arguments });
            var content = result!["result"]!["content"]!.AsArray()[0]!["text"]!.GetValue<string>();
            return JsonNode.Parse(content)!.AsObject();
        }

        private async Task<JsonNode?> Send(string method, JsonObject? parameters, bool notification = false)
        {
            var message = new JsonObject { ["jsonrpc"] = "2.0", ["method"] = method };
            if (!notification)
            {
                message["id"] = ++_id;
            }

            if (parameters is not null)
            {
                message["params"] = parameters;
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
            {
                Content = new StringContent(message.ToJsonString(), Encoding.UTF8, "application/json"),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", EditorFixture.AuthoringToken);
            request.Headers.Accept.ParseAdd("application/json");
            request.Headers.Accept.ParseAdd("text/event-stream");
            if (_session is not null)
            {
                request.Headers.Add("Mcp-Session-Id", _session);
            }

            using var response = await _http.SendAsync(request);
            response.EnsureSuccessStatusCode();
            if (response.Headers.TryGetValues("Mcp-Session-Id", out var ids))
            {
                _session = ids.First();
            }

            if (notification)
            {
                return null;
            }

            var body = await response.Content.ReadAsStringAsync();
            // Streamable HTTP may answer as an event stream: the JSON-RPC answer is the last data line.
            var json = body.TrimStart().StartsWith('{')
                ? body
                : body.Split('\n').Where(l => l.StartsWith("data:", StringComparison.Ordinal)).Select(l => l[5..].Trim()).Last();
            return JsonNode.Parse(json);
        }
    }
}
