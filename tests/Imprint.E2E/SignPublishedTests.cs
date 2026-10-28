using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Imprint.E2E;

/// <summary>
/// Articles that went live without a signature are signed by the reviewer from the articles list in one go, and the
/// post editor then shows the signature. With sign-in off (as here) the editor treats the visitor as the reviewer.
/// </summary>
[Collection("editor")]
public sealed class SignPublishedTests(EditorFixture fixture)
{
    [Fact]
    public async Task Sign_all_signs_the_live_unsigned_articles_and_the_editor_shows_the_signature()
    {
        var page = await fixture.OpenEditor();
        using var mcp = new McpSession(fixture.BaseUrl);
        var site = (await mcp.Call("create_blog", new JsonObject { ["name"] = "Signing center", ["kind"] = "resource-center" }))["siteId"]!.GetValue<string>();
        var posts = new List<string>();
        foreach (var slug in new[] { "first-live", "second-live" })
        {
            var post = (await mcp.Call("create_post", new JsonObject
            {
                ["siteId"] = site, ["title"] = slug, ["slug"] = slug, ["locale"] = "en", ["kind"] = "guide",
            }))["postId"]!.GetValue<string>();
            await mcp.Call("set_post_body", new JsonObject { ["postId"] = post, ["markdown"] = "# Hello\n\nLive words.\n", ["locale"] = "en" });
            await Publish(post);
            posts.Add(post);
        }

        await mcp.Call("set_site_reviewer", new JsonObject { ["siteId"] = site, ["name"] = "Jimmy", ["email"] = "jimmy@example.com" });

        await page.GotoAsync($"/site/{site}/posts");
        await page.WaitForInteractive();
        var signAll = page.Locator("[data-testid='posts-sign-all']");
        await Expect(signAll).ToContainTextAsync("Sign all 2 live articles");
        await Shot(page, "posts-sign-all.png");
        await signAll.ClickAsync();
        await Expect(signAll).ToHaveCountAsync(0);

        await page.GotoAsync($"/posts/{posts[0]}");
        await page.WaitForInteractive();
        await Expect(page.Locator("[data-testid='post-signature-line']")).ToContainTextAsync("Signed by");
        await Expect(page.Locator("[data-testid='post-sign']")).ToHaveCountAsync(0);
        await Shot(page, "post-signed.png");
    }

    private async Task Publish(string post)
    {
        using var http = new HttpClient { BaseAddress = new Uri(fixture.BaseUrl) };
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", EditorFixture.AuthoringToken);
        using var response = await http.PostAsync($"/api/authoring/posts/{post}/publish", null);
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
    }

    private static async Task Shot(IPage page, string name)
    {
        if (Environment.GetEnvironmentVariable("IMPRINT_E2E_SHOTS") is { Length: > 0 } shots)
        {
            await page.ScreenshotAsync(new() { Path = Path.Combine(shots, name), FullPage = true });
        }
    }
}
